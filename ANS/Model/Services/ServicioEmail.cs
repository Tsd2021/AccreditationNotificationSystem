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
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
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

            await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync("dchiquiar@tecnisegur.com.uy", "cfsg xikf qjwp iwzu");

            return smtp;
        }

        public bool enviarExcelPorMail(string excelPath, string asunto, string cuerpo, Cliente c, Banco b, ConfiguracionAcreditacion config)
        {

            List<Email> listaEmails = new List<Email>();

            if (c != null)
            {
                listaEmails = ObtenerEmailsPorClienteBancoYConfig(c, b, config);
            }
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

                    //mail.CC.Add("pablo@tecnisegur.com.uy");

                    // Adjunta el archivo Excel
                    if (!string.IsNullOrEmpty(excelPath))
                    {
                        mail.Attachments.Add(new Attachment(excelPath));
                    }

                    System.Net.Mail.SmtpClient clienteSmtp = new System.Net.Mail.SmtpClient("smtp.gmail.com")
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

        private List<Email> ObtenerEmailsPorClienteBancoYConfig(Cliente c, Banco banco, ConfiguracionAcreditacion config)
        {

            List<Email> retorno = new List<Email>();
            if (c != null)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(_conexionTSD))
                    {
                        conn.Open();

                        string query = @"select email,esprincipal 
                                         from emaildestinoenvio 
                                         where banco = @banco 
                                         and idcliente = @idCli 
                                         and tipoacreditacion = @tipoAcreditacion 
                                         and activo = true";
                        SqlCommand cmd = new SqlCommand(query, conn);

                        cmd.Parameters.AddWithValue("@banco", banco.NombreBanco);
                        cmd.Parameters.AddWithValue("@idCli", c.IdCliente);
                        cmd.Parameters.AddWithValue("@tipoAcreditacion", config.TipoAcreditacion);

                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            int emailOrdinal = r.GetOrdinal("email");
                            int esPrincipalOrdinal = r.GetOrdinal("esprincipal");

                            while (r.Read())
                            {
                                Email e = new Email();
                                e.EsPrincipal = r.GetBoolean(esPrincipalOrdinal);
                                e.Correo = r.GetString(emailOrdinal);


                            }
                        }

                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error al obtener emails por cliente: " + e.Message);
                }

            }


            return retorno;
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

        public async Task<bool> EnviarExcelPorMailMasivoConMailKit(Stream excelStream, string fileName, string subject, string body, string destino, MailKit.Net.Smtp.SmtpClient smtpClient)
        {
            try
            {

                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse("dchiquiar@tecnisegur.com.uy"));
                message.To.Add(MailboxAddress.Parse(destino));
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


        public int AgregarEmailDestino(string banco, int idCliente, string cliente, string tipoAcreditacion, string correo, bool esPrincipal)
        {
            try
            {
                if (string.IsNullOrEmpty(banco) || string.IsNullOrEmpty(cliente) || string.IsNullOrEmpty(tipoAcreditacion) || string.IsNullOrEmpty(correo))
                {
                    return -1;
                }

                using (SqlConnection cn = new SqlConnection(_conexionTSD))
                {

                    cn.Open();

                    string query = @"
                                INSERT INTO EmailDestinoEnvio
                                    (IdCliente,
                                     Cliente,
                                     TipoAcreditacion,
                                     Banco,
                                     Email,
                                     EsPrincipal)
                                VALUES
                                    (@idCliente,
                                     @cliente,
                                     @tipoAcreditacion,
                                     @banco,
                                     @email,
                                     @esPrincipal);";

                    SqlCommand cmd = new SqlCommand(query, cn);

                    cmd.Parameters.AddWithValue("@idCliente", idCliente);
                    cmd.Parameters.AddWithValue("@cliente", cliente);
                    cmd.Parameters.AddWithValue("@tipoAcreditacion", tipoAcreditacion);
                    cmd.Parameters.AddWithValue("@banco", banco);
                    cmd.Parameters.AddWithValue("@email", correo);
                    cmd.Parameters.AddWithValue("@esPrincipal", esPrincipal);

                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<Email> ListarPor(int idCliente, string nombreBanco, string tipoAcreditacion)
        {
            List<Email> retorno = new List<Email>();

            try
            {
                if (idCliente <= 0 || string.IsNullOrEmpty(nombreBanco) || string.IsNullOrEmpty(tipoAcreditacion))
                {
                    throw new Exception("Los parámetros para Listar mails son incorrecots");
                }

                string q = @"select email,esprincipal
                             from emaildestinoenvio
                             where idcliente = @idCliente
                             and banco = @nombreBanco
                             and tipoacreditacion = @tipoAcreditacion";

                using (SqlConnection c = new SqlConnection(_conexionTSD))
                {
                    c.Open();

                    SqlCommand cmd = new SqlCommand(q, c);

                    cmd.Parameters.AddWithValue("@idCliente", idCliente);
                    cmd.Parameters.AddWithValue("@nombreBanco", nombreBanco);
                    cmd.Parameters.AddWithValue("@tipoAcreditacion", tipoAcreditacion);

                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        int esPrincipalOrdinal = r.GetOrdinal("esprincipal");
                        int emailOrdinal = r.GetOrdinal("email");

                        while (r.Read())
                        {
                            Email e = new Email()
                            {

                                EsPrincipal = r.GetBoolean(esPrincipalOrdinal),
                                Correo = r.GetString(emailOrdinal)

                            };
                            retorno.Add(e);
                        }
                    }
                }
                return retorno;
            }

            catch (Exception e)
            {
                throw e;
            }

        }
    }
}
