# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**TAAS** (Tecnisegur Acreditaciones y Notificaciones) is a WPF desktop application (.NET 8.0-windows) that automates bank accreditation file generation and email notifications for multiple Uruguayan banks (BBVA, SANTANDER, SCOTIABANK, ITAU, BANDES, HSBC).

## Build & Run

```bash
# Build
dotnet build ANS.csproj

# Publish (self-contained single-file, win-x64)
dotnet publish ANS.csproj -c Release -r win-x64

# Run from Visual Studio: F5 or Ctrl+F5
# No automated test suite exists
```

There is no unit test project. The closest thing is `Model/Jobs/JobPrueba.cs`, a placeholder Quartz job for manual integration testing.

## Architecture

### Layer Structure

```
Model/
  Services/        # Business logic (~25 services, all use ADO.NET raw SQL)
  Interfaces/      # Contracts for services
  Jobs/            # Quartz job implementations (one folder per bank)
  GeneradorArchivoPorBanco/  # Factory pattern: bank-specific file generators
  DTOs/            # Data transfer objects
  Reports/         # RDLC report definitions
ViewModel/         # MVVM ViewModels bound to Views via DataContext
Views/             # XAML forms
UserControls/      # Reusable WPF controls
Runtime/           # TEST vs PRODUCTION environment isolation (see below)
Scheduling/        # Quartz scheduler initialization and SQLite job history
Connected Services/ # WCF service references (TensStdr for Santander)
```

### Key Patterns

- **Services as singletons** — accessed via `ServicioXxx.getInstancia()` (Lazy<T> thread-safe)
- **ADO.NET raw SQL** — no EF Core, no Dapper; all services use `SqlConnection`/`SqlCommand`/`SqlDataReader` directly
- **Parameterized queries** — use `SqlCommand` parameters; never build SQL via string concatenation
- **Three SQL Server databases** — connection strings in `App.config`:
  - `conexionTSD` / `conexionTSDTest` — main business database
  - `conexionWebBuzones` / `conexionWebBuzonesTest` — mailbox operations
  - `conexionENCUESTA` / `conexionENCUESTATest` — surveys
- **Acreditation table name** — never hardcode `AcreditacionDepositoDiegoTest`; always use `TableNameResolver.AcreditacionDeposito` so it resolves to the correct table per runtime mode
- **Bank file generators** — `GeneradorArchivoPorBanco/` uses a Factory pattern; each bank has its own generator class
- **Quartz jobs** — each bank has a job under `Model/Jobs/{BankName}/`; SQLite stores run history (`QuartzRuns.db`)

### Domain Language (Spanish)

The codebase uses Spanish for domain terms. Key vocabulary:
- `Acreditacion` — accreditation (bank payment file)
- `Deposito` — deposit
- `Buzon` / `CuentaBuzon` — mailbox / mailbox account
- `Banco` — bank
- `Cliente` — client
- `Tarea` — task (email/notification task)
- `EnvioMasivo` — bulk send

## Runtime Mode System (TEST vs PRODUCTION)

The app has a strict environment isolation layer in `Runtime/`. **Default is TEST** (safe fallback).

### Switching Modes

1. **Preferred**: Create `ans.mode` file next to the executable with content `TEST` or `PRODUCTION`
2. **Alternative**: Set `<add key="RuntimeMode" value="TEST" />` in `App.config`
3. Priority: `ans.mode` → `App.config` → default (TEST)

### TEST Mode Guarantees

- Uses `*Test` connection strings (mandatory — app fails to start if missing)
- Accreditation table = `AcreditacionDepositoDiegoTest_Replica` (guard throws if PROD table is accessed)
- All file output goes to `TestBasePath` (default: `C:\Users\dchiquiar.ABUDIL\Desktop\TAAS TEST`), configurable via `App.config` key `TestBasePath`
- All generated files prefixed with `TEST_`
- WebServices (Santander WCF) always blocked — throws controlled exception
- SMTP blocked by default; enable with `TestAllowSmtp=true` + `TestEmailWhitelist` in `App.config`; all recipients replaced by whitelist

### Runtime Infrastructure Classes

| Class | Responsibility |
|-------|---------------|
| `AppRuntime` | Central runtime; exposes `IsTest` / `IsProduction` |
| `PathResolver` | Maps PROD file paths → TEST paths under `TestBasePath` |
| `TableNameResolver` | Resolves SQL table names per mode |
| `Guards/FileSystemGuard` | Blocks writes outside `TestBasePath` in TEST |
| `Guards/WebServiceGuard` | Blocks HTTP/WCF calls in TEST |
| `Guards/EmailGuard` | Enforces email whitelist and SMTP policy in TEST |

### Startup Sequence (`App.xaml.cs` OnStartup)

1. Single-instance Mutex (prevents duplicate bank submissions)
2. `AppRuntime.Initialize()` + mode banner displayed
3. Timezone set to "Montevideo Standard Time"
4. Pre-load caches: clients, banks, NC list, branch/client mappings, email tasks
5. `initServicios()` — initializes all service singletons
6. Quartz scheduler setup with `MyJobFactory`
7. SQLite store init via `PathResolver` (resolves path by mode)
8. Main window displayed

## App.config Keys of Note

- `RuntimeMode` — `TEST` or `PRODUCTION`
- `TestBasePath` — local folder root for TEST output
- `TestAllowSmtp` — `true`/`false`
- `TestEmailWhitelist` — comma-separated allowed addresses in TEST
- Connection strings: `conexionTSD`, `conexionTSDTest`, `conexionWebBuzones`, `conexionWebBuzonesTest`, `conexionENCUESTA`, `conexionENCUESTATest`

## Santander TIME OUT — Regla de negocio

Cuando el Web Service de Santander responde TIME OUT (`EnvioSantanderResult.EstadoTimeout = "TIMEOUT"`), el resultado es **ambiguo**: el banco puede haber procesado el archivo aunque el sistema no recibió confirmación.

**Comportamiento requerido:**
- **APPROVED / OK**: insertar solo en tabla principal (`TableNameResolver.AcreditacionDeposito`). No registrar en `AcreditacionesConError`.
- **TIME OUT**: insertar en `AcreditacionesConError` (auditoría) **y también** en la tabla principal. Esto previene re-acreditación del mismo IDOPERACION y deja trazabilidad para revisión manual.
- **Otros errores Santander**: registrar solo en `AcreditacionesConError`. No insertar en tabla principal.
- **Otros bancos**: no se ven afectados (BBVA, Scotiabank, Itaú, Bandes, HSBC no usan WS Santander).

**Implementación:**
- `GeneracionArchivoBancoResult.EsTimeoutWs` encapsula la detección (`RequiereAuditoriaEnvioFallido && EstadoEnvioWsParaAuditoria == "TIMEOUT"`).
- `ServicioCuentaBuzon.LogearTimeoutSantander(...)` centraliza el log de auditoría.
- Ambas tablas tienen `IF NOT EXISTS (IDBUZON + IDOPERACION + MONEDA + IDCUENTA)` — no hay riesgo de duplicados.

**Columna correcta en `AcreditacionesConError`:** `NombreArchivoOrigen` (no `NombreArchivoOriginal`).

## Columnas de acreditación pobladas por el flujo de generación (NSU, NOMBRE_ARCHIVO)

La tabla de acreditaciones (`TableNameResolver.AcreditacionDeposito`) tiene dos columnas que se llenan durante la generación del archivo. Ambas se insertan **siempre** en `ServicioAcreditacion.insertar` y en `ServicioAcreditacionManual.InsertarAcreditacionEnTransaccion`.

- **`NSU INT NULL`** — solo para buzones **PERMAQUIN** (`CC.TIPO = 3`, mapeado a `CuentaBuzon.TipoBuzon` vía `c.TIPO AS TIPOBUZON` en los builders de `ServicioCuentaBuzon`/`ServicioAcreditacionManual`). Se trae `Depositos.NSU` (base WebBuzones) y se guarda en la columna `NSU`. **`IDOPERACION` NO cambia**; el guard anti-duplicados sigue por `IDOPERACION`. No-PERMAQUIN → `NULL`. PERMAQUIN sin NSU → fallback a `IDOPERACION` (con warning). Helpers estáticos: `ServicioAcreditacion.LeerTipoBuzon` y `ResolverNsuParaInsert` (+ `TipoBuzonPermaquin = 3`). La columna `Depositos.NSU` (WebBuzones) la administra otro equipo.
- **`NOMBRE_ARCHIVO nvarchar(255) NULL`** — nombre **base** (sin ruta ni prefijo `TEST_`, estilo PROD) del TXT donde se acreditó cada depósito. Solo bancos que generan archivo (BBVA, Scotiabank, Santander). Mecanismo: cada FileGenerator setea `CuentaBuzon.NombreArchivoGenerado` por buzón durante la generación (la misma lista de `CuentaBuzon` fluye de `generarArchivoPorBanco` a `crearAcreditaciones*`, así que la mutación llega al insert); viaja a `Acreditacion.NombreArchivo` → columna. Santander (contenido aplanado a 6 StringBuilders antes de existir el nombre) lo resuelve con "subir el nombre": snapshot de los archivos en `_archivosDeLaCorrida` + emparejar por bucket `(ciudad, divisa)`; si un bucket se parte en chunks (>500 líneas), gana el primer chunk. Setear el string NO cambia el archivo → no viola la regla de oro de formatos TXT.

## Jobs día a día dedicados por cliente (patrón Nike/Mans/Abasto/URUIMPORTA)

Un cliente puntual puede acreditarse a su propia hora con un job Quartz dedicado. **4 puntos:** (1) clase `AcreditarDiaADia{Cliente}` que llama `acreditarDiaADiaPorCliente(cli, banco, hora)`; (2) case en `MyJobFactory`; (3) region + trigger + `ScheduleJob` en `App.xaml.cs`; (4) **excluir al cliente del run genérico** del banco (lista estática `NOT IN` en `getAllByTipoAcreditacionYBanco`) para no duplicar.

- **URUIMPORTA (ID 1014)** — Scotiabank, DXD dedicado **07:03 MON-FRI** (`AcreditarDiaADiaUruimporta`). Excluido del DXD genérico Scotia (`NOT IN (164, 179, 1014)`). Usa la **hora de cierre de cada buzón** (`TimeSpan.Zero` + rama `cli.IdCliente == 998 || 1014` en `acreditarDiaADiaPorCliente`). Archivo TXT propio con sufijo `_Uruimporta` (excluido del combinador de las 17:10).
- **Semántica del 3er parámetro `horaCierreActual`:** hora fija = corte a esa hora para todos los buzones; `TimeSpan.Zero` = query **sin corte**, SALVO clientes en la rama gated (998 Nike, 1014 Uruimporta) que usan la cierre propia de cada buzón (`cu.Cierre`).

## Envío masivo — selección por hora de cierre

`ServicioEnvioMasivo.getBuzonesByNumeroEnvioMasivo` selecciona buzones por rango de `CC.CIERRE`: masivo **1** `(00:00, 07:00]` (dispara **07:10 MON-FRI**), **2** `(07:00, 14:30]`, **3** `(14:30, 17:00]`, **4** `(17:00, 19:30]`. **Hay un hueco sin cubrir: `(19:30, 24:00]`** → los buzones con cierre en ese rango (ej. 23:00) no entran en ningún masivo. Como parche, la lista hardcodeada `ncIncluirMasivo1` fuerza NCs puntuales dentro del masivo 1: relaja **solo** el filtro de cierre (siguen aplicando `estado='alta'`, `IDCLIENTE<>160` y las exclusiones `NOT IN`).

## TO-DO / Pendientes

- [x] **Reemplazar `IDOPERACION` por `NSU` en el TXT de BBVA para buzones tipo 3 (PERMAQUIN)** — hecho **solo en el path p2p (`Exporta_Reme`)** por decisión del dueño. El remito conserva la forma `IdReferenciaAlCliente + "X" + <valor>`; para PERMAQUIN (`buz.TipoBuzon == ServicioAcreditacion.TipoBuzonPermaquin`) el `<valor>` es el `NSU` del depósito en vez de `IdOperacion`. Se reutiliza `ServicioAcreditacion.ResolverNsuParaInsert(true, dep.NSU, dep.IdOperacion, buz.NC)` para que el remito coincida **siempre** con la columna `NSU` persistida (mismo fallback a `IDOPERACION` + warning si `NSU` es nulo). No-PERMAQUIN → `IdOperacion` (sin cambios). **`Exporta_Reme_Agrupado` NO se tocó** (ahí el remito nunca contuvo `IdOperacion`: es `prefijo(4)+HHmmssff`). Datos verificados como poblados en el flujo: `CuentaBuzon.TipoBuzon` (builders de `ServicioCuentaBuzon`) y `Depositos.NSU` (`ServicioDeposito.cs:158-169`, solo depósitos `"Validado"`).

## Scripts

`Scripts/` contiene scripts SQL de migración (se corren **manualmente** contra la base):
- `AddNombreArchivoOriginalAcreditacionesConError.sql`
- `CreateAcreditacionDepositoSantanderPendiente.sql`
- `AddNsuAcreditaciones.sql` — agrega `NSU INT NULL` a `AcreditacionDepositoDiegoTest` y `_Replica` (lado acreditaciones). El `ALTER` de `Depositos.NSU` (WebBuzones) lo corre otro equipo. La columna `NOMBRE_ARCHIVO` se agregó manualmente en ambas tablas (sin script en el repo).
