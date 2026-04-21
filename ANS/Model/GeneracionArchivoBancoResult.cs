namespace ANS.Model
{
    /// <summary>
    /// Resultado de la generación (y, si aplica, envío al proveedor) del archivo de acreditación.
    /// Solo Santander usa <see cref="RequiereAuditoriaEnvioFallido"/> cuando el Web Service no confirma éxito.
    /// </summary>
    public sealed class GeneracionArchivoBancoResult
    {
        /// <summary>
        /// Si es false, no deben insertarse filas en la tabla principal de acreditaciones para este lote.
        /// </summary>
        public bool PermiteInsertarEnAcreditacionPrincipal { get; init; }

        /// <summary>
        /// Si es true, registrar los depósitos afectados en la tabla de auditoría Santander (pendiente / fallo WS).
        /// </summary>
        public bool RequiereAuditoriaEnvioFallido { get; init; }

        public string Motivo { get; init; }

        public static GeneracionArchivoBancoResult ExitoSinRestricciones() => new GeneracionArchivoBancoResult
        {
            PermiteInsertarEnAcreditacionPrincipal = true,
            RequiereAuditoriaEnvioFallido = false,
            Motivo = null
        };

        public static GeneracionArchivoBancoResult SantanderEnvioWebServiceFallido(string motivo) => new GeneracionArchivoBancoResult
        {
            PermiteInsertarEnAcreditacionPrincipal = false,
            RequiereAuditoriaEnvioFallido = true,
            Motivo = motivo
        };
    }
}
