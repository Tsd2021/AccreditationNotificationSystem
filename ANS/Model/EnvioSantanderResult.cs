namespace ANS.Model
{
    /// <summary>
    /// Resultado de una invocación al Web Service de envío de archivo Santander (uploadLotFile).
    /// </summary>
    public sealed class EnvioSantanderResult
    {
        public const string EstadoOk = "OK";
        public const string EstadoValidacion = "VALIDACION";
        public const string EstadoTimeout = "TIMEOUT";
        public const string EstadoRespuestaError = "RESPUESTA_ERROR";
        public const string EstadoRespuestaNula = "RESPUESTA_NULA";
        public const string EstadoFaultSoap = "FAULT_SOAP";
        public const string EstadoErrorComunicacion = "ERROR_COMUNICACION";
        public const string EstadoExcepcion = "EXCEPCION";

        public bool Exito { get; init; }
        public string EstadoEnvio { get; init; }
        public string Detalle { get; init; }

        public static EnvioSantanderResult Exitoso() => new EnvioSantanderResult
        {
            Exito = true,
            EstadoEnvio = EstadoOk,
            Detalle = null
        };

        public static EnvioSantanderResult Fallo(string estadoEnvio, string detalle) => new EnvioSantanderResult
        {
            Exito = false,
            EstadoEnvio = estadoEnvio ?? EstadoExcepcion,
            Detalle = detalle
        };
    }
}
