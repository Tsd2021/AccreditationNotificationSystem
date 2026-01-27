using ANS.Model.Services;
using System;
using System.Net;
using System.Linq;

namespace ANS.Runtime.Guards
{
    /// <summary>
    /// Guardia para consumo de WebServices y operaciones de red
    /// Bloquea HTTP/WebServices en modo TEST, pero permite SMTP si está habilitado explícitamente
    /// </summary>
    public static class WebServiceGuard
    {
        /// <summary>
        /// Valida que el consumo del WebService de Santander esté permitido
        /// </summary>
        /// <param name="operation">Operación que intenta consumir el WS</param>
        /// <exception cref="InvalidOperationException">SIEMPRE en modo TEST</exception>
        public static void EnsureSantanderWebServiceAllowed(string operation = "Operación Santander")
        {
            EnsureWebServiceAllowed("Santander", operation);
        }

        /// <summary>
        /// Valida que el consumo de un WebService genérico esté permitido
        /// </summary>
        public static void EnsureWebServiceAllowed(string bankName, string operation = "Operación")
        {
            if (AppRuntime.IsTest)
            {
                var msg = $"BLOQUEO EN TEST: Intento de consumir WebService | " +
                         $"Banco: {bankName} | Operación: {operation} | " +
                         $"En modo TEST, los WebServices están SIEMPRE bloqueados (incluyendo Santander).";
                ServicioLog.instancia.WriteError(msg, "WebServiceGuard | EnsureWebServiceAllowed");
                throw new InvalidOperationException(msg);
            }
        }

        /// <summary>
        /// ✅ Guardia genérica: Bloquea operaciones HTTP/WebServices en modo TEST
        /// NO bloquea SMTP si TestAllowSmtp=true (usar EnsureSmtpAllowed para SMTP)
        /// </summary>
        /// <param name="serviceName">Nombre del servicio/operación</param>
        /// <param name="operation">Detalle de la operación</param>
        /// <exception cref="InvalidOperationException">En modo TEST para HTTP/WebServices</exception>
        public static void EnsureNetworkAllowed(string serviceName, string operation = "Operación de red")
        {
            if (AppRuntime.IsTest)
            {
                var msg = $"BLOQUEO EN TEST: Intento de realizar operación de red (HTTP/WebService) | " +
                         $"Servicio: {serviceName} | Operación: {operation} | " +
                         $"En modo TEST, las operaciones HTTP/WebServices están bloqueadas. " +
                         $"Para SMTP, configurar TestAllowSmtp=true en App.config.";
                ServicioLog.instancia.WriteError(msg, "WebServiceGuard | EnsureNetworkAllowed");
                throw new InvalidOperationException(msg);
            }
        }

        /// <summary>
        /// ✅ Guardia específica para SMTP: Permite SMTP en TEST solo si TestAllowSmtp=true
        /// </summary>
        /// <param name="operation">Operación SMTP que intenta realizarse</param>
        /// <exception cref="InvalidOperationException">Si en TEST y TestAllowSmtp=false</exception>
        public static void EnsureSmtpAllowed(string operation = "Conexión SMTP")
        {
            if (AppRuntime.IsTest)
            {
                var settings = AppRuntime.Settings.Email;
                if (!settings.TestAllowSmtp)
                {
                    var msg = $"BLOQUEO EN TEST: Intento de conexión SMTP | Operación: {operation} | " +
                             $"En modo TEST, SMTP está bloqueado por defecto. " +
                             $"Para habilitar, configurar TestAllowSmtp=true en App.config.";
                    ServicioLog.instancia.WriteError(msg, "WebServiceGuard | EnsureSmtpAllowed");
                    throw new InvalidOperationException(msg);
                }
                
                // Si está permitido, loggear que se está usando SMTP en TEST
                ServicioLog.instancia.WriteInfo(
                    $"SMTP PERMITIDO EN TEST | Operación: {operation} | " +
                    $"TestEmailWhitelist: {settings.TestEmailWhitelist}",
                    "WebServiceGuard | EnsureSmtpAllowed");
            }
        }

        /// <summary>
        /// Valida que una URL/endpoint esté permitido antes de realizar una llamada HTTP
        /// </summary>
        public static void EnsureHttpCallAllowed(string url, string operation = "Llamada HTTP")
        {
            EnsureNetworkAllowed($"HTTP Request a {url}", operation);
        }
    }
}
