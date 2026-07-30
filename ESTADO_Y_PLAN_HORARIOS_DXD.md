# Estado del proyecto y plan — Horarios DxD escalables

> **Fecha:** 2026-07-28 · **Actualizado:** 2026-07-30
> **Objetivo de la próxima sesión:** dejar de tocar código cada vez que entra un cliente día a día que no acredita a la hora estándar del banco.
>
> Documento relacionado: [`HORARIOS_DINAMICOS_PROPUESTA.md`](HORARIOS_DINAMICOS_PROPUESTA.md) — el diseño de fondo sigue siendo válido, pero **sus tablas de horarios actuales están desactualizadas**. Ver sección "Correcciones al doc previo".

---

## 0. Novedades del 2026-07-30 (cierre y commit)

Todo lo que estaba sin commitear quedó commiteado en la rama `branch_CambioNombreHSBC`.
**Build verificado antes de commitear: `ANS.sln` compila, exit code 0** (sólo warnings
preexistentes de nulabilidad y NuGet). Esto cierra la duda que dejaba la sección 2 sobre
si el reacomodo de llaves de `acreditarDiaADiaPorCliente` rompía la compilación: no la rompe.

### ⚠️ Discrepancia de ID a confirmar: RUTADOCE es 977, no 997

Este documento decía originalmente que RUTADOCE era el cliente **997**. El código, en cambio,
usa **977** de forma consistente en los dos lugares que importan:

- `ANS/Model/Jobs/BBVA/AcreditarDiaADiaBBVARutaDoce.cs:47` → `getById(977); // RUTADOCE`
- `ANS/Model/Services/ServicioCuentaBuzon.cs:443` → `NOT IN (998, 1016, 976, 977)`

Ambos archivos fueron editados el 2026-07-29, después de escrito este doc, así que **977 es
casi con seguridad la corrección** y 997 el error. Las tablas de abajo quedaron actualizadas a 977.

**Igual hay que confirmarlo contra la base**, porque es exactamente el riesgo #3 de la sección 4:
si el ID del job y el del `NOT IN` coincidieran entre sí pero **no** con el cliente real,
RUTADOCE quedaría sin excluir del genérico de las 17:00 → **doble acreditación silenciosa**.
Verificación mínima: `SELECT IdCliente, Nombre FROM <tabla clientes> WHERE IdCliente IN (976, 977, 997)`.

### Cambio adicional en `ServicioDeposito.cs` (no cubierto por el doc original)

Se agregaron excepciones que fuerzan `QueryBuscaDepositoConIgual()` para ciertos buzones:

| Banco | Empresa |
|---|---|
| Santander | `COMITAN` |
| Santander | `COMITAN SUC 71` |
| Santander | `NUMMI ANCAP RUTA DOCE` |
| BBVA | `RUTA DOCE` |

Son matcheos por **nombre de empresa en texto plano**, no por ID. Frágiles ante cualquier
cambio de razón social o espaciado en la base. Candidatos a moverse a la tabla de
configuración junto con los horarios.

---

## 1. Qué se hizo hoy

Se agregaron dos clientes BBVA día a día con hora dedicada, siguiendo el patrón Nike/Mans.

| Cliente | IdCliente | Banco | Cron | Hora |
|---|---|---|---|---|
| ROBLEFUERTE | 976 | BBVA | `0 15 14 ? * MON-FRI` | 14:15:00 |
| RUTADOCE | 977 | BBVA | `0 15 14 ? * MON-FRI` | 14:15:00 |

**Archivos nuevos**
- `ANS/Model/Jobs/BBVA/AcreditarDiaADiaBBVARobleFuerte.cs`
- `ANS/Model/Jobs/BBVA/AcreditarDiaADiaBBVARutaDoce.cs`

**Archivos modificados**
- `ANS/Model/Jobs/MyJobFactory.cs` — dos cases nuevos en la region `JOBS_QUE_ACREDITAN` de BBVA
- `ANS/App.xaml.cs` — regions `TAREA_ACREDITAR_DIAADIA_ROBLEFUERTE` / `_RUTADOCE` + sus dos `ScheduleJob`
- `ANS/Model/Services/ServicioCuentaBuzon.cs` — `NOT IN (998, 1016)` → `NOT IN (998, 1016, 976, 977)`

**Decisiones tomadas**
- Límite de acreditación = **hora de cierre de cada buzón** (`cu.Cierre`). Los jobs pasan `TimeSpan.Zero`.
- Un job dedicado por cliente (no uno compartido).
- Sin cambios en `BBVAFileGenerator`: los TXT salen con formato y nomenclatura de siempre.

**Estado:** compila (0 errores). ✅ **Commiteado el 2026-07-30** en `branch_CambioNombreHSBC`.

### Pendientes de esta tanda

- [ ] **Confirmar que RUTADOCE es el cliente 977** (ver sección 0). Es el pendiente más urgente: si el ID está mal, hay doble acreditación silenciosa.
- [ ] **Los tres jobs de las 14:15 disparan en el mismo minuto** (Mans, ROBLEFUERTE, RUTADOCE). `DisallowConcurrentExecution` es por clase, así que corren en paralelo → tres generaciones de TXT BBVA simultáneas. Evaluar escalonar a `:15` / `:16` / `:17`.
- [ ] **Verificar que `cc.CIERRE` no sea NULL** para los buzones de 976 y 977. Si es NULL, `horaCierreAUsar` queda en `TimeSpan.Zero` y `ServicioDeposito.cs:31` cae en la rama **sin corte**: acredita todo lo pendiente, en silencio, sin log. Considerar agregar un `WriteInfo` en ese caso.
- [x] ~~Commitear.~~ Hecho el 2026-07-30.
- [ ] Actualizar `ANS/CLAUDE.md` — sigue diciendo que `TimeSpan.Zero` es "sin corte salvo 998/1014". Ya no es cierto (ver sección 2).
- [ ] Limpiar la indentación de `acreditarDiaADiaPorCliente`. Compila, pero el anidado que se lee no es el real.

---

## 2. Cambio detectado en `acreditarDiaADiaPorCliente` (no hecho por Claude)

Durante la sesión, `ANS/Model/Services/ServicioCuentaBuzon.cs` cambió: **se eliminó el gate** `if (cli.IdCliente == 998 || cli.IdCliente == 1014)` y su rama `else`.

**Antes:** `TimeSpan.Zero` = query sin corte, **salvo** Nike (998) y Uruimporta (1014), que usaban `cu.Cierre`.
**Ahora:** `TimeSpan.Zero` = **todos** los clientes con job dedicado usan `cu.Cierre` (`ServicioCuentaBuzon.cs:1415-1417`).

Consecuencias:

- Es el comportamiento deseado para 976 y 977.
- **Cambia el comportamiento de Mans (1016)**, que antes acreditaba sin corte y ahora corta en el cierre de cada buzón. Confirmar que es lo buscado.
- Las llaves quedaron corridas: `if (cu.Depositos != null ...)` quedó fuera de `if (ultIdOperacion > 0)`, y `if (cuentasConDepositos.Count == 0)` fuera del `if (cuentaBuzones != null)`. **Compila y el null-check lo salva**, pero la indentación no refleja el anidado real. Vale un cleanup.
- **`ANS/CLAUDE.md` quedó desactualizado.** La sección "Jobs día a día dedicados por cliente" todavía dice: *"`TimeSpan.Zero` = query sin corte, SALVO clientes en la rama gated (998 Nike, 1014 Uruimporta)"*. Ya no es cierto. **Actualizar.**

Este cambio simplifica el diseño futuro: si el límite es **siempre** el cierre del buzón, la tabla de horarios solo necesita guardar la **hora de disparo**, no el corte.

---

## 3. Inventario real de jobs de acreditación (verificado hoy en `App.xaml.cs`)

> Cron Quartz = `seg min hora ? * DOW`.

### BBVA — `crearJobsBBVA`

| Job | Cliente | Cron | Hora |
|---|---|---|---|
| `AcreditarDiaADiaBBVAJob` | genérico | `45 0 17` | 17:00:45 |
| `AcreditarDiaADiaBBVANike` | 998 | `0 2 7` | 07:02:00 |
| `AcreditarDiaADiaBBVAMans` | 1016 | `0 15 14` | 14:15:00 |
| `AcreditarDiaADiaBBVARobleFuerte` | 976 | `0 15 14` | 14:15:00 |
| `AcreditarDiaADiaBBVARutaDoce` | 977 | `0 15 14` | 14:15:00 |

Exclusión del genérico: `cb.IDCLIENTE NOT IN (998, 1016, 976, 977)`

### Scotiabank — `crearJobsScotiabank`

| Job | Cliente | Cron | Hora |
|---|---|---|---|
| `AcreditarDiaADiaScotiabank` | genérico | `36 01 17` | 17:01:36 |
| `AcreditarDiaADiaAbastoElPlacer` | 1015 | `0 2 7` | 07:02:00 |
| `AcreditarDiaADiaUruimporta` | 1014 | `0 3 7` | 07:03:00 |
| `AcreditarDiaADiaFarmashop` | 164/179 | — | **comentado/deshabilitado** |
| `AcreditarTanda1HendersonScotiabank` | Henderson | `0 6 7` | 07:06:00 |
| `AcreditarTanda2HendersonScotiabank` | Henderson | `0 52 14` | 14:52:00 |

Exclusión del genérico: `cb.idcliente not in (164, 179, 1014)`

⚠️ **Abasto El Placer (1015) NO está excluido, a propósito**: tiene job dedicado de mañana **y además** entra en el DxD genérico de la tarde. No es un caso "cliente a otra hora" sino "cliente dos veces por día". El diseño nuevo tiene que soportarlo.

### Santander — `crearJobsSantander`

| Job | Cliente | Cron | Hora |
|---|---|---|---|
| `AcreditarDiaADiaSantander` | genérico | `0 50 15` | 15:50:00 |
| `AcreditarDiaADiaSantanderDeLasSierras` | 268 | `0 4 7` | 07:04:00 |
| `AcreditarTanda1SantanderHenderson` | Henderson | `0 5 7` | 07:05:00 |
| `AcreditarTanda2SantanderHenderson` | Henderson | `0 50 14` | 14:50:00 |

Exclusión del genérico: `cb.IDCLIENTE NOT IN (268)`

**Total: 8 jobs dedicados**, ~130 líneas de copy-paste cada uno.

---

## 4. Qué está hardcodeado (el problema, en concreto)

| # | Qué | Dónde | Riesgo si se olvida |
|---|---|---|---|
| 1 | Hora de disparo | cron en `App.xaml.cs` | el cliente no acredita |
| 2 | El cliente | `getById(976)` dentro de la clase job | — |
| 3 | Exclusión del run genérico | `NOT IN (...)` en `ServicioCuentaBuzon.getAllByTipoAcreditacionYBanco` | **doble acreditación** |
| 4 | Registro en el factory | `MyJobFactory.cs` | el job tira excepción al dispararse |

El **#3 es el peligroso**: es silencioso y produce plata acreditada dos veces.

---

## 5. Correcciones al doc previo (`HORARIOS_DINAMICOS_PROPUESTA.md`)

El diseño (dos tablas + job genérico parametrizado por `(banco, hora)`) es correcto y se mantiene. Lo que hay que corregir antes de usarlo como base:

| Dice | Real |
|---|---|
| BBVA DxD `14:30` | `17:00:45` |
| Scotiabank DxD `16:10` | `17:01:36` |
| Santander DxD `07:15` | `15:50:00` |
| Nike (998) hora especial `14:25` | `07:02:00` |
| DeLasSierras (268) `07:04` | ✅ correcto |
| Farmashop (164,179) `06:58` | job **comentado**, hoy no corre |
| Tabla de clientes especiales: 3 filas | son **8 jobs dedicados** (falta Mans, Abasto, Uruimporta, + los 2 nuevos) |

Falta además contemplar el caso **Abasto El Placer**: un cliente puede necesitar **dos** disparos por día (mañana dedicado + tarde genérico). El esquema `HorarioConfigAcreditacion` ya lo soporta (N filas por config), pero la query de exclusión tiene que tenerlo en cuenta: no alcanza con "si tiene override, excluilo del genérico".

---

## 6. Plan propuesto para mañana

### Fase 0 — Cerrar lo de hoy (30 min)
1. Decidir si se escalonan los tres jobs de las 14:15.
2. Verificar `cc.CIERRE` de los buzones de 976 y 977.
3. Actualizar `ANS/CLAUDE.md` (semántica de `TimeSpan.Zero`).
4. Limpiar la indentación de `acreditarDiaADiaPorCliente`.
5. Commitear.

### Fase 1 — Matar el `NOT IN` hardcodeado (el cambio de mayor valor / menor riesgo)

Es independiente del resto y se puede desplegar solo.

1. Crear la tabla de horarios y cargarla con los 8 jobs actuales.
2. Reemplazar en `getAllByTipoAcreditacionYBanco`:

```sql
AND cb.IDCLIENTE NOT IN (
    SELECT IdCliente FROM HorarioAcreditacion
    WHERE Banco = @bank AND TipoAcreditacion = @tipoAcreditacion AND Activo = 1)
```

3. **Validar en TEST** que la query nueva selecciona exactamente los mismos buzones que la vieja, banco por banco, antes de tocar producción.

Resultado: agregar un cliente ya no puede causar doble acreditación por olvido.

### Fase 2 — Job genérico parametrizado

`AcreditarDiaADiaPorHorario : IJob` lee `banco` y `hora` del `JobDataMap`, busca los clientes de esa `(banco, hora)` y llama `acreditarDiaADiaPorCliente(cli, banco, TimeSpan.Zero)` **en secuencia**.

- Un solo case en `MyJobFactory`.
- **Sigue siendo una llamada por cliente** → los TXT salen idénticos (un archivo por cliente, misma nomenclatura). No se toca la regla de oro.
- Al ser secuencial desaparece el problema de N generaciones en paralelo.

### Fase 3 — Agendado dinámico al arrancar

`foreach` sobre las filas activas armando **un trigger por `(banco, hora)`** — no por cliente. 5 clientes a las 12:30 = 1 trigger + 5 acreditaciones secuenciales.

Alta de cliente nuevo = un `INSERT` + reiniciar la app. Cero deploy.

### Fase 4 — Migración banco por banco

Empezar por **BBVA** (más casos, sin WS de por medio), después Scotiabank, después Santander. Borrar cada clase dedicada solo cuando su fila esté cargada y validada. **No big-bang.**

### Fase 5 — Opcional
- Job `RefrescarHorarios` (ej. 00:30) que re-sincroniza triggers sin reiniciar, o botón en la UI.
- Derivar la hora de disparo de `MAX(cc.CIERRE) + margen` en lugar de cargarla a mano.

---

## 7. Fuera de alcance

Los jobs dedicados que **no** son "el mismo trabajo a otra hora" sino **otra lógica** siguen necesitando clase propia:

- **Henderson Tanda1/Tanda2** (Scotia y Santander) — otro tipo de acreditación.
- **Uruimporta (1014)** — archivo TXT propio con sufijo `_Uruimporta` + exclusión del combinador de las 17:10.
- **DeLasSierras (268)** — revisar si tiene lógica propia o es solo horario.

La tabla resuelve el caso frecuente (976, 977, 1016, 998), que es el que obliga a tocar código.

---

## 8. Reglas a respetar

- **Regla de oro:** no cambiar layout, nombre ni nomenclatura de los TXT por banco sin permiso explícito del dueño. La Fase 2 está diseñada para no tocarlos (una llamada por cliente).
- **No duplicar acreditaciones:** cualquier cambio en las queries de selección se valida en TEST comparando el set de buzones contra la query actual.
- **Aislamiento TEST/PROD:** usar siempre `TableNameResolver.AcreditacionDeposito`, nunca hardcodear la tabla.
- **ADO.NET parametrizado:** sin concatenación de SQL.

---

## 9. Preguntas abiertas

1. ¿La hora de disparo se carga a mano o se deriva de `cc.CIERRE`?
2. ¿La tabla va por **cliente** (`IdCliente`) o por **cuenta/config** (`ConfigAcreditacionId`)? El doc previo propone por config (más fino); los jobs actuales trabajan por cliente. **Por config es más flexible pero migra peor** desde el estado actual.
3. ¿Se acepta reiniciar la app para tomar un horario nuevo, o hace falta recarga en caliente desde el arranque?
4. ¿Qué pasa con los **Excel encadenados** (hoy hay un job de Excel 1 minuto después de cada acreditación)? Si las horas son dinámicas, esos triggers también tienen que serlo.
5. ¿Confirmás que el cambio de Mans (1016) de "sin corte" a "corte en `cu.Cierre`" es intencional?
