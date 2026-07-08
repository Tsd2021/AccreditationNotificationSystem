-- ============================================================================
-- Feature PERMAQUIN / NSU
-- Agrega la columna NSU (INT NULL) a las tablas de acreditaciones.
--
-- ALCANCE DE ESTE SCRIPT: lado ACREDITACIONES (base TSD / Conexion22).
-- Correr MANUALMENTE, ANTES de desplegar o ejecutar el nuevo cdigo.
-- Si el binario corre sin esta columna, el INSERT rompe con:
--     "Invalid column name 'NSU'".
--
-- Nombres reales (ver TableNameResolver.AcreditacionDeposito):
--     PRODUCTION -> AcreditacionDepositoDiegoTest          (singular)
--     TEST       -> AcreditacionDepositoDiegoTest_Replica
-- Ejecutar el ALTER que corresponda al/los ambiente(s) donde vayas a probar.
-- ============================================================================

-- Tabla principal (PRODUCTION)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('AcreditacionDepositoDiegoTest') AND name = 'NSU'
)
BEGIN
    ALTER TABLE AcreditacionDepositoDiegoTest ADD NSU INT NULL;
END
GO

-- Rplica de TEST
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('AcreditacionDepositoDiegoTest_Replica') AND name = 'NSU'
)
BEGIN
    ALTER TABLE AcreditacionDepositoDiegoTest_Replica ADD NSU INT NULL;
END
GO

-- ============================================================================
-- FUERA DE ALCANCE (lo ejecuta el equipo responsable de WebBuzones):
--   Base WebBuzones (_conexionWebBuzones), tabla Depositos:
--
--   ALTER TABLE Depositos ADD NSU INT NULL;
--
-- El cdigo asume que Depositos.NSU ya existir al momento de probar.
-- ============================================================================
