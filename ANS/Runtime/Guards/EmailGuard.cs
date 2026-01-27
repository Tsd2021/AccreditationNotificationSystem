using ANS.Model;
using ANS.Model.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ANS.Runtime.Guards
{
    /// <summary>
    /// Guardia y política para envío de emails
    /// En modo TEST: reemplaza destinatarios por whitelist (acreditaciones@tecnisegur.com.uy)
    /// </summary>
    public static class EmailGuard
    {
        /// <summary>
        /// Aplica la política de email según el modo actual
        /// En TEST: reemplaza TODOS los destinatarios por whitelist
        /// </summary>
        /// <param name="originalRecipients">Lista original de emails destinatarios</param>
        /// <param name="originalSubject">Subject original</param>
        /// <param name="originalBody">Body original</param>
        /// <returns>Tupla con (recipients, subject, body) modificados según política</returns>
        public static (List<string> Recipients, string Subject, string Body) ApplyEmailPolicy(
            List<string> originalRecipients,
            string originalSubject,
            string originalBody)
        {
            if (AppRuntime.IsTest)
            {
                var settings = AppRuntime.Settings.Email;
                
                // ✅ En TEST, SIEMPRE usar whitelist (ignorar destinatarios originales)
                var whitelistEmails = settings.TestEmailWhitelist
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToList();

                if (!whitelistEmails.Any())
                {
                    // Fallback seguro si whitelist está vacío
                    whitelistEmails = new List<string> { settings.OverrideRecipientInTest };
                }

                // Recipients: solo los emails de whitelist
                var recipients = whitelistEmails;

                // Subject: agregar prefijo TEST si está configurado
                var subject = originalSubject;
                if (settings.AddTestPrefixToSubject && !subject.StartsWith("[TEST", StringComparison.OrdinalIgnoreCase))
                {
                    subject = $"[TEST MODE] {subject}";
                }

                // Body: agregar información de destinatarios originales si está configurado
                var body = originalBody;
                if (settings.IncludeOriginalRecipientsInBody && originalRecipients != null && originalRecipients.Any())
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("<hr style='border: 1px solid #ccc; margin: 20px 0;'/>");
                    sb.AppendLine("<div style='background-color: #fff3cd; padding: 10px; border-left: 4px solid #ffc107; margin: 10px 0;'>");
                    sb.AppendLine("<strong>⚠️ MODO TEST ACTIVO</strong><br/>");
                    sb.AppendLine("Este email fue enviado en modo TEST. Los destinatarios originales fueron reemplazados.");
                    sb.AppendLine("<br/><br/>");
                    sb.AppendLine("<strong>Destinatarios originales:</strong><br/>");
                    foreach (var email in originalRecipients)
                    {
                        sb.AppendLine($"• {email}<br/>");
                    }
                    sb.AppendLine("</div>");
                    sb.AppendLine("<hr style='border: 1px solid #ccc; margin: 20px 0;'/>");
                    body = sb.ToString() + body;
                }

                ServicioLog.instancia.WriteInfo(
                    $"POLÍTICA EMAIL TEST APLICADA | Originales: {string.Join(", ", originalRecipients ?? new List<string>())} | " +
                    $"Destinatarios finales (whitelist): {string.Join(", ", recipients)}",
                    "EmailGuard | ApplyEmailPolicy");

                return (recipients, subject, body);
            }

            // PRODUCCIÓN: sin cambios
            return (originalRecipients ?? new List<string>(), originalSubject, originalBody);
        }

        /// <summary>
        /// ✅ Valida que los destinatarios finales estén en whitelist (solo en TEST)
        /// Debe llamarse ANTES de enviar el email por SMTP
        /// </summary>
        /// <param name="finalRecipients">Lista de destinatarios finales que se intentarán enviar</param>
        /// <param name="context">Contexto de la operación (para logging)</param>
        /// <exception cref="InvalidOperationException">Si en TEST y algún destinatario no está en whitelist</exception>
        public static void ValidateRecipientsWhitelist(List<string> finalRecipients, string context = "Envío de email")
        {
            if (!AppRuntime.IsTest)
                return; // En PROD no validar whitelist

            var settings = AppRuntime.Settings.Email;
            var whitelistEmails = settings.TestEmailWhitelist
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim().ToLowerInvariant())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList();

            if (!whitelistEmails.Any())
            {
                whitelistEmails = new List<string> { settings.OverrideRecipientInTest.ToLowerInvariant() };
            }

            var invalidRecipients = finalRecipients?
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim().ToLowerInvariant())
                .Where(r => !whitelistEmails.Contains(r))
                .ToList() ?? new List<string>();

            if (invalidRecipients.Any())
            {
                var msg = $"ERROR CRÍTICO DE SEGURIDAD: Intento de enviar email a destinatarios NO whitelist en modo TEST | " +
                         $"Contexto: {context} | " +
                         $"Destinatarios inválidos: {string.Join(", ", invalidRecipients)} | " +
                         $"Whitelist permitida: {string.Join(", ", whitelistEmails)} | " +
                         $"El email NO será enviado.";
                ServicioLog.instancia.WriteError(msg, "EmailGuard | ValidateRecipientsWhitelist");
                throw new InvalidOperationException(msg);
            }

            ServicioLog.instancia.WriteInfo(
                $"VALIDACIÓN WHITELIST OK | Contexto: {context} | " +
                $"Destinatarios validados: {string.Join(", ", finalRecipients ?? new List<string>())}",
                "EmailGuard | ValidateRecipientsWhitelist");
        }

        /// <summary>
        /// Aplica política a lista de objetos Email
        /// </summary>
        public static List<Email> ApplyEmailPolicyToEmailList(List<Email> originalEmails)
        {
            if (AppRuntime.IsTest)
            {
                var settings = AppRuntime.Settings.Email;
                var testRecipient = settings.OverrideRecipientInTest;

                // Reemplazar todos los emails por el de test
                var testEmail = new Email
                {
                    Correo = testRecipient,
                    EsPrincipal = true,
                    Activo = true
                };

                var originalList = originalEmails?.Select(e => e.Correo).ToList() ?? new List<string>();

                ServicioLog.instancia.WriteInfo(
                    $"POLÍTICA EMAIL TEST APLICADA | Originales: {string.Join(", ", originalList)} | " +
                    $"Destinatario final: {testRecipient}",
                    "EmailGuard | ApplyEmailPolicyToEmailList");

                return new List<Email> { testEmail };
            }

            // PRODUCCIÓN: sin cambios
            return originalEmails ?? new List<Email>();
        }
    }
}
