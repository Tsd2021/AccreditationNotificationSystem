# Runtime Mode - TEST vs PRODUCTION

## Descripción
Sistema robusto de conmutación entre modo TEST y PRODUCTION que garantiza que en modo TEST la aplicación no toque recursos de producción. En TEST, replica la estructura de PROD bajo un TestRoot configurable localmente.

## Cómo Cambiar el Modo

### Opción 1: Archivo `ans.mode` (Recomendado)
1. Crear/editar el archivo `ans.mode` en la carpeta donde está el ejecutable (misma carpeta que ANS.exe)
2. Escribir una de estas líneas:
   - `TEST` o `test` → Modo TEST
   - `PRODUCTION` o `PROD` o `production` o `prod` → Modo PRODUCTION
3. Guardar el archivo
4. Reiniciar la aplicación

**Ejemplo:**
```
C:\ANS\ANS.exe
C:\ANS\ans.mode  ← Crear este archivo con contenido "TEST" o "PRODUCTION"
```

### Opción 2: App.config
1. Abrir `App.config`
2. Agregar en la sección `<appSettings>`:
   ```xml
   <add key="RuntimeMode" value="TEST" />
   ```
   o
   ```xml
   <add key="RuntimeMode" value="PRODUCTION" />
   ```
3. Guardar y reiniciar la aplicación

### Prioridad
1. **Primero**: Archivo `ans.mode` (si existe)
2. **Segundo**: App.config `RuntimeMode`
3. **Default**: Si no hay configuración, usa **TEST** (modo seguro)

**Nota**: No hay detección automática de servidor. El control es explícito por configuración.

## Configuración Local de TestBasePath

### App.config (Recomendado)
Para personalizar la carpeta base de TEST sin recompilar:

1. Abrir `App.config`
2. Agregar en la sección `<appSettings>`:
   ```xml
   <add key="TestBasePath" value="C:\Users\dchiquiar.ABUDIL\Desktop\TAAS TEST" />
   ```

3. Si la key no existe, se usa el TestBasePath por defecto:
   ```
   C:\Users\dchiquiar.ABUDIL\Desktop\TAAS TEST
   ```

**Ejemplo completo en App.config:**
```xml
<appSettings>
  <add key="RuntimeMode" value="TEST" />
  <add key="TestBasePath" value="C:\Users\dchiquiar.ABUDIL\Desktop\TAAS TEST" />
  <!-- Configuración de SMTP en TEST (opcional) -->
  <add key="TestAllowSmtp" value="false" />
  <add key="TestEmailWhitelist" value="acreditaciones@tecnisegur.com.uy" />
  <!-- ... otras configuraciones ... -->
</appSettings>
```

### Configuración de SMTP en TEST (Opcional)

Por defecto, en modo TEST los emails se procesan localmente sin envío real por SMTP. Si necesitas enviar emails reales en TEST (útil para pruebas de integración), puedes habilitarlo con seguridad estricta:

1. Abrir `App.config`
2. Agregar en la sección `<appSettings>`:
   ```xml
   <add key="TestAllowSmtp" value="true" />
   <add key="TestEmailWhitelist" value="acreditaciones@tecnisegur.com.uy" />
   ```

**Reglas de seguridad (CRÍTICAS):**
- ✅ En TEST, TODOS los destinatarios (To/Cc/Bcc) se reemplazan por whitelist
- ✅ Si `TestAllowSmtp=true`, se permite conexión SMTP real
- ✅ ANTES de enviar, se valida que todos los destinatarios estén en whitelist
- ✅ Si algún destinatario no está en whitelist, aborta con error crítico
- ✅ Subject se modifica con prefijo `[TEST MODE]`
- ✅ Body incluye lista de destinatarios originales para auditoría

**Ejemplo de configuración para habilitar SMTP en TEST:**
```xml
<appSettings>
  <add key="RuntimeMode" value="TEST" />
  <add key="TestAllowSmtp" value="true" />
  <add key="TestEmailWhitelist" value="acreditaciones@tecnisegur.com.uy" />
</appSettings>
```

**⚠️ IMPORTANTE**: 
- `TestAllowSmtp=false` (default): SMTP bloqueado, emails procesados localmente
- `TestAllowSmtp=true`: SMTP permitido, pero SOLO si destinatarios están en whitelist
- WebServices (incluyendo Santander) SIEMPRE bloqueados en TEST (no afectados por TestAllowSmtp)

### Estructura de Rutas en TEST
El sistema replica automáticamente la estructura de PROD bajo TestRoot:

- **PROD**: `\\server\bbva\SALIDA\BBVA_20250101.txt`
- **TEST**: `C:\Users\dchiquiar.ABUDIL\Desktop\TAAS TEST\Bancos\BBVA\SALIDA\TEST_BBVA_20250101.txt`

- **PROD**: `D:\TECNISEGUR\TAAS FILES\EXCEL\Reporte.xlsx`
- **TEST**: `C:\Users\dchiquiar.ABUDIL\Desktop\TAAS TEST\Excel\TEST_Reporte.xlsx`

- **PROD**: `D:\TECNISEGUR\TAAS FILES\Logs\log.txt`
- **TEST**: `C:\Users\dchiquiar.ABUDIL\Desktop\TAAS TEST\Logs\TEST_log.txt`

- **SQLite PROD**: `C:\ANS\QuartzRuns.db`
- **SQLite TEST**: `C:\Users\dchiquiar.ABUDIL\Desktop\TAAS TEST\sqlite\ans_test.db`

## Comportamiento por Modo

### Modo TEST
- ✅ **Base de datos**: Usa connection strings de TEST (**OBLIGATORIAS**, sin fallback a PROD)
  - Mismo server/IP que PROD, pero DB/tablas de prueba (réplica)
  - SQL permitido (no se bloquea SqlConnection)
- ✅ **Tabla de acreditaciones**: Usa `AcreditacionDepositoDiegoTest_replica` (resuelto por TableNameResolver)
  - **Guardia anti-accidente**: Si se intenta usar `AcreditacionDepositoDiegoTest` (tabla PROD), aborta con error crítico
- ✅ **TestBasePath**: Carpeta base configurable (App.config key "TestBasePath" o default)
- ✅ **Archivos bancarios**: Escribe bajo `TestBasePath\Bancos\{Banco}\` replicando estructura de PROD
- ✅ **Archivos Excel**: Escribe bajo `TestBasePath\Excel\` replicando estructura de PROD
- ✅ **Logs**: Escribe bajo `TestBasePath\Logs\` replicando estructura de PROD
- ✅ **SQLite**: Crea en `TestBasePath\sqlite\ans_test.db`
- ✅ **Prefijo archivos**: Todos los archivos generados tienen prefijo `TEST_`
- ✅ **Emails**: TODOS los emails van a `acreditaciones@tecnisegur.com.uy` (ignora CCEMAIL)
  - **SMTP opcional**: Por defecto NO se conecta por SMTP (procesa localmente sin envío real)
  - **SMTP habilitado**: Si `TestAllowSmtp=true` en App.config, permite envío real pero SOLO a whitelist
  - Subject con prefijo `[TEST MODE]`
  - Body incluye lista de destinatarios originales para auditoría
- ❌ **Red bloqueada**: WebServices y HTTP siempre bloqueados (lanza excepción controlada)
- ⚠️ **SMTP condicional**: Bloqueado por defecto, habilitable con `TestAllowSmtp=true` (con validación de whitelist estricta)
- ❌ **Rutas fuera de TestBasePath**: BLOQUEADAS (whitelist estricta)

### Modo PRODUCTION
- ✅ **Base de datos**: Usa connection strings de producción (App.config normal)
- ✅ **SQLite**: Crea en carpeta del ejecutable como `QuartzRuns.db`
- ✅ **Archivos bancarios**: Escribe en shares/red configurados en App.config
- ✅ **Archivos Excel**: Escribe en ruta de producción configurada
- ✅ **Logs**: Escribe en ruta de producción configurada
- ✅ **Emails**: Comportamiento normal (usa CCEMAIL real y SMTP real)
- ✅ **WebService Santander**: Funciona normalmente

## Archivos Modificados

### Nuevos Archivos Creados
- `ANS/Runtime/RuntimeMode.cs` - Enum del modo
- `ANS/Runtime/AppSettings.cs` - Configuración tipada por ambiente
- `ANS/Runtime/AppRuntime.cs` - Clase central de runtime
- `ANS/Runtime/LocalConfig.cs` - Cargador de TestBasePath desde App.config
- `ANS/Runtime/PathResolver.cs` - Resuelve rutas replicando estructura PROD en TEST
- `ANS/Runtime/TableNameResolver.cs` - Resuelve nombres de tablas según RuntimeMode (PROD vs réplica)
- `ANS/Runtime/Guards/FileSystemGuard.cs` - Guardia de escritura de archivos
- `ANS/Runtime/Guards/WebServiceGuard.cs` - Guardia de WebServices
- `ANS/Runtime/Guards/EmailGuard.cs` - Guardia y política de emails

### Archivos Modificados
- `ANS/App.xaml.cs` - Inicializa AppRuntime y PathResolver, resuelve SQLite según modo
- `ANS/ConfiguracionGlobal.cs` - Resuelve connection strings y rutas usando PathResolver
- `ANS/Model/Services/ServicioAcreditacion.cs` - Usa TableNameResolver para todas las queries
- `ANS/Model/Services/ServicioAcreditacionManual.cs` - Usa TableNameResolver para queries
- `ANS/Model/Services/ServicioEnvioMasivo.cs` - Usa TableNameResolver, procesa emails localmente en TEST sin SMTP
- `ANS/Model/Services/ServicioCuentaBuzon.cs` - Usa TableNameResolver para todas las queries, helper para guardias en Excel
- `ANS/Model/Services/ServicioEnvioAcreditacionManual.cs` - Usa TableNameResolver para queries
- `ANS/Model/Services/ServicioSantander.cs` - Bloquea WS en TEST
- `ANS/Model/Services/ServicioEmail.cs` - Aplica política de emails, NO conecta SMTP en TEST
- `ANS/Model/GeneradorArchivoPorBanco/SantanderFileGenerator.cs` - Aplica guardias y prefijos TEST_
- `ANS/Model/GeneradorArchivoPorBanco/ScotiaFileGenerator.cs` - Aplica guardias y prefijos TEST_
- `ANS/Model/GeneradorArchivoPorBanco/BBVAFileGenerator.cs` - Usa PathResolver para rutas

## Configuración de Connection Strings de TEST (OBLIGATORIO)

⚠️ **CRÍTICO**: En modo TEST, todas las connection strings de TEST son **OBLIGATORIAS**. Si falta alguna, la aplicación **fallará al iniciar**.

Agregar en `App.config` dentro de `<connectionStrings>`:

```xml
<add name="conexionTSDTest"
     connectionString="Server=172.16.10.20;Database=TSD_TEST;User Id=tecni;Password=xxx;Encrypt=True;TrustServerCertificate=True;"
     providerName="System.Data.SqlClient" />
<add name="conexionTSD22Test"
     connectionString="Server=172.16.10.22;Database=TSD_TEST;User Id=tecni;Password=xxx;Encrypt=True;TrustServerCertificate=True;"
     providerName="System.Data.SqlClient" />
<add name="conexionENCUESTATest"
     connectionString="Server=10.0.0.4;Database=ENCUESTA_TEST;User Id=tecni;Password=xxx;Encrypt=True;TrustServerCertificate=True;"
     providerName="System.Data.SqlClient" />
<add name="conexionWebBuzonesTest"
     connectionString="Server=10.0.0.4;Database=WEBBUZONES_TEST;User Id=tecni;Password=xxx;Encrypt=True;TrustServerCertificate=True;"
     providerName="System.Data.SqlClient" />
```

**IMPORTANTE**: 
- SQL de TEST está en la misma LAN y mismo server/IP que PROD, pero usa DB/tablas de prueba (réplica)
- En TEST nunca se tocan tablas PROD
- SQL está permitido (no se bloquea SqlConnection)

## Verificación

### Al iniciar la aplicación
Buscar en los logs:
```
═══════════════════════════════════════════════════════════════
RUNTIME MODE: Test | IsTest: True | IsProduction: False
CONNECTION STRING TEST | Nombre: conexionTSDTest | Database: TSD_TEST
TABLA DE ACREDITACIONES: AcreditacionDepositoDiegoTest_Replica
EMAIL TEST | TestAllowSmtp: False | TestEmailWhitelist: acreditaciones@tecnisegur.com.uy
EffectiveTestRoot: C:\Users\dchiquiar.ABUDIL\Desktop\TAAS TEST
Rutas resueltas TEST:
  - Bancos: C:\Users\dchiquiar.ABUDIL\Desktop\TAAS TEST\Bancos\...
  - Excel: C:\Users\dchiquiar.ABUDIL\Desktop\TAAS TEST\Excel
  - Logs: C:\Users\dchiquiar.ABUDIL\Desktop\TAAS TEST\Logs
  - SQLite: C:\Users\dchiquiar.ABUDIL\Desktop\TAAS TEST\sqlite\ans_test.db
═══════════════════════════════════════════════════════════════
```

### Checklist de Pruebas

#### 1. Tabla correcta por modo (CRÍTICO)
- ✅ Verificar en logs al iniciar: "TABLA DE ACREDITACIONES: AcreditacionDepositoDiegoTest_replica"
- ✅ Ejecutar cualquier operación que inserte/consulte acreditaciones
- ✅ Verificar en SQL Server que se usa la tabla `AcreditacionDepositoDiegoTest_replica` (no la de PROD)
- ✅ Intentar hardcodear "AcreditacionDepositoDiegoTest" en código → debe fallar con error crítico en TEST

#### 2. Generar archivos bancos (TXT)
- ✅ Ejecutar acreditación de cualquier banco (Santander, Scotiabank, BBVA)
- ✅ Verificar que los archivos se crean bajo `TestBasePath\Bancos\{Banco}\`
- ✅ Verificar que replican la estructura de PROD (mismas subcarpetas)
- ✅ Verificar que tienen prefijo `TEST_` (ej: `TEST_SCOTIA_20250101.txt`)

#### 3. Generar Excel
- ✅ Ejecutar generación de Excel (reportes, tandas, etc.)
- ✅ Verificar que se crean bajo `TestBasePath\Excel\`
- ✅ Verificar que tienen prefijo `TEST_` (ej: `TEST_Santander_Henderson_Tanda_1.xlsx`)

#### 4. Logs
- ✅ Verificar que los logs se escriben bajo `TestBasePath\Logs\`
- ✅ Verificar que tienen prefijo `TEST_` si aplica

#### 5. SQLite
- ✅ Verificar que SQLite se crea en `TestBasePath\sqlite\ans_test.db`
- ✅ Verificar que NO se crea en carpeta del ejecutable

#### 6. Santander WebService bloqueado
- ✅ Intentar acreditación Santander
- ✅ Debe fallar con mensaje: "BLOQUEO EN TEST: Intento de realizar operación de red..."

#### 7. Emails override sin SMTP real
- ✅ Enviar email masivo
- ✅ Verificar en logs: "EMAIL TEST (NO ENVIADO) | Subject: [TEST MODE] ..."
- ✅ Verificar que NO se conecta por SMTP (no hay llamada a `smtp.ConnectAsync`)
- ✅ Verificar que destinatarios son solo `acreditaciones@tecnisegur.com.uy`

#### 8. Estructura replicada
- ✅ Comparar estructura de carpetas en TEST vs PROD
- ✅ Verificar que subcarpetas se replican correctamente
- ✅ Ejemplo: PROD `\\server\bbva\SALIDA` → TEST `TestBasePath\Bancos\BBVA\SALIDA`

## Troubleshooting

### Error: "ERROR CRÍTICO: Faltan connection strings de TEST"
- **Causa**: En modo TEST, todas las connection strings de TEST son OBLIGATORIAS
- **Solución**: Agregar TODAS las connection strings de TEST en App.config (conexionTSDTest, conexionTSD22Test, conexionENCUESTATest, conexionWebBuzonesTest)

### Error: "ERROR CRÍTICO DE SEGURIDAD: En modo TEST se intentó usar la tabla de PRODUCCIÓN"
- **Causa**: Se hardcodeó "AcreditacionDepositoDiegoTest" en lugar de usar `TableNameResolver.AcreditacionDeposito`
- **Solución**: Reemplazar todos los hardcodes de nombre de tabla por `TableNameResolver.AcreditacionDeposito`

### Error: "BLOQUEO EN TEST: Intento de escribir fuera de whitelist permitida"
- **Causa**: En modo TEST, solo se permite escribir bajo EffectiveTestRoot (TestBasePath)
- **Solución**: Verificar que las rutas se resuelven correctamente usando PathResolver. Verificar que TestBasePath está configurado correctamente en App.config

### Error: "BLOQUEO EN TEST: Intento de realizar operación de red (HTTP/WebService)"
- **Causa**: En modo TEST, las operaciones HTTP/WebServices están SIEMPRE bloqueadas
- **Solución**: Cambiar a PRODUCTION si necesitas probar WebServices. Los WebServices (incluyendo Santander) NO se pueden habilitar en TEST.

### Error: "BLOQUEO EN TEST: Intento de conexión SMTP"
- **Causa**: En modo TEST, SMTP está bloqueado por defecto (`TestAllowSmtp=false`)
- **Solución**: 
  - Si quieres enviar emails reales en TEST, configurar `TestAllowSmtp=true` en App.config
  - Los destinatarios siempre serán reemplazados por whitelist (`acreditaciones@tecnisegur.com.uy`)

### Error: "ERROR CRÍTICO DE SEGURIDAD: Intento de enviar email a destinatarios NO whitelist"
- **Causa**: En modo TEST con `TestAllowSmtp=true`, se detectó un intento de enviar a un email que no está en whitelist
- **Solución**: Verificar que `TestEmailWhitelist` en App.config contiene todos los emails permitidos. Por defecto debe ser `acreditaciones@tecnisegur.com.uy`

### Los archivos no tienen prefijo TEST_
- **Causa**: El método `FileSystemGuard.GetFileNameWithTestPrefix` no se está llamando
- **Solución**: Verificar que todos los puntos de escritura usan el helper

### TestBasePath no se lee de App.config
- **Causa**: La key "TestBasePath" no existe en App.config o tiene formato incorrecto
- **Solución**: 
  1. Agregar en App.config: `<add key="TestBasePath" value="C:\ruta\completa" />`
  2. Verificar que la ruta es absoluta y válida
  3. Si no existe, se usa el default: `C:\Users\dchiquiar.ABUDIL\Desktop\TAAS TEST`

### Estructura de carpetas no se replica correctamente
- **Causa**: PathResolver no puede extraer la estructura relativa de la ruta PROD
- **Solución**: Verificar que las rutas de PROD en App.config son correctas. PathResolver intenta extraer la estructura automáticamente, pero si falla usa defaultSubPath.
