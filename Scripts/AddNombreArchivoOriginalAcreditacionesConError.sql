-- Columna de referencia: nombre del archivo .dat enviado (o intentado) a Santander cuando falla el WS.
-- Ejecutar en la base donde exista dbo.AcreditacionesConError.

IF COL_LENGTH(N'dbo.AcreditacionesConError', N'NombreArchivoOriginal') IS NULL
BEGIN
    ALTER TABLE dbo.AcreditacionesConError
        ADD NombreArchivoOriginal NVARCHAR(500) NULL;
END
GO
