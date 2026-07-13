# Migración de banco: HSBC → BTG PACTUAL

Documento de coordinación entre el **cambio de código (Etapa A, ya implementado)** y la
**migración de base de datos (Etapa B, a cargo del DBA / otro programador)**.

El banco antes identificado como `HSBC` pasa a llamarse `BTG PACTUAL`. El cambio se diseñó como
una **migración coordinada y retrocompatible**: código y base de datos **no** tienen que
desplegarse en el mismo instante.

> ⚠️ **NO ejecutar los `UPDATE` de este documento sin verificar antes** con las consultas de
> diagnóstico. Todo el SQL de escritura es **propuesta para revisión**, no para correr a ciegas.

---

## 1. Qué hace el código después de la Etapa A

- **Identidad canónica en memoria:** el banco se siembra como `BTG PACTUAL` (`App.xaml.cs`,
  `BancoId = 3` sin cambios).
- **Normalización central:** `ANS/Model/IdentidadBanco.cs` mapea `HSBC` y `BTG PACTUAL`
  (en cualquier caja / con espacios) al **mismo banco lógico**.
- **Retrocompatibilidad en las queries de selección de cuentas:** los filtros por banco pasaron de
  `cb.BANCO = @banco` a `UPPER(LTRIM(RTRIM(cb.BANCO))) IN (@banco, @bancoAlias)`, con
  `@bancoAlias = 'HSBC'`. Es decir, el código selecciona correctamente las cuentas del banco
  **tanto si `cb.BANCO` vale `'HSBC'` como si vale `'BTG PACTUAL'`** (y tolera espacios/caja).
- **Visible al usuario:** UI, emails y mensajes muestran `BTG PACTUAL`.

### Contrato explícito

| Estado de la BD | ¿El código funciona? | Por qué |
|-----------------|----------------------|---------|
| `cb.BANCO = 'HSBC'` (sin migrar) | ✅ Sí | El `IN` incluye el alias `'HSBC'` |
| `cb.BANCO = 'BTG PACTUAL'` (migrada) | ✅ Sí | `@banco` = `'BTG PACTUAL'` |
| Mezcla `'HSBC'` + `'BTG PACTUAL'` durante la ventana | ✅ Sí, **sin duplicar** | Ver §2 |

**No hay riesgo de doble acreditación por el rename:** el guard anti-duplicados de la tabla de
acreditaciones es `IF NOT EXISTS (IDBUZON + IDOPERACION + MONEDA + IDCUENTA)` — **no incluye el
nombre del banco**. Cambiar el nombre no puede generar una segunda acreditación del mismo depósito.

---

## 2. Riesgo de duplicados a vigilar (no por acreditación, por configuración)

El único duplicado posible es **de configuración**: que la **misma cuenta/buzón** quede representada
dos veces, una con `BANCO='HSBC'` y otra con `BANCO='BTG PACTUAL'`. Eso NO lo crea el código; lo
crearía una migración parcial/inconsistente. Por eso el DBA debe verificar duplicados **antes** y
**después** (consultas en §4).

---

## 3. Objetos de base de datos a revisar

El repo solo evidencia con certeza dos columnas, pero **no asumir que son las únicas**. El DBA debe
barrer el esquema (consulta 4.1) para descubrir todos los lugares reales.

| Tipo | Objeto | Columna | Evidencia en el repo |
|------|--------|---------|----------------------|
| Tabla | `cuentasbuzones` | `BANCO` | `WHERE cb.BANCO ...` en `ServicioCuentaBuzon` |
| Tabla | `cc` | `BANCO` | `c.BANCO as BANCOBUZON` (banco del buzón; **ver nota**) |
| ¿Tabla/otro? | *(a descubrir)* | — | barrer con 4.1 |

> **Nota sobre `cc.BANCO`:** puede contener `CASHOFFICE` u otros valores además del banco de la
> cuenta. Revisar antes de actualizar: **solo** deben migrarse las filas cuyo valor sea el banco
> (`HSBC`), no las de `CASHOFFICE`.

**Objetos que el DBA debe chequear explícitamente** (no visibles desde el repo): vistas, funciones,
stored procedures, triggers, constraints/CHECK sobre el nombre del banco, índices sobre la columna
`BANCO`, y **jobs del SQL Server Agent** que filtren por `'HSBC'`.

---

## 4. Consultas de diagnóstico (NO destructivas) — correr ANTES

### 4.1 Descubrir TODAS las columnas de texto que contienen 'HSBC'
```sql
-- Genera los SELECT de conteo por cada columna de texto del esquema.
-- Revisar el resultado y ejecutar los SELECT que interesen (no corre nada por sí solo).
SELECT
    'SELECT ''' + s.name + '.' + t.name + '.' + c.name + ''' AS Ubicacion, COUNT(*) AS N '
    + 'FROM [' + s.name + '].[' + t.name + '] WHERE [' + c.name + '] LIKE ''%HSBC%'';' AS SqlDiagnostico
FROM sys.columns c
JOIN sys.tables t  ON t.object_id = c.object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.types ty  ON ty.user_type_id = c.user_type_id
WHERE ty.name IN ('varchar','nvarchar','char','nchar','text','ntext');
```

### 4.2 Buscar referencias 'HSBC' en el código de objetos programables (vistas/SP/funciones/triggers)
```sql
SELECT o.type_desc, s.name AS esquema, o.name AS objeto
FROM sys.sql_modules m
JOIN sys.objects o ON o.object_id = m.object_id
JOIN sys.schemas s ON s.schema_id = o.schema_id
WHERE m.definition LIKE '%HSBC%';
```

### 4.3 Conteo y variantes de escritura (caja / espacios) en las columnas conocidas
```sql
SELECT 'cuentasbuzones.BANCO' AS Col, '[' + BANCO + ']' AS ValorEntreCorchetes, COUNT(*) AS N
FROM cuentasbuzones
WHERE UPPER(LTRIM(RTRIM(BANCO))) IN ('HSBC','BTG PACTUAL')
GROUP BY '[' + BANCO + ']'
UNION ALL
SELECT 'cc.BANCO', '[' + BANCO + ']', COUNT(*)
FROM cc
WHERE UPPER(LTRIM(RTRIM(BANCO))) IN ('HSBC','BTG PACTUAL')
GROUP BY '[' + BANCO + ']'
ORDER BY 1, 2;
```
Esto revela valores como `Hsbc`, `hsbc`, `HSBC ` (con espacio final), etc.

### 4.4 Detectar el duplicado de configuración (misma cuenta con ambos nombres)
```sql
-- Cuentas cuyo par (idcliente, cuenta, moneda) aparece con HSBC y con BTG PACTUAL a la vez.
SELECT IDCLIENTE, CUENTA, MONEDA, COUNT(DISTINCT UPPER(LTRIM(RTRIM(BANCO)))) AS NombresDistintos
FROM cuentasbuzones
WHERE UPPER(LTRIM(RTRIM(BANCO))) IN ('HSBC','BTG PACTUAL')
GROUP BY IDCLIENTE, CUENTA, MONEDA
HAVING COUNT(DISTINCT UPPER(LTRIM(RTRIM(BANCO)))) > 1;
```
Si devuelve filas → **resolver la inconsistencia antes** de migrar (no debería haber duplicados).

---

## 5. SQL de migración (PROPUESTA — revisar y ejecutar en transacción)

Ejecutar **dentro de una transacción**, revisando `@@ROWCOUNT` contra lo esperado del diagnóstico.

```sql
BEGIN TRAN;

-- Normaliza a 'BTG PACTUAL' cualquier variante de HSBC en la columna de banco de la CUENTA.
UPDATE cuentasbuzones
SET BANCO = 'BTG PACTUAL'
WHERE UPPER(LTRIM(RTRIM(BANCO))) = 'HSBC';

-- Banco del BUZÓN: solo filas que representan el banco (NO tocar 'CASHOFFICE' u otros).
UPDATE cc
SET BANCO = 'BTG PACTUAL'
WHERE UPPER(LTRIM(RTRIM(BANCO))) = 'HSBC';

-- Revisar @@ROWCOUNT y las verificaciones de §6 ANTES de confirmar.
-- COMMIT;   -- descomentar solo si todo cuadra
-- ROLLBACK; -- si algo no cuadra
```

> Agregar aquí los `UPDATE` de cualquier otra columna que aparezca en 4.1/4.2 (vistas indexadas,
> tablas de parámetros, catálogos, etc.). No incluidos porque no hay evidencia en el repo.

---

## 6. Verificación POSTERIOR (Etapa C)

```sql
-- No deben quedar registros ACTIVOS con 'HSBC' (salvo históricos que se decidan conservar).
SELECT 'cuentasbuzones' AS Tabla, COUNT(*) AS ResiduoHSBC FROM cuentasbuzones WHERE UPPER(LTRIM(RTRIM(BANCO))) = 'HSBC'
UNION ALL
SELECT 'cc', COUNT(*) FROM cc WHERE UPPER(LTRIM(RTRIM(BANCO))) = 'HSBC';
-- Esperado: 0 en ambas.

-- Confirmar que las cuentas ahora figuran como BTG PACTUAL.
SELECT COUNT(*) AS CuentasBTG FROM cuentasbuzones WHERE UPPER(LTRIM(RTRIM(BANCO))) = 'BTG PACTUAL';
```

Verificación funcional en la app (modo TEST, skill `run-ans`): correr DXD y Excel del banco y
confirmar que selecciona las mismas cuentas y muestra `BTG PACTUAL`.

---

## 7. Orden recomendado de despliegue

1. **Desplegar el código (Etapa A).** Funciona con la BD **sin migrar** (alias `HSBC`).
2. Correr diagnóstico (§4) y resolver duplicados si los hay.
3. **Migrar la BD (Etapa B, §5)** en ventana controlada, dentro de transacción.
4. Verificar (§6).
5. Más adelante, **Etapa D:** retirar el alias legado (ver §9).

El orden inverso (BD primero, código después) también es seguro: la versión vieja del código
selecciona por `'HSBC'` exacto y dejaría de ver las cuentas migradas → por eso **conviene código
primero**. Con el código nuevo, cualquier orden funciona.

---

## 8. Rollback

- **Código:** revertir el commit/PR de la Etapa A. La app vuelve a operar solo con `HSBC`; requiere
  que la BD **no** esté migrada (o volver a `HSBC`, abajo).
- **Base de datos** (si se confirmó y hay que volver atrás):
```sql
BEGIN TRAN;
UPDATE cuentasbuzones SET BANCO = 'HSBC' WHERE UPPER(LTRIM(RTRIM(BANCO))) = 'BTG PACTUAL';
UPDATE cc            SET BANCO = 'HSBC' WHERE UPPER(LTRIM(RTRIM(BANCO))) = 'BTG PACTUAL';
-- COMMIT; / ROLLBACK;
```
- Como el **código nuevo tolera ambos nombres**, un rollback solo de BD (a `HSBC`) **no rompe** la
  app: es la red de seguridad principal.

---

## 9. Etapa D — Retiro futuro del alias `HSBC`

Recién cuando se confirme que: la BD quedó migrada, no hay configuraciones externas ni jobs de SQL
Agent con `'HSBC'`, no hay versiones viejas de la app conectadas, y los logs históricos ya no
requieren interpretarse. Entonces retirar:

- En `IdentidadBanco`: los `case "HSBC"` de `Normalizar` y la rama de `AliasLegado`.
- En `ServicioCuentaBuzon`: el segundo término `@bankAlias` / `@bancoAlias` de los `IN` (volver a `=`).
- En `ServicioAcreditacionManual`: la fila `(5, 'HSBC')` del `VALUES`.
- En `ServicioLog`: el token `"HSBC"` del clasificador.
- En `VariablesGlobales`: la const `hsbc` (y el arm `VariablesGlobales.hsbc` de `BankFactory`).
- En `BancoModal` / `SeleccionDeBanco.xaml`: opcionalmente renombrar el `Tag="Hsbc"` de ruteo.

**Fuera de alcance de código (pendiente de diseño):** el logo `Images/hsbc.png` sigue siendo el
activo visual del banco; reemplazarlo por el de BTG PACTUAL es una tarea de assets, no de código.
