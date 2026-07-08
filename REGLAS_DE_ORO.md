# Reglas de oro — ANS / TAAS

> Reglas innegociables de la operación. Esta app **acredita dinero real y manda
> archivos/emails reales a bancos**. El incumplimiento puede rechazar un archivo
> en el banco, sub-acreditar dinero o dañar la relación con el banco.

## 1. Nunca cambiar formatos de TXT ni nombres de archivo sin permiso del dueño

**JAMÁS** modificar, sin autorización explícita del dueño (Diego):

- El **layout de la línea** de ancho fijo de los archivos por banco: posiciones de
  campos, longitudes, padding, ancho total (p.ej. Scotiabank = 875 chars).
- El **cálculo del importe** (centavos, pad-left) o su posición (p.ej. `substring(52,15)`).
- Los **prefijos de cuenta** (p.ej. `2101` CtaCte / `2201` CajaAhorro), signos, códigos de moneda.
- El **encoding** de escritura de los archivos.
- Los **nombres de archivo** y la **nomenclatura / sufijos**: `AcreditacionTecnisegur`,
  `AcreditacionBuzonesTecnisegur`, `_DiaADia`, `_Tanda1`, `_Tanda2`, `_Farmashop`,
  `_AbastoElPlacer`, `Mont` / `Mald`, `_Excepciones`, etc.

**Por qué:** el banco parsea estos archivos por ancho fijo y los detecta/combina por
nombre y sufijo. Un cambio de formato o de nombre puede hacer que el banco rechace o
procese mal el archivo, o que el combinado no encuentre los archivos → sub-acreditar
dinero real.

**Cómo se aplica:**

- **Leer / analizar / auditar** la generación de archivos → permitido, sin pedir permiso.
- **Editar cualquier cosa que afecte los bytes del TXT o el nombre del archivo** →
  frenar y pedir autorización explícita, **aunque parezca un bugfix obvio**.
- Si se detecta un bug de formato → **reportarlo citando `archivo:línea` y esperar
  luz verde**; no corregirlo por cuenta propia.

**Alcance:** `ScotiaFileGenerator`, `SantanderFileGenerator`, generadores BBVA,
`ServicioCombinarTxtScotiabank` y cualquier otro generador/combinador por banco.

### Qué SÍ es formato (protegido) y qué NO

La regla protege el **formato/bytes de la línea**, no la cantidad de líneas ni
cómo se agrupan los importes.

- **Protegido (no tocar sin permiso):** ancho de línea, posiciones de campos,
  espacios/padding, cálculo y ancho del importe, prefijos y armado de cuenta,
  encoding, nombres y sufijos de archivo.
- **NO es violación de formato:** agregar/quitar líneas, o cambiar la
  granularidad de agrupación (p.ej. una línea por buzón en vez de una por
  cuenta), **siempre que cada línea conserve el mismo layout byte a byte**. Eso
  es un cambio de lógica/negocio y se decide con el dueño como cualquier cambio
  funcional, pero no rompe el formato del archivo.
