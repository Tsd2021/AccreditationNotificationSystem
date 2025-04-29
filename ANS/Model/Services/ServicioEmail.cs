using ANS.Model.Interfaces;
using ClosedXML.Parser;
using DocumentFormat.OpenXml.Wordprocessing;
using MailKit.Security;
using MimeKit;
using MimeKit.Utils;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using ContentType = MimeKit.ContentType;


namespace ANS.Model.Services
{
    public class ServicioEmail : IServicioEmail
    {


        string remitente = "dchiquiar@tecnisegur.com.uy";
        string contrasena = "cfsg xikf qjwp iwzu";
        string destinatario = "dchiquiar@tecnisegur.com.uy";
        public static ServicioEmail instancia { get; set; }

        public static ServicioEmail getInstancia()
        {
            if(instancia == null)
            {
                instancia = new ServicioEmail();
            }
            return instancia;   
        }


        public async Task<MailKit.Net.Smtp.SmtpClient> getNewSmptClient()
        {
            var smtp = new MailKit.Net.Smtp.SmtpClient();

            smtp.Timeout = 5 * 60 * 1000;

            await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync("dchiquiar@tecnisegur.com.uy", "cfsg xikf qjwp iwzu");

            return smtp;
        }

        public bool enviarExcelPorMail(string excelPath, string asunto, string cuerpo)
        {
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
       
        public async Task<bool> EnviarMailDesconexion(MimeMessage msg,MailKit.Net.Smtp.SmtpClient smtpClient)
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




        public async Task<bool> EnviarExcelPorMailMasivoConMailKit(Stream excelStream,string fileName,string subject,string body,string destino, MailKit.Net.Smtp.SmtpClient smtpClient)
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
    }
}
