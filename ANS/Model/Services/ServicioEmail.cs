using ANS.Model.Interfaces;
using ANS.Runtime.Guards;
using MailKit.Security;
using Microsoft.Data.SqlClient;
using MimeKit;
using MimeKit.Utils;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Linq;
using ContentType = MimeKit.ContentType;


namespace ANS.Model.Services
{
    public class ServicioEmail : IServicioEmail
    {

        public List<Email> emailsEnvio { get; set; } = new List<Email>();
        string remitente = "acreditaciones@tecnisegur.com.uy";
        string contrasena = "exad urge pknr xpaj";
        string destinatario = "acreditaciones@tecnisegur.com.uy";
        private string _conexionTSD = ConfiguracionGlobal.Conexion22;
        
        // ✅ Thread-safe: Lazy<T> garantiza inicialización única y thread-safe
        // Importante: Este servicio maneja conexiones SMTP compartidas, por lo que thread-safety es crítico
        private static readonly Lazy<ServicioEmail> _lazy = 
            new Lazy<ServicioEmail>(() => new ServicioEmail());
        
        public static ServicioEmail instancia => _lazy.Value;

        public static ServicioEmail getInstancia()
        {
            return _lazy.Value;
        }


        public void CargarEmailsEnvio()
        {

            using (SqlConnection c = new SqlConnection(_conexionTSD))
            {

                string query = @"SELECT DISTINCT
                                e.Email, e.Activo, e.EsPrincipal, e.TipoAcreditacion
                                FROM EmailDestinoEnvio e
                                WHERE e.IdCliente IN (
                                SELECT IDCLIENTE 
                                FROM CC 
                                WHERE IDCLIENTE = 164 
                                OR IDCLIENTE IN 
                                (SELECT IdCliente
                                FROM ClientesRelacionadosTest
                                WHERE IdCliente = 164))
                                AND e.Activo = 1
                                AND e.TipoAcreditacion = 'Tanda1'
                                AND e.Banco = 'SANTANDER';";

                c.Open();

            }

        }

        public async Task<MailKit.Net.Smtp.SmtpClient> getNewSmptClient()
        {
            // ✅ GUARDIA: Validar si SMTP está permitido en TEST
            if (ANS.Runtime.AppRuntime.IsTest)
            {
                // Verificar si TestAllowSmtp está habilitado
                WebServiceGuard.EnsureSmtpAllowed("Conexión SMTP para envío de emails");
                // Si llegamos aquí, TestAllowSmtp=true y está permitido conectar
            }

            var smtp = new MailKit.Net.Smtp.SmtpClient();

            smtp.Timeout = 5 * 60 * 1000;

            // Aquí pones la validación personalizada del certificado
            smtp.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
            {
                if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                    return true;

                if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors)
                {
                    foreach (var status in chain.ChainStatus)
                    {
                        if (status.Status == System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.RevocationStatusUnknown ||
                            status.Status == System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.OfflineRevocation)
                        {
                            continue;
                        }
                        else
                        {
                            return false;
                        }
                    }

                    return true;
                }

                return false;
            };

            await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync("acreditaciones@tecnisegur.com.uy", contrasena);

            return smtp;
        }


        public bool enviarExcelPorMail(string excelPath, string asunto, string cuerpo, Cliente c, Banco b, string tarea ,string ciudad)
        {
            var listaEmails = ObtenerEmailsPorBancoTareaYCiudad(b, tarea, ciudad);

            if (listaEmails == null || listaEmails.Count == 0)
                return false; // no hay a quién enviar

            try
            {
                // ✅ Convertir a método async y usar MailKit con guardias
                return enviarExcelPorMailAsync(excelPath, asunto, cuerpo, listaEmails, b, tarea, ciudad).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                // ✅ Logging mejorado: Registra error con contexto (banco, tarea, ciudad)
                ServicioLog.instancia.WriteLog(ex, b?.NombreBanco ?? "Desconocido", 
                    $"Envío Excel | Tarea: {tarea} | Ciudad: {ciudad}");
                return false;
            }
        }

        /// <summary>
        /// ✅ Versión async de enviarExcelPorMail usando MailKit con guardias
        /// SendWithGuards aplicará la política automáticamente
        /// </summary>
        private async Task<bool> enviarExcelPorMailAsync(string excelPath, string asunto, string cuerpo, List<Email> listaEmails, Banco b, string tarea, string ciudad)
        {
            try
            {
                cuerpo += "<html>" +
                  "<body>" +
                  "<p>Saludos cordiales.</p>" +
                  "</body>" +
                  "</html>";

                // ✅ Crear mensaje base (SendWithGuards aplicará política y validará whitelist)
                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse(remitente));
                message.Subject = asunto;

                // Agregar destinatarios originales (SendWithGuards los reemplazará en TEST)
                var originalRecipients = listaEmails?.Select(e => e.Correo).ToList() ?? new List<string>();
                foreach (var recipient in originalRecipients)
                {
                    message.To.Add(MailboxAddress.Parse(recipient));
                }

                var builder = new BodyBuilder();
                builder.HtmlBody = cuerpo;

                // Adjuntar Excel si existe
                if (!string.IsNullOrEmpty(excelPath) && File.Exists(excelPath))
                {
                    builder.Attachments.Add(excelPath);
                }

                message.Body = builder.ToMessageBody();

                // ✅ Obtener cliente SMTP (con guardias)
                using (var smtpClient = await getNewSmptClient())
                {
                    // ✅ Usar método centralizado SendWithGuards (aplica política + valida whitelist + envía)
                    return await SendWithGuards(message, smtpClient, $"enviarExcelPorMail | Tarea: {tarea} | Ciudad: {ciudad}");
                }
            }
            catch (Exception ex)
            {
                ServicioLog.instancia.WriteLog(ex, b?.NombreBanco ?? "Desconocido", 
                    $"enviarExcelPorMailAsync | Tarea: {tarea} | Ciudad: {ciudad}");
                return false;
            }
        }

        private List<Email> ObtenerEmailsPorBancoTareaYCiudad(Banco banco,string tarea,string ciudad)
        {

            if (banco == null || string.IsNullOrEmpty(tarea) || string.IsNullOrEmpty(ciudad))
            {
                throw new ArgumentException("Banco , tarea y ciudad son obligatorios.");
            }

            List<Email> retorno = ServicioEmailTarea.Instancia.ObtenerTodosLosEmailTarea().Where(e=> e.Banco.ToUpper() == banco.NombreBanco.ToUpper() &&
                     e.Tarea.ToUpper() == tarea.ToUpper() &&
                     e.Ciudad.ToUpper() == ciudad.ToUpper()).ToList();

            return retorno;
        }

        /// <summary>
        /// ✅ Método centralizado para enviar emails con guardias de seguridad
        /// Aplica ApplyEmailPolicy + ValidateRecipientsWhitelist antes de enviar
        /// </summary>
        private async Task<bool> SendWithGuards(
            MimeMessage originalMessage,
            MailKit.Net.Smtp.SmtpClient smtpClient,
            string context = "Envío de email")
        {
            try
            {
                // ✅ Extraer destinatarios originales (To, Cc, Bcc)
                var originalRecipients = new List<string>();
                foreach (var to in originalMessage.To)
                {
                    if (to is MailboxAddress mb)
                        originalRecipients.Add(mb.Address);
                }
                foreach (var cc in originalMessage.Cc)
                {
                    if (cc is MailboxAddress mb)
                        originalRecipients.Add(mb.Address);
                }
                foreach (var bcc in originalMessage.Bcc)
                {
                    if (bcc is MailboxAddress mb)
                        originalRecipients.Add(mb.Address);
                }

                var originalSubject = originalMessage.Subject ?? "";
                var originalBody = originalMessage.HtmlBody ?? originalMessage.TextBody ?? "";

                // ✅ Aplicar política de emails (reemplaza destinatarios en TEST)
                var (recipients, subjectFinal, bodyFinal) = EmailGuard.ApplyEmailPolicy(
                    originalRecipients,
                    originalSubject,
                    originalBody);

                // ✅ VALIDACIÓN CRÍTICA: Verificar whitelist ANTES de enviar
                EmailGuard.ValidateRecipientsWhitelist(recipients, context);

                // ✅ Crear mensaje modificado con política aplicada
                var message = new MimeMessage();
                message.From.Add(originalMessage.From.Mailboxes.FirstOrDefault() ?? MailboxAddress.Parse("acreditaciones@tecnisegur.com.uy"));
                
                // Limpiar destinatarios originales y agregar solo whitelist
                message.To.Clear();
                message.Cc.Clear();
                message.Bcc.Clear();
                
                foreach (var recipient in recipients)
                {
                    message.To.Add(MailboxAddress.Parse(recipient));
                }

                message.Subject = subjectFinal;

                // Copiar attachments del mensaje original
                var builder = new BodyBuilder();
                if (originalMessage.Body is Multipart multipart)
                {
                    foreach (var part in multipart)
                    {
                        if (part is MimePart mimePart && mimePart.IsAttachment)
                        {
                            builder.Attachments.Add(mimePart);
                        }
                    }
                }

                builder.HtmlBody = bodyFinal;
                message.Body = builder.ToMessageBody();

                // ✅ Logging antes de enviar
                ServicioLog.instancia.WriteInfo(
                    $"ENVIANDO EMAIL (SendWithGuards) | Contexto: {context} | Subject: {subjectFinal} | " +
                    $"Destinatarios finales: {string.Join(", ", recipients)} | " +
                    $"Destinatarios originales: {string.Join(", ", originalRecipients)} | " +
                    $"Modo: {(ANS.Runtime.AppRuntime.IsTest ? "TEST" : "PRODUCTION")}",
                    "ServicioEmail | SendWithGuards");

                await smtpClient.SendAsync(message);

                return true;
            }
            catch (Exception ex)
            {
                ServicioLog.instancia.WriteLog(ex, "Todos", $"SendWithGuards | Contexto: {context}");
                return false;
            }
        }

        public async Task<bool> EnviarMailDesconexion(MimeMessage msg, MailKit.Net.Smtp.SmtpClient smtpClient)
        {
            // ✅ Usar método centralizado con guardias
            return await SendWithGuards(msg, smtpClient, "Envío Mail Desconexión");
        }

        public async Task<bool> EnviarExcelPorMailMasivoConMailKit(Stream? excelStream, string? fileName, string subject, string body, List<Email> _destinos, MailKit.Net.Smtp.SmtpClient smtpClient)
        {
            try
            {
                // ✅ Crear mensaje base (SendWithGuards aplicará política y validará whitelist)
                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse("acreditaciones@tecnisegur.com.uy"));
                
                // Agregar destinatarios originales (SendWithGuards los reemplazará en TEST)
                var originalRecipients = _destinos?.Select(e => e.Correo).ToList() ?? new List<string>();
                foreach (var recipient in originalRecipients)
                {
                    message.To.Add(MailboxAddress.Parse(recipient));
                }
          
                message.Subject = subject;

                var builder = new BodyBuilder();
                builder.HtmlBody = body;

                // Solo adjuntar si hay Excel
                if (excelStream != null && !string.IsNullOrEmpty(fileName))
                {
                    builder.Attachments.Add(fileName, excelStream,
                        new ContentType("application", "vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
                }

                message.Body = builder.ToMessageBody();

                // ✅ Usar método centralizado SendWithGuards (aplica política + valida whitelist + envía)
                return await SendWithGuards(message, smtpClient, "EnviarExcelPorMailMasivoConMailKit");
            }
            catch (Exception ex)
            {
                // ✅ Logging mejorado: Reemplaza escritura directa a archivo por servicio de logging
                // Incluye información del archivo y destinatarios para debugging
                string destinosInfo = _destinos != null && _destinos.Count > 0 
                    ? string.Join(", ", _destinos.Select(e => e.Correo).Take(3)) 
                    : "Sin destinatarios";
                ServicioLog.instancia.WriteLog(ex, "Todos", 
                    $"Envío Excel Masivo MailKit | Archivo: {fileName ?? "N/A"} | Destinos: {destinosInfo}");
                return false;
            }
        }

        public int AgregarEmailDestino(
            string banco,
            int? idCliente,
            string tarea,
            string correo,
            bool activo,
            string ciudad)
        {
            if (string.IsNullOrWhiteSpace(banco)
             || string.IsNullOrWhiteSpace(tarea)
             || string.IsNullOrWhiteSpace(correo)
             || string.IsNullOrWhiteSpace(ciudad))
            {
                return 0;
            }

            const string sql = @"
            INSERT INTO Email_Tarea
                (Email, Tarea, Banco, Ciudad, activo, idcliente)
            VALUES
                (@email, @tarea, @banco, @ciudad, 1 , @idCliente);
        ";

            using var cn = new SqlConnection(_conexionTSD);
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@email", correo);
            cmd.Parameters.AddWithValue("@tarea", tarea);
            cmd.Parameters.AddWithValue("@banco", banco);
            cmd.Parameters.AddWithValue("@ciudad", ciudad);
            cmd.Parameters.AddWithValue("@activo", activo);
            cmd.Parameters.AddWithValue(
                "@idCliente",
                idCliente.HasValue ? (object)idCliente.Value : DBNull.Value
            );

            cn.Open();
            int rowsAffected = cmd.ExecuteNonQuery();
            
            // ✅ Logging avanzado: Detalle de inserción de email destino
            if (rowsAffected > 0)
            {
                string idClienteStr = idCliente.HasValue ? idCliente.Value.ToString() : "N/A";
                ServicioLog.instancia.WriteInfo(
                    $"Inserción de email destino | Email: {correo} | Banco: {banco} | Tarea: {tarea} | " +
                    $"Ciudad: {ciudad} | IdCliente: {idClienteStr} | Activo: {activo}",
                    "ServicioEmail | AgregarEmailDestino");
            }
            
            return rowsAffected;
        }

        public List<Email> ListarPor(
            int? idCliente,
            string nombreBanco,
            string tarea,
            string ciudad)
        {
            if (string.IsNullOrWhiteSpace(nombreBanco)
             || string.IsNullOrWhiteSpace(tarea)
             || string.IsNullOrWhiteSpace(ciudad))
            {
                throw new ArgumentException("Banco, tarea y ciudad son obligatorios.");
            }

            const string sql = @"
            SELECT Email, activo
            FROM Email_Tarea
            WHERE Banco = @nombreBanco
              AND Tarea = @tarea
              AND Ciudad = @ciudad
              AND (idcliente IS NULL OR idcliente = @idCliente);";

            var resultado = new List<Email>();
            using var conn = new SqlConnection(_conexionTSD);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@nombreBanco", nombreBanco);
            cmd.Parameters.AddWithValue("@tarea", tarea);
            cmd.Parameters.AddWithValue("@ciudad", ciudad);
            cmd.Parameters.AddWithValue(
                "@idCliente",
                idCliente.HasValue ? (object)idCliente.Value : DBNull.Value
            );

            conn.Open();
            using var reader = cmd.ExecuteReader();
            int emailOrd = reader.GetOrdinal("Email");
            int activoOrd = reader.GetOrdinal("activo");

            while (reader.Read())
            {
                resultado.Add(new Email
                {
                    Correo = reader.GetString(emailOrd),
                    Activo = reader.GetBoolean(activoOrd) // reutilizamos esta propiedad para 'activo'
                });
            }

            return resultado;
        }

        public int EliminarEmailDestino(
            string banco,
            int? idCliente,
            string tarea,
            string correo,
            string ciudad)
        {
            if (string.IsNullOrWhiteSpace(banco)
             || string.IsNullOrWhiteSpace(tarea)
             || string.IsNullOrWhiteSpace(correo)
             || string.IsNullOrWhiteSpace(ciudad))
            {
                return 0;
            }

            const string sql = @"
            DELETE FROM Email_Tarea
            WHERE Email = @email
              AND Tarea = @tarea
              AND Banco = @banco
              AND Ciudad = @ciudad
              AND (idcliente IS NULL OR idcliente = @idCliente);";

            using var cn = new SqlConnection(_conexionTSD);
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@email", correo);
            cmd.Parameters.AddWithValue("@tarea", tarea);
            cmd.Parameters.AddWithValue("@banco", banco);
            cmd.Parameters.AddWithValue("@ciudad", ciudad);
            cmd.Parameters.AddWithValue(
                "@idCliente",
                idCliente.HasValue ? (object)idCliente.Value : DBNull.Value
            );

            cn.Open();
            int rowsAffected = cmd.ExecuteNonQuery();
            
            // ✅ Logging avanzado: Detalle de eliminación de email destino
            if (rowsAffected > 0)
            {
                string idClienteStr = idCliente.HasValue ? idCliente.Value.ToString() : "N/A";
                ServicioLog.instancia.WriteInfo(
                    $"Eliminación de email destino | Email: {correo} | Banco: {banco} | Tarea: {tarea} | " +
                    $"Ciudad: {ciudad} | IdCliente: {idClienteStr}",
                    "ServicioEmail | EliminarEmailDestino");
            }
            
            return rowsAffected;
        }

        public int ModificarEmailDestino(
            string banco,
            int? idCliente,
            string tarea,
            string correoViejo,
            string correoNuevo,
            bool activo,
            string ciudad)
        {
            if (string.IsNullOrWhiteSpace(banco)
             || string.IsNullOrWhiteSpace(tarea)
             || string.IsNullOrWhiteSpace(correoViejo)
             || string.IsNullOrWhiteSpace(correoNuevo)
             || string.IsNullOrWhiteSpace(ciudad))
            {
                return 0;
            }

            const string sql = @"
            UPDATE Email_Tarea
            SET Email = @correoNuevo,
                activo = @activo
            WHERE Email = @correoViejo
              AND Tarea = @tarea
              AND Banco = @banco
              AND Ciudad = @ciudad
              AND (idcliente IS NULL OR idcliente = @idCliente);";

            using var cn = new SqlConnection(_conexionTSD);
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@correoViejo", correoViejo);
            cmd.Parameters.AddWithValue("@correoNuevo", correoNuevo);
            cmd.Parameters.AddWithValue("@tarea", tarea);
            cmd.Parameters.AddWithValue("@banco", banco);
            cmd.Parameters.AddWithValue("@ciudad", ciudad);
            cmd.Parameters.AddWithValue("@activo", activo);
            cmd.Parameters.AddWithValue(
                "@idCliente",
                idCliente.HasValue ? (object)idCliente.Value : DBNull.Value
            );

            cn.Open();
            int rowsAffected = cmd.ExecuteNonQuery();
            
            // ✅ Logging avanzado: Detalle de modificación de email destino
            if (rowsAffected > 0)
            {
                string idClienteStr = idCliente.HasValue ? idCliente.Value.ToString() : "N/A";
                ServicioLog.instancia.WriteInfo(
                    $"Modificación de email destino | Email viejo: {correoViejo} | Email nuevo: {correoNuevo} | " +
                    $"Banco: {banco} | Tarea: {tarea} | Ciudad: {ciudad} | IdCliente: {idClienteStr} | Activo: {activo}",
                    "ServicioEmail | ModificarEmailDestino");
            }
            
            return rowsAffected;
        }
    }
}
