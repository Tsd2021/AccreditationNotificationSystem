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

        /// <summary>
        /// Valor para columna EstadoEnvioWS en auditoría (p. ej. TIMEOUT, RESPUESTA_ERROR). Solo aplica si <see cref="RequiereAuditoriaEnvioFallido"/>.
        /// </summary>
        public string EstadoEnvioWsParaAuditoria { get; init; }

        /// <summary>
        /// Texto detallado para columna Observacion en auditoría. Solo aplica si <see cref="RequiereAuditoriaEnvioFallido"/>.
        /// </summary>
        public string ObservacionParaAuditoria { get; init; }

        public static GeneracionArchivoBancoResult ExitoSinRestricciones() => new GeneracionArchivoBancoResult
        {
            PermiteInsertarEnAcreditacionPrincipal = true,
            RequiereAuditoriaEnvioFallido = false,
            Motivo = null,
            EstadoEnvioWsParaAuditoria = null,
            ObservacionParaAuditoria = null
        };

        public static GeneracionArchivoBancoResult SantanderEnvioWebServiceFallido(
            string motivo,
            string estadoEnvioWsParaAuditoria = null,
            string observacionParaAuditoria = null) => new GeneracionArchivoBancoResult
        {
            PermiteInsertarEnAcreditacionPrincipal = false,
            RequiereAuditoriaEnvioFallido = true,
            Motivo = motivo,
            EstadoEnvioWsParaAuditoria = string.IsNullOrWhiteSpace(estadoEnvioWsParaAuditoria)
                ? "FALLIDO_WS"
                : estadoEnvioWsParaAuditoria.Trim(),
            ObservacionParaAuditoria = observacionParaAuditoria ?? motivo
        };
    }
}
