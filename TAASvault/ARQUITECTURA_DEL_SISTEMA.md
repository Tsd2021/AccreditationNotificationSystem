# 🏦 ANS/TAAS System Architecture & Data Model

**Accreditation Notification System** — Multi-bank Payment File Generation & Email Automation

> Este es un análisis completo de la arquitectura, base de datos y flujo de datos del sistema TAAS (ANS).

## 📐 Architecture Overview

### Presentation Layer (WPF)
- **Technology**: XAML, MVVM, MaterialDesign themes
- **Components**:
  - MainWindow + BancoModal (bank selection)
  - 23 specialized UserControls (per-bank operation controls)
  - ViewModels (1 per bank type + utilities)
- **Entry**: `App.xaml.cs OnStartup()` — initializes Quartz scheduler

### Business Logic Layer
- **Pattern**: Singleton services, raw ADO.NET (no ORM)
- **Services (~24 total)**:
  - Core: Banco, Cliente, Deposito, CuentaBuzon, CuentaCliente
  - Acreditation: Acreditacion, EnvioManual
  - Email: Email, EmailTarea, Mensajeria
  - Operations: EnvioMasivo, Niveles, Excel
- **Key**: All parameterized SQL queries

### Scheduling & Jobs
- **Framework**: Quartz 3.13.1
- **Job Types**:
  - Day-to-Day accruals (Dia a Dia)
  - Point-to-Point (Punto a Punto)
  - Bulk sends (Envio Masivo)
  - Daily/Weekly Excel reports
- **Per Bank**: BBVA (6), Santander (9), Scotiabank (10), Itaú, Bandes, HSBC (→BTG), Heritage

### Data Access Layer
- **Technology**: Microsoft.Data.SqlClient (raw SQL)
- **Database**: SQL Server + SQLite (job history)
- **Key Pattern**: TableNameResolver routes tables by runtime mode (TEST → *_Test_Replica)

### Runtime Isolation (TEST vs PRODUCTION)
- **AppRuntime**: Central mode dispatcher
- **Guards (TEST mode)**:
  - FileSystemGuard — blocks file writes outside TestBasePath
  - WebServiceGuard — blocks HTTP/WCF calls
  - EmailGuard — whitelist + SMTP policy
- **Toggle**: `ans.mode` file or `App.config`

### File Generation Factory
- **Pattern**: GeneradorArchivoPorBanco (per-bank generator classes)
- **Generators**: BBVA, Santander, Scotiabank, Itaú, Bandes, HSBC, BROU, Heritage
- **Output**: Bank-specific TXT accreditation formats

---

## 💾 Database Schema & Data Model

### Primary Tables

| Table | Purpose | Key Columns | Notes |
|-------|---------|------------|-------|
| **CC** | Customers / Accounts | NC (PK), NN, BANCO, CLIENTE, ID, ESTADO, EMAIL | Master client records; TIPO=3 for PERMAQUIN |
| **CUENTASBUZONES** | Mailbox Accounts | ID (PK), SUCURSAL, CUENTA, MONEDA, BANCO, EMPRESA, IDCLIENTE, TANDA | Bridges customers to bank mailbox operations |
| **ConfiguracionAcreditacion** | Accreditation Config | ConfigId (PK), CuentasBuzonesId (FK), TipoAcreditacion, NC | Defines how each account is accrued |
| **AcreditacionDeposito** | **Accreditation Records** | IDBUZON, IDOPERACION, IDCUENTA, MONEDA (PK), FECHA, MONTO, **NSU**, **NOMBRE_ARCHIVO** | **Main audit trail; NSU & NOMBRE_ARCHIVO are new features** |
| **AcreditacionesConError** | Error Audit | Timestamp, Error details, IDBUZON/IDOPERACION | Tracks failed accruals; Santander timeout special case |
| **Depositos** | Deposits (WebBuzones DB) | IdDeposito (PK), IdOperacion, Codigo, Empresa, NSU, FechaDep, RV | Located in WEBBUZONES server; **NSU is source of truth** |

### Runtime Mode Table Mapping (via TableNameResolver)

```
PRODUCTION:  AcreditacionDeposito
TEST:        AcreditacionDepositoDiegoTest_Replica
```

### Recent Schema Additions (Features)

#### NSU Column (PERMAQUIN)
- **Column**: `AcreditacionDeposito.NSU` (INT)
- **Source**: `Depositos.NSU` from WEBBUZONES (10.0.0.4)
- **Rule**: When `CC.TIPO=3` (PERMAQUIN), extract NSU from deposit and store it
- **Status**: ✅ Implemented; DDL handled by WebBuzones team

#### NOMBRE_ARCHIVO Column
- **Column**: `AcreditacionDeposito.NOMBRE_ARCHIVO` (NVARCHAR 255)
- **Purpose**: Track which bank's TXT file originated the accrual
- **Implementation**: BBVA, Scotia, Santander via `CuentaBuzon.NombreArchivoGenerado`
- **Status**: ✅ Implemented for 3 banks

---

## 📂 Project Structure (ANS.sln)

### ANS (Main Project)
- **.NET 8.0-windows WPF**
- **Folders**:
  - `Model/` — Business logic, jobs, services
  - `ViewModel/` — MVVM ViewModels per bank
  - `Views/` — XAML forms
  - `UserControls/` — Reusable WPF controls (23)
  - `Runtime/` — TEST/PROD isolation
  - `Scheduling/` — Quartz setup
  - `Properties/` — VS project settings

### Model/ (Deep Dive)

**Services/** — 24 singleton services (ADO.NET)
- Core: Banco, Cliente, Deposito, CuentaBuzon, CuentaCliente
- Acreditacion, AcreditacionManual, EnvioAcreditacionManual
- Email, EmailTarea, Mensajeria, Tarea, TareaCC
- EnvioMasivo, EnvioSemanalFrog, Excel, Utilidad, Log
- Santander (WCF), Niveles, SucursalesClientes, FeriadosTAAS

**Jobs/** — Quartz jobs per bank:
- BBVA/: `AcreditarDiaADiaBBVA`, `AcreditarDiaADiaBBVANike`, `AcreditarDiaADiaBBVAMans`, `AcreditarPuntoAPuntoBBVA`, etc. (6 total)
- Santander/: Day-to-day, Point-to-point, Tanda, Henderson variants (9 total)
- Scotiabank/: Day-to-day, Point-to-point, Tanda, Henderson, regional variants (10 total)
- Itaú/, Bandes/, Heritage/: 3–4 each
- ENVIO_MASIVO/, ENVIO_NIVELES/

**GeneradorArchivoPorBanco/** — Bank-specific file generators (Factory pattern)
- `BBVAFileGenerator`, `SantanderFileGenerator`, `ScotiaFileGenerator`, `ItauFileGenerator`, `BandesFileGenerator`, `HSBCFileGenerator`, `BROUFileGenerator`, `HeritageFileGenerator`

**DTOs/** — Data transfer objects
- `BuzonBusquedaDto`, `OperacionEnvioDto`, `ResultadoBatchDto`, `DepositoAcreditacionDto`, `EmpresaDto`, `AcreditacionDTO2`, `BuzonDTO2`, `FrogAcreditacionSucursalDto`, `SucursalClienteDto`

**Domain Model Classes**:
- `Acreditacion`, `Deposito`, `Buzon`, `BuzonDTO`, `CuentaCliente`, `Cliente`, `Banco`, `Tarea`, `Mensaje`, `SnackBarMsg`, `Total`
- `ConfiguracionAcreditacion`, `FeriadoTAAS`, `TipoFeriadoTAAS`, `FeriadosCache`, `MonedaDisplayHelper`
- `EnvioSantanderResult`, `GeneracionArchivoBancoResult`, `Email`

### Supporting Projects

| Project | Purpose |
|---------|---------|
| **ANS.Web** | ASP.NET Core (placeholder) |
| **ANS.Tests** | XUnit (minimal, 2 test classes) |
| **SharedDTOs** | .NET 8.0 class lib (4 shared DTOs) |
| **TAAS.Reports** | WinForms ReportViewer + SSRS |
| **TAAS.ReportGenerator** | Stub project |

---

## ⚙️ Core Patterns & Golden Rules

### Service Singleton Pattern

```csharp
public static Lazy<ServicioXxx> _instancia =>
  new(() => new ServicioXxx());

public static ServicioXxx getInstancia()
  => _instancia.Value;
```

### ADO.NET Query Pattern

```csharp
using (SqlCommand cmd = new SqlCommand(sql, conn)) {
  cmd.Parameters.AddWithValue("@param", value);
  using (SqlDataReader reader = cmd.ExecuteReader()) {
    // Read results
  }
}
```

### TEST Mode Safety

```
Guards in TEST mode:
- Acreditation table: *_Test_Replica
- File output: TEST_*.txt in TestBasePath
- WS calls: BLOCKED (guard throws exception)
- Email: Whitelist-only SMTP

Toggle: ans.mode file or App.config
```

### Bank File Generator Factory

```csharp
GeneradorArchivoPorBanco.
  AcreditacionesPorBancoGenerador(
    banco, acreditaciones)

// Returns bank-specific TXT format
```

### 🚫 Golden Rules

- **Never modify TXT format layouts** without explicit bank owner approval
- **Always parameterize SQL queries** — no string concatenation
- **No duplicate accruals** — use NOT IN static list for dedicated DxD jobs (Nike, Mans pattern)
- **Santander TIMEOUT is special**: record in BOTH audit table AND accrual table to prevent re-accrual
- **Do not commit with "Co-Authored-By: Claude"** — use real author names only
- **Verify scheduling + banner modal** — smoke test of run-ans doesn't cover scheduled jobs; modal blocks before job creation

---

## 🎯 What TAAS Does (End-to-End)

### Typical Daily Workflow

1. **Quartz Scheduler** fires daily at configured times (e.g., 08:00, 14:00)
2. **Job Execution** calls ServicioAcreditacion to fetch pending deposits/operations
3. **File Generation** GeneradorArchivoPorBanco formats deposits into bank-specific TXT
4. **Database Insert** Acreditaciones inserted → NSU + NOMBRE_ARCHIVO recorded
5. **Email Notification** ServicioEmail sends confirmation to configured recipients
6. **History Tracking** Quartz job history stored in SQLite (RepositorioJobHistory)
7. **Error Handling** Failures logged to AcreditacionesConError (audit trail)

### Real Example: BBVA Nike Dedicated Job

**Problem**: Nike is a high-volume customer requiring separate scheduling from generic BBVA customers

**Solution (4-point pattern)**:
1. Create `AcreditarDiaADiaBBVANike.cs` job class
2. Implement `IBancoModoAcreditacion` with Nike-specific logic
3. Add **NOT IN static list** in generic `AcreditarDiaADiaBBVA`: `WHERE CLIENTE NOT IN ('Nike')`
4. Register both jobs in Quartz at different times (Nike at 08:30, generic at 09:00)

**Result**: No duplicate accruals; each customer accrued exactly once per cycle

*Variant customers (Mans, RobleFuerte, RutaDoce) follow same pattern*

---

## 🗣️ Domain Vocabulary (Spanish → English)

### Core Terms
- **Acreditación** = Accreditation (payment batch file)
- **Depósito** = Deposit (individual transaction)
- **Buzón / CuentaBuzón** = Mailbox / Mailbox account (bank account)
- **Banco** = Bank
- **Cliente** = Customer/Client
- **Tanda** = Batch/Lot (payment grouping)

### Job Types
- **Día a Día** = Day-to-Day (daily accruals)
- **Punto a Punto** = Point-to-Point (on-demand)
- **Envío Masivo** = Bulk Send (weekly/monthly)
- **Envío Niveles** = Tier-based Send
- **Tarea** = Task (email notification)
- **Reporte** = Report (Excel daily summary)

### Special Terms
- **PERMAQUIN** = Scotiabank customer type (NSU tracking)
- **NSU** = Número de Secuencia Único (unique sequence number)
- **DxD** = Día a Día (Day-to-Day)
- **Feriado** = Holiday (holiday logic in BBVA, Santander)
- **NO_ENVIADO** = Not Sent flag (accrual status)

---

## ⚙️ Configuration & Build

### App.config (Main)

```xml
<add name="conexionTSD" value="Server=10.0.0.22;..." />
<add name="conexionTSD22" value="Server=10.0.0.22;..." />
<add name="conexionENCUESTA" value="..." />
<add name="conexionWebBuzones" value="10.0.0.4;" />

RuntimeMode: TEST (default) or PRODUCTION

<!-- TEST only -->
TestBasePath: C:\TAAS_TEST\
TestAllowSmtp: false
TestEmailWhitelist: dev@example.com
```

### Build & Publish

```bash
dotnet build ANS.csproj

dotnet publish ANS.csproj \
  -c Release -r win-x64

Output: TAAS.exe (~150 MB)
Type: Self-contained, single-file
```

### Database Servers

| Server | Purpose | Notes |
|--------|---------|-------|
| **TSD (10.0.0.22)** | Production data | Primary |
| **TSD22 (10.0.0.22)** | Secondary data | Backup |
| **ENCUESTA (10.0.0.4)** | Survey data | Customer surveys |
| **WEBBUZONES (10.0.0.4)** | Mailbox operations | **Source of NSU** |

*Note: No sqlcmd access from dev machine*

---

## 📋 Representative Jobs (Sample)

| Job Class | Bank | Type | Purpose |
|-----------|------|------|---------|
| **AcreditarDiaADiaBBVA** | BBVA | Day-to-Day | Daily accruals for standard BBVA customers |
| **AcreditarDiaADiaBBVANike** | BBVA | Day-to-Day (Dedicated) | Nike-only accruals; NOT IN list excludes Nike from generic job |
| **AcreditarDiaADiaSantander** | Santander | Day-to-Day | Daily accruals; may trigger WCF timeout → special error handling |
| **AcreditarDiaADiaScotiabank** | Scotiabank | Day-to-Day | Daily accruals with TXT combination by currency |
| **EnvioMasivo** | All | Bulk Send | Weekly / monthly bulk accruals across all banks |
| **ExcelBBVAReporteDiario** | BBVA | Report | Daily Excel report; uses ClosedXML |

---

## 🔄 Recent Changes & Ongoing Work

### Feature: NSU (PERMAQUIN)
- **Status**: ✅ Implemented
- **Rule**: `CC.TIPO=3` → fetch `Depositos.NSU`, store in `AcreditacionDeposito.NSU`
- **Source**: WEBBUZONES database (10.0.0.4)
- **Note**: WebBuzones team handles DDL; code side is ready

### Feature: NOMBRE_ARCHIVO
- **Status**: ✅ Implemented (BBVA, Scotia, Santander)
- **Column**: `AcreditacionDeposito.NOMBRE_ARCHIVO`
- **Method**: `CuentaBuzon.NombreArchivoGenerado`
- **Santander Special**: Uses bucket city/currency naming

### Migration: HSBC → BTG PACTUAL
- **Status**: Phase A (code) complete + backward-compatible
- **Method**: IdentidadBanco enum routing
- **Next**: Database migration (handled by another dev)
- **Reference**: MIGRACION_HSBC_A_BTG_PACTUAL.md

### Plan: Scalable DxD Scheduling
- **Goal**: Move job schedules from code → database; kill 8 dedicated jobs
- **Phase 1**: Fix NOT IN static list pattern
- **Status & Plan**: See ESTADO_Y_PLAN_HORARIOS_DXD.md
- **Benefit**: Ops can adjust job times without code redeploy

---

## 📚 Related Documentation

- **CLAUDE.md** — Complete architecture reference (patterns, runtime modes, ADO.NET guidelines)
- **ESTADO_Y_PLAN_HORARIOS_DXD.md** — DxD scheduling plan & phased rollout
- **MIGRACION_HSBC_A_BTG_PACTUAL.md** — HSBC → BTG migration notes
- **REGLAS_DE_ORO.md** — Golden rules for the system
- **Estructuras de las tablas** — Database schema definitions (DDL)

---

## 🚀 Getting Started

1. Read **CLAUDE.md** for complete patterns and architecture principles
2. Explore **Model/Services/** to understand the singleton pattern
3. Review **Model/Jobs/** to see how Quartz jobs call services
4. Check **Model/GeneradorArchivoPorBanco/** to understand bank-specific file generation
5. Study **Runtime/AppRuntime** for TEST/PRODUCTION isolation logic

---

**Last Updated**: 2026-08-04  
**Author**: Claude Code Analysis  
**Status**: Complete architecture documentation
