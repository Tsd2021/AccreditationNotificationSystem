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

## Scripts

`Scripts/` contains two SQL migration scripts (run manually against the database):
- `AddNombreArchivoOriginalAcreditacionesConError.sql`
- `CreateAcreditacionDepositoSantanderPendiente.sql`
