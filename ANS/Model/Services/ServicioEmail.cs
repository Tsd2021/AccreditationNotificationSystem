using ANS.Model.Interfaces;
using ClosedXML.Parser;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Wordprocessing;
using MailKit.Security;
using Microsoft.Data.SqlClient;
using MimeKit;
using MimeKit.Utils;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System.CodeDom;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using ContentType = MimeKit.ContentType;


namespace ANS.Model.Services
{
    public class ServicioEmail : IServicioEmail
    {

        public List<Email> emailsEnvio { get; set; } = new List<Email>();
        string remitente = "dchiquiar@tecnisegur.com.uy";
        string contrasena = "cfsg xikf qjwp iwzu";
        string destinatario = "dchiquiar@tecnisegur.com.uy";
        private string _conexionTSD = ConfiguracionGlobal.Conexion22;
        public static ServicioEmail instancia { get; set; }

        public static ServicioEmail getInstancia()
        {
            if (instancia == null)
            {
                instancia = new ServicioEmail();
            }
            return instancia;
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

            await smtp.AuthenticateAsync("dchiquiar@tecnisegur.com.uy", "cfsg xikf qjwp iwzu");

            return smtp;
        }


        public bool enviarExcelPorMail(string excelPath, string asunto, string cuerpo, Cliente c, Banco b, ConfiguracionAcreditacion config)
        {

            var listaEmails = ObtenerEmailsPorClienteBancoYConfig(c, b, config);

            if (!listaEmails.Any())
                return false; // no hay a quién enviar

            try
            {
                cuerpo += "<html>" +
              "<body>" +
              "<p>Saludos cordiales.</p>" +
              // Agrega un contenedor para la firma con un margen superior para separar del texto
              "<div style='margin-top:30px;'>" +
              "<img src='cid:bannerImage' alt='Firma Diego Chiquiar' style='width:400px; height:750;'/>" +
              "</div>" +
              "</body>" +
              "</html>";

                // Crea la vista HTML para el correo
                AlternateView avHtml = AlternateView.CreateAlternateViewFromString(cuerpo, null, MediaTypeNames.Text.Html);

                // Crea el recurso vinculado (imagen) y establece su ContentId para que coincida con el del HTML
                LinkedResource inlineImage = new LinkedResource("Images/FirmaDiegoMail.png", MediaTypeNames.Image.Png)
                {
                    ContentId = "bannerImage",
                    TransferEncoding = TransferEncoding.Base64
                };

                avHtml.LinkedResources.Add(inlineImage);

                // Configuración de remitente y destinatario


                using (MailMessage mail = new MailMessage(remitente, destinatario))
                {
                    mail.Subject = asunto;
                    mail.Body = cuerpo;
                    mail.IsBodyHtml = true;
                    mail.AlternateViews.Add(avHtml);

                    //Cuando esté en producción activar esto:
                    if (listaEmails != null && listaEmails.Count > 0)
                    {
                        foreach (var e in listaEmails)
                        {
                            mail.To.Add(e.Correo);
                        }
                    }

                    //mail.CC.Add("pablo@tecnisegur.com.uy");

                    // Adjunta el archivo Excel
                    if (!string.IsNullOrEmpty(excelPath))
                    {
                        mail.Attachments.Add(new Attachment(excelPath));
                    }

                    SmtpClient clienteSmtp = new SmtpClient("smtp.gmail.com")
                    {
                        Port = 587,
                        Credentials = new NetworkCredential(remitente, contrasena),
                        UseDefaultCredentials = false,
                        EnableSsl = true
                    };

                    clienteSmtp.Send(mail);
                }
                return true;
            }
            catch (Exception ex)
            {
                // Opcional: registrar el error para diagnóstico
                Console.WriteLine("Error al enviar el correo: " + ex.Message);
                return false;
            }
        }

        private List<Email> ObtenerEmailsPorClienteBancoYConfig(Cliente cliente, Banco banco, ConfiguracionAcreditacion config)
        {
            var emails = new List<Email>();

            // Construyo la consulta base
            var sb = new StringBuilder(@"
                                        SELECT email, esprincipal
                                        FROM EmailDestinoEnvio
                                        WHERE banco = @banco
                                      ");

            // Filtro opcional por tipo de acreditación
            if (config != null)
            {
                sb.Append(" AND tipoacreditacion = @tipoAcreditacion");
            }

            // Filtro de cliente: genérico (NULL) o específico
            if (cliente != null)
            {
                sb.Append(" AND (idcliente IS NULL OR idcliente = @idcliente)");
            }
            else
            {
                sb.Append(" AND idcliente IS NULL");
            }

            using var conn = new SqlConnection(_conexionTSD);
            using var cmd = new SqlCommand(sb.ToString(), conn);

            // Parámetros obligatorios
            cmd.Parameters.Add("@banco", SqlDbType.VarChar, 100).Value = banco.NombreBanco;

            // Parámetros opcionales (solo si los usamos)
            if (config != null)
            {
                cmd.Parameters.Add("@tipoAcreditacion", SqlDbType.VarChar, 50)
                   .Value = config.TipoAcreditacion;
            }
            if (cliente != null)
            {
                cmd.Parameters.Add("@idcliente", SqlDbType.Int)
                   .Value = cliente.IdCliente;
            }

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                emails.Add(new Email
                {
                    Correo = reader.GetString(reader.GetOrdinal("email")),
                    EsPrincipal = reader.GetBoolean(reader.GetOrdinal("esprincipal"))
                });
            }

            return emails;

        }


        public async Task<bool> EnviarMailDesconexion(MimeMessage msg, MailKit.Net.Smtp.SmtpClient smtpClient)
        {
            try
            {
                await smtpClient.SendAsync(msg);

                return true;
            }
            catch (Exception ex)
            {
                // Opcional: registrar el error para diagnóstico
                Console.WriteLine("Error al enviar el correo: " + ex.Message);
                return false;
            }
        }

        public async Task<bool> EnviarExcelPorMailMasivoConMailKit(Stream excelStream, string fileName, string subject, string body, List<Email> _destinos, MailKit.Net.Smtp.SmtpClient smtpClient)
        {
            try
            {

                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse("dchiquiar@tecnisegur.com.uy"));
                //Cuando esté en producción activar esto:
                /*
                foreach (var e in _destinos)
                {
                    message.To.Add(MailboxAddress.Parse(e.Correo));
                }*/
                message.To.Add(MailboxAddress.Parse("dchiquiar@tecnisegur.com.uy"));
                message.Subject = subject;

                var builder = new BodyBuilder();
                var firma = builder.LinkedResources.Add("Images/FirmaDiegoMail.png");
                firma.ContentId = MimeUtils.GenerateMessageId();
                builder.HtmlBody = $@"
                {body}
                 <div style=""text-align:left; margin-top:30px;"">
                              <img 
                                src=""cid:{firma.ContentId}"" 
                                style=""max-width:300px; height:auto;"" 
                                alt=""Firma Diego"" />
                            </div>";

                builder.Attachments.Add(fileName, excelStream,
                    new ContentType(
                        "application",
                        "vnd.openxmlformats-officedocument.spreadsheetml.sheet"));

                message.Body = builder.ToMessageBody();

                await smtpClient.SendAsync(message);

                return true;
            }
            catch (Exception ex)
            {
                // Graba la excepción en log para depurar
                File.AppendAllText("errores_mailkit.log",
                    $"{DateTime.Now:O} — Error MailKit: {ex}\r\n");
                return false;
            }
        }


        // ServicioEmail: AgregarEmailDestino
        public int AgregarEmailDestino(
            string banco,
            int? idCliente,
            string cliente,
            string tipoAcreditacion,
            string correo,
            bool esPrincipal)
        {
            // Validaciones mínimas
            if (string.IsNullOrWhiteSpace(banco)
             || string.IsNullOrWhiteSpace(tipoAcreditacion)
             || string.IsNullOrWhiteSpace(correo))
            {
                return 0;
            }

            const string sql = @"
        INSERT INTO EmailDestinoEnvio
            (IdCliente, Cliente, TipoAcreditacion, Banco, Email, EsPrincipal)
        VALUES
            (@idCliente, @cliente, @tipoAcreditacion, @banco, @email, @esPrincipal);
    ";

            using var cn = new SqlConnection(_conexionTSD);
            using var cmd = new SqlCommand(sql, cn);
            {
                // Parámetro nullable para IdCliente
                cmd.Parameters.AddWithValue(
                    "@idCliente",
                    idCliente.HasValue ? (object)idCliente.Value : DBNull.Value
                );
                // Cliente puede ser null o vacío en envíos genéricos
                cmd.Parameters.AddWithValue(
                    "@cliente",
                    string.IsNullOrWhiteSpace(cliente)
                        ? (object)DBNull.Value
                        : cliente
                );
                cmd.Parameters.AddWithValue("@tipoAcreditacion", tipoAcreditacion);
                cmd.Parameters.AddWithValue("@banco", banco);
                cmd.Parameters.AddWithValue("@email", correo);
                cmd.Parameters.AddWithValue("@esPrincipal", esPrincipal);

                cn.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        public List<Email> ListarPor(int? idCliente, string nombreBanco, string tipoAcreditacion)
        {
            if (string.IsNullOrWhiteSpace(nombreBanco)
             || string.IsNullOrWhiteSpace(tipoAcreditacion))
            {
                throw new ArgumentException("Banco y tipo de acreditación son obligatorios.");
            }

            const string sql = @"
        SELECT email, esprincipal
          FROM emaildestinoenvio
         WHERE banco            = @nombreBanco
           AND tipoacreditacion = @tipoAcreditacion
           AND (
                 idcliente IS NULL 
              OR idcliente = @idCliente 
           )";

            var resultado = new List<Email>();

            using var conn = new SqlConnection(_conexionTSD);
            using var cmd = new SqlCommand(sql, conn);
            {
                // Parámetros básicos
                cmd.Parameters.AddWithValue("@nombreBanco", nombreBanco);
                cmd.Parameters.AddWithValue("@tipoAcreditacion", tipoAcreditacion);

                // Parámetro opcional de cliente
                object sqlIdCliente = idCliente.HasValue
                                      ? (object)idCliente.Value
                                      : DBNull.Value;
                cmd.Parameters.AddWithValue("@idCliente", sqlIdCliente);

                conn.Open();
                using var reader = cmd.ExecuteReader();
                int emailOrd = reader.GetOrdinal("email");
                int principalOrd = reader.GetOrdinal("esprincipal");

                while (reader.Read())
                {
                    resultado.Add(new Email
                    {
                        Correo = reader.GetString(emailOrd),
                        EsPrincipal = reader.GetBoolean(principalOrd)
                    });
                }
            }

            return resultado;
        }
    }
}
