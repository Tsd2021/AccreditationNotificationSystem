using Microsoft.Data.SqlClient;
using MimeKit;
using MimeKit.Utils;
using System.Data;

namespace ANS.Model.Services
{
    public class ServicioNiveles
    {

        private string _conexion = ConfiguracionGlobal.ConexionWebBuzones;
        public static ServicioNiveles instancia { get; set; }
        public static ServicioNiveles getInstancia()
        {
            if (instancia == null)
            {
                instancia = new ServicioNiveles();
            }
            return instancia;
        }
        public async Task ProcesarNotificacionesPorDesconexion()
        {
            try
            {


                List<Buzon> buzonesDesconectados = await GetBuzonesDesconectados();

                if (buzonesDesconectados == null || buzonesDesconectados.Count <= 0) return;

                MailKit.Net.Smtp.SmtpClient smtpClient = await ServicioEmail.instancia.getNewSmptClient();

                foreach (Buzon unBuzon in buzonesDesconectados)
                {
                    MimeMessage mail = CrearMensajeDesconexion(unBuzon);

                    await ServicioEmail.instancia.EnviarMailDesconexion(mail, smtpClient);
                }

                await smtpClient.DisconnectAsync(true);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error al procesar notificaciones por desconexión: " + e.Message);
            }
        }
        public MimeMessage CrearMensajeDesconexion(Buzon buz)
        {


            int horasEnteras = (int)Math.Floor(buz.HorasDesconectado);
            var msg = new MimeMessage();

            msg.From.Add(MailboxAddress.Parse("dchiquiar@tecnisegur.com.uy"));
            msg.To.Add(MailboxAddress.Parse("dchiquiar@tecnisegur.com.uy"));
            msg.Subject = $"Notificación: Buzón [{buz.NN}] desconectado!";

            var builder = new BodyBuilder();
            var firma = builder.LinkedResources.Add("Images/FirmaDiegoMail.png");
            firma.ContentId = MimeUtils.GenerateMessageId();
            // Cuerpo HTML personalizado:
            builder.HtmlBody = $@"
                        <html>
                          <body style=""font-family: Arial, sans-serif; color: #333333; line-height: 1.5;"">
                            <h2 style=""color: #004080; margin-bottom: 1em;"">Notificación de desconexión de buzón</h2>

                            <p>Estimado cliente,</p>

                            <p>
                              Por este medio le informamos que su buzón 
                              <strong style=""color: #000;"">{buz.NN}</strong> se encuentra 
                              <strong style=""color: #d9534f;"">desconectado</strong> desde el 
                              <em>{buz.UltimaVezConectado:dd/MM/yyyy HH:mm}</em>.
                            </p>

                            <p>
                              En total, hace 
                              <strong style=""color: #000;"">{horasEnteras}</strong> 
                              horas que está fuera de línea.
                            </p>

                            <p>
                              Por favor, verifique su equipo y proceda a reconectarse a la brevedad.
                              Si presenta inconvenientes o necesita asistencia, no dude en ponerse 
                              en contacto con nosotros.
                            </p>

                            <p>Muchas gracias por su atención.</p>

                            <p style=""margin-top: 2em;"">
                              Saludos cordiales,<br/>
                              <strong>Equipo TECNISEGUR</strong>
                            </p>
                          <div style=""text-align:left; margin-top:30px;"">
                              <img 
                                src=""cid:{firma.ContentId}"" 
                                style=""max-width:300px; height:auto;"" 
                                alt=""Firma Diego"" />
                            </div>
                          </body>
                        </html>";





            msg.Body = builder.ToMessageBody();

            return msg;
        }
        public async Task<List<Buzon>> GetBuzonesDesconectados()
        {

            var buzonesDesconectados = new List<Buzon>();

            using (var conn = new SqlConnection(_conexion))
            {
                await conn.OpenAsync();


                string query = @"SELECT n.codigobuzon,n.fechaultconex,c.email,c.nn
                                FROM Niveles n
                                inner join cc c on n.CodigoBuzon = c.NC
                                WHERE  c.estado = 'ALTA'
                                and DATEDIFF(hour, FechaUltConex, GETDATE()) >= 4;";

                SqlCommand cmd = new SqlCommand(query, conn);

                using (SqlDataReader r = await cmd.ExecuteReaderAsync())
                {

                    int ncOrdinal = r.GetOrdinal("codigobuzon");
                    int fechaUltConexOrdinal = r.GetOrdinal("fechaultconex");
                    int emailOrdinal = r.GetOrdinal("email");
                    int nnOrdinal = r.GetOrdinal("nn");

                    while (r.Read())
                    {

                        Buzon buzonDesconectado = new Buzon
                        {
                            NC = r.GetString(ncOrdinal),
                            UltimaVezConectado = r.GetDateTime(fechaUltConexOrdinal),
                            Email = r.GetString(emailOrdinal),
                            NN = r.GetString(nnOrdinal)
                        };

                        buzonDesconectado.estaOnline();
                        buzonesDesconectados.Add(buzonDesconectado);
                    }
                }
            }
            return buzonesDesconectados;
        }
    }
}
    



