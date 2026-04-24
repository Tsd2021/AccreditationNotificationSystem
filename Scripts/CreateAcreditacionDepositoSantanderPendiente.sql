-- Tabla auxiliar: acreditaciones Santander cuando el WS no confirma éxito (error explícito, timeout, etc.)
-- Crear también la réplica usada en modo TEST (mismo esquema).

IF OBJECT_ID(N'dbo.AcreditacionDepositoSantanderPendiente', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AcreditacionDepositoSantanderPendiente
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AcreditacionDepositoSantanderPendiente PRIMARY KEY,
        IDBUZON         NVARCHAR(50)  NOT NULL,
        IDOPERACION     BIGINT        NOT NULL,
        FECHA           DATETIME2(0)  NOT NULL,
        IDBANCO         INT           NULL,
        IDCUENTA        INT           NULL,
        MONEDA          INT           NOT NULL,
        NO_ENVIADO      BIT           NOT NULL CONSTRAINT DF_AcredSantPend_NoEnviado DEFAULT (0),
        MONTO           FLOAT         NOT NULL,
        FECHADEP        DATETIME2(0)  NULL,
        EstadoEnvioWS   NVARCHAR(32)  NOT NULL,
        Observacion     NVARCHAR(2000) NULL
    );
END
GO

IF OBJECT_ID(N'dbo.AcreditacionDepositoSantanderPendiente_Replica', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AcreditacionDepositoSantanderPendiente_Replica
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AcredSantPend_Replica PRIMARY KEY,
        IDBUZON         NVARCHAR(50)  NOT NULL,
        IDOPERACION     BIGINT        NOT NULL,
        FECHA           DATETIME2(0)  NOT NULL,
        IDBANCO         INT           NULL,
        IDCUENTA        INT           NULL,
        MONEDA          INT           NOT NULL,
        NO_ENVIADO      BIT           NOT NULL CONSTRAINT DF_AcredSantPendRep_NoEnviado DEFAULT (0),
        MONTO           FLOAT         NOT NULL,
        FECHADEP        DATETIME2(0)  NULL,
        EstadoEnvioWS   NVARCHAR(32)  NOT NULL,
        Observacion     NVARCHAR(2000) NULL
    );
END
GO
