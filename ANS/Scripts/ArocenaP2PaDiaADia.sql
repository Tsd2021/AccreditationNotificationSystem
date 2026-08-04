-- ============================================================================
-- AROCENA SRL (IdCliente 732) - BBVA: pasar de PuntoAPunto a DiaADia
--
-- Correr MANUALMENTE. Base TSD (Conexion22).
--
-- CONTEXTO
-- AROCENA acredita a la cuenta 27012913, la misma que el resto de los clientes
-- dedicados de BBVA (Nike, Mans, ROBLEFUERTE, RUTADOCE). Hasta ahora lo tomaba
-- el job P2P de BBVA, que corre cada 30 minutos, y sus depositos quedaban
-- repartidos en varios REME del dia. Pasa a tener job dedicado a las 14:21 y
-- salir en un solo archivo diario.
--
-- Buzon: EA22L0315N12000051  'ANCAP AROCENA (spectro6)'
-- Cuentas: 27012913-000 (PESOS, CuentasBuzonesId 557)
--          27012913-001 (DOLARES, CuentasBuzonesId 558)
-- Cierre del buzon: 14:00
--
-- ----------------------------------------------------------------------------
-- ORDEN DE DESPLIEGUE - IMPORTANTE
--
-- Correr este script DESPUES de publicar el binario con el job nuevo, no antes.
--
--   1) Publicar TAAS con AcreditarDiaADiaBBVAArocena y con 732 en
--      VariablesGlobales.clientesDxDDedicadosBBVA.
--   2) Recien ahi correr este script.
--
-- Por que ese orden: en cuanto las configs pasan a 'DiaADia', el job P2P deja
-- de tomarlas. Si el binario nuevo todavia no esta publicado, no existe el job
-- de las 14:21 y AROCENA no acreditaria por ningun lado.
--
-- Si se corre en el orden inverso hay una ventana en la que AROCENA no acredita.
-- No se pierde plata (los depositos quedan pendientes y entran en la primera
-- corrida que si lo tome), pero se retrasa.
--
-- ----------------------------------------------------------------------------
-- QUE CAMBIA EN EL ARCHIVO QUE VE EL BANCO
--
-- El layout de cada linea NO cambia. Lo que cambia es la granularidad:
--   ANTES (P2P, Exporta_Reme):          una linea por deposito,
--                                       remito = IdReferencia + 'X' + IdOperacion
--   AHORA (DiaADia, Exporta_Reme_Agrupado): una linea por cuenta con el total,
--                                       remito = prefijo(4) + HHmmssff
--
-- Es el mismo formato que ya usan Mans, ROBLEFUERTE y RUTADOCE.
-- Autorizado por el dueño el 2026-08-04.
-- ============================================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

PRINT '=== ANTES ===';
SELECT ca.ConfigId, ca.NC, ca.CuentasBuzonesId, ca.TipoAcreditacion,
       cb.CUENTA, cb.MONEDA, UPPER(LTRIM(RTRIM(cb.BANCO))) AS Banco
FROM ConfiguracionAcreditacion ca
INNER JOIN CUENTASBUZONES cb ON ca.CuentasBuzonesId = cb.ID
WHERE cb.IDCLIENTE = 732
ORDER BY ca.CuentasBuzonesId;
GO

-- Filtra por cliente + banco (no por CuentasBuzonesId literal) para que siga
-- siendo correcto si mañana le agregan otra cuenta al mismo cliente.
UPDATE ca
SET TipoAcreditacion = 'DiaADia'
FROM ConfiguracionAcreditacion ca
INNER JOIN CUENTASBUZONES cb ON ca.CuentasBuzonesId = cb.ID
WHERE cb.IDCLIENTE = 732
  AND UPPER(LTRIM(RTRIM(cb.BANCO))) = 'BBVA'
  AND ca.TipoAcreditacion <> 'DiaADia';
GO

PRINT '';
PRINT '=== DESPUES (las 2 configs deben decir DiaADia) ===';
SELECT ca.ConfigId, ca.NC, ca.CuentasBuzonesId, ca.TipoAcreditacion,
       cb.CUENTA, cb.MONEDA, UPPER(LTRIM(RTRIM(cb.BANCO))) AS Banco
FROM ConfiguracionAcreditacion ca
INNER JOIN CUENTASBUZONES cb ON ca.CuentasBuzonesId = cb.ID
WHERE cb.IDCLIENTE = 732
ORDER BY ca.CuentasBuzonesId;
GO

PRINT '';
PRINT '=== CHEQUEO: AROCENA NO debe aparecer en el run generico de las 17:00:45 ===';
PRINT '(debe devolver CERO filas; si devuelve algo, falta 732 en';
PRINT ' VariablesGlobales.clientesDxDDedicadosBBVA y se acreditaria dos veces)';
SELECT DISTINCT c.NC, cb.ID AS IdCuenta, cb.MONEDA
FROM ConfiguracionAcreditacion config
INNER JOIN CUENTASBUZONES cb ON config.CuentasBuzonesId = cb.ID
INNER JOIN CC c ON cb.IDCLIENTE = c.IDCLIENTE AND c.NC = config.NC
WHERE UPPER(LTRIM(RTRIM(cb.BANCO))) = 'BBVA'
  AND config.TipoAcreditacion = 'DiaADia'
  AND cb.IDCLIENTE NOT IN (998, 1016, 976, 977, 732)   -- lista del codigo
  AND cb.IDCLIENTE = 732;
GO

-- ============================================================================
-- ROLLBACK (si hay que volver atras)
--
-- UPDATE ca SET TipoAcreditacion = 'PuntoAPunto'
-- FROM ConfiguracionAcreditacion ca
-- INNER JOIN CUENTASBUZONES cb ON ca.CuentasBuzonesId = cb.ID
-- WHERE cb.IDCLIENTE = 732 AND UPPER(LTRIM(RTRIM(cb.BANCO))) = 'BBVA';
--
-- Y sacar 732 de VariablesGlobales.clientesDxDDedicadosBBVA + republicar,
-- o el cliente queda excluido del generico sin tener job propio activo.
-- ============================================================================
