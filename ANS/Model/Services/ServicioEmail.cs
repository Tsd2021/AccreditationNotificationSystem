using ANS.Model.Interfaces;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Windows.Interop;

namespace ANS.Model.Services
{
    public class ServicioEmail : IServicioEmail
    {
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
                LinkedResource inlineImage = new LinkedResource("Images/FirmaDiegoTecnisegur.jpeg", MediaTypeNames.Image.Jpeg)
                {
                    ContentId = "bannerImage",
                    TransferEncoding = TransferEncoding.Base64
                };

                avHtml.LinkedResources.Add(inlineImage);

                // Configuración de remitente y destinatario
                string remitente = "dchiquiar@tecnisegur.com.uy";
                string contrasena = "D23012025";
                string destinatario = "dchiquiar@tecnisegur.com.uy";

                using (MailMessage mail = new MailMessage(remitente, destinatario))
                {
                    mail.Subject = asunto;
                    mail.Body = cuerpo;
                    mail.IsBodyHtml = true;
                    mail.AlternateViews.Add(avHtml);

                    //mail.CC.Add("pablo@tecnisegur.com.uy");

                    // Adjunta el archivo Excel
                    mail.Attachments.Add(new Attachment(excelPath));

                    // Configura el cliente SMTP
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



        public async Task<bool> enviarExcelPorMailMasivo(string excelPath, string asunto, string cuerpo,string destino)
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
                LinkedResource inlineImage = new LinkedResource("Images/FirmaDiegoTecnisegur.jpeg", MediaTypeNames.Image.Jpeg)
                {
                    ContentId = "bannerImage",
                    TransferEncoding = TransferEncoding.Base64
                };

                avHtml.LinkedResources.Add(inlineImage);

                // Configuración de remitente y destinatario
                string remitente = "dchiquiar@tecnisegur.com.uy";
                string contrasena = "D23012025";
                string destinatario = destino;

                using (MailMessage mail = new MailMessage(remitente, destinatario))
                {
                    mail.Subject = asunto;
                    mail.Body = cuerpo;
                    mail.IsBodyHtml = true;
                    mail.AlternateViews.Add(avHtml);

                    //mail.CC.Add("pablo@tecnisegur.com.uy");

                    // Adjunta el archivo Excel
                    mail.Attachments.Add(new Attachment(excelPath));

                    // Configura el cliente SMTP
                    SmtpClient clienteSmtp = new SmtpClient("smtp.gmail.com")
                    {
                        Port = 587,
                        Credentials = new NetworkCredential(remitente, contrasena),
                        UseDefaultCredentials = false,
                        EnableSsl = true
                    };

                    clienteSmtp.Send(mail);


                    await Task.Delay(100);

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
    }
}
