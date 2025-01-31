using ANS.Model.Interfaces;
using System.Net.Security;
using System.Net;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel;
using System.Windows;
using System.Xml;
using System.IO;
using Microsoft.Web.Services3.Security.Tokens;
using System.Xml.Serialization;

using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security;
using System.Security.Principal;
using System.Text;


namespace ANS.Model.Services
{
    public class ServicioSantander : IServicioSantanderTens
    {
        public static ServicioSantander Instance { get; set; }
       
        public static ServicioSantander getInstancia()
        {
            if (Instance == null)
            {
                Instance = new ServicioSantander();
            }
            return Instance;
        }



        public async Task EnviarArchivoVacioConCliente()
        {

             var credenciales = new NetworkCredential("urprmaetecnisegur", "9Nw$d9aQ");

            // Definir el binding HTTPS con seguridad de transporte
            var binding = new CustomBinding(
                   new TextMessageEncodingBindingElement(MessageVersion.Soap12, Encoding.UTF8),  // Mensajes SOAP 1.2 con UTF-8
                   new HttpsTransportBindingElement  // Transporte HTTPS con autenticación básica
                   {
                       AuthenticationScheme = AuthenticationSchemes.Basic,
                       RequireClientCertificate = false,  // No requiere certificado de cliente
                       MaxReceivedMessageSize = 20000000  // Ajusta el tamaño de mensaje según necesidad
                   }
               );


            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
            {
                if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                    return true;

                Console.WriteLine($"⚠️ Error en el certificado: {sslPolicyErrors}");

                
                if (certificate != null)
                {
                    X509Certificate2 cert2 = new X509Certificate2(certificate);
                    Console.WriteLine($"Certificado recibido: {cert2.Subject}");
                    return true; 
                }

                return false; 
            };

            // Definir el endpoint correcto
            var endpoint = new EndpointAddress("https://uyasdmz02.uy.corp:9982/TenSOnlineTxnWS/services/tenSOnlineTxn");

            try
            {
                // Crear el cliente con el binding y endpoint especificados
                using (var client = new TensStdr.TenSOnlineTxnServiceClient(binding, endpoint))
                {

                    client.ClientCredentials.UserName.UserName = credenciales.UserName;

                    client.ClientCredentials.UserName.Password = credenciales.Password;

                    string filePath = @"C:\TEC_005_20230303021903.dat";

                    byte[] archivo = File.ReadAllBytes(filePath);

                    // Crear el objeto de servicio con el archivo a enviar

                    var txService = new TensStdr.txservice
                    {
                        lotFile = new TensStdr.lotFile
                        {
                            fileName = "TEC_005_20230303021903.dat",
                            fileBytes = archivo
                        },
                        refNumber = "1",
                        waitProcess = true,
                        method = "uploadLotFile"
                    };
                    
                    // Crear la solicitud

                   
   
                    var request = new TensStdr.execute(txService);

                    Console.WriteLine("Enviando solicitud al servicio...");

                    var response = await client.executeAsync(request);
               
                    // Procesar la respuesta
                    if (response?.result != null)
                    {
                        Console.WriteLine($"Código de respuesta: {response.result.code}");

                        Console.WriteLine($"Descripción: {response.result.description}");

                        if (response.result.code == "0")
                        {
                            Console.WriteLine("El archivo vacío se envió correctamente.");
                        }
                        else
                        {
                            Console.WriteLine("Hubo un problema al enviar el archivo vacío.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("La respuesta del servicio fue nula.");
                    }
                }
            }
            catch (TimeoutException tex)
            {
                Console.WriteLine($"Error de tiempo de espera: {tex.Message}");
            }
            catch (CommunicationException cex)
            {
                Console.WriteLine($"Error de comunicación: {cex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar el archivo vacío: {ex.Message}");
            }
        }




        //public TensStdr.transactionResponse EnviarArchivoConClienteWS(string NombreCSV, byte[] Archivo)
        //{
        //    try
        //    {
        //        var credenciales = new NetworkCredential("urprmaetecnisegur", "9Nw$d9aQ", "");
        //        var behavior = new PasswordDigestBehavior(credenciales.UserName, credenciales.Password);
        //        var ClienteTens = CrearCliente();

        //        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        //        ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

        //        ClienteTens.Endpoint.EndpointBehaviors.Add(behavior);

        //        TensStdr.lotFile lotFile = new TensStdr.lotFile()
        //        {
        //            fileName = NombreCSV,
        //            fileBytes = Archivo
        //        };

        //        TensStdr.txservice Consulta = new TensStdr.txservice()
        //        {
        //            lotFile = lotFile,
        //            refNumber = "1",
        //            waitProcess = true,
        //            method = "uploadLotFile"
        //        };

        //        return ClienteTens.execute(Consulta);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error en EnviarArchivoConClienteWS: {ex.Message}");
        //        return null;
        //    }
        //}

        public static TensStdr.transactionResponse TESTEnviarArchivoConClienteWS(string NombreCSV, byte[] Archivo)
        {
            return new TensStdr.transactionResponse();
        }

        public class PasswordDigestMessageInspector : IClientMessageInspector
        {
            public string Username { get; set; }
            public string Password { get; set; }

            public PasswordDigestMessageInspector(string username, string password)
            {
                Username = username;
                Password = password;
            }

            public object BeforeSendRequest(ref System.ServiceModel.Channels.Message request, IClientChannel channel)
            {
                XmlDocument doc = new XmlDocument();

                XmlElement security = doc.CreateElement("wsse", "Security", "http://schemas.xmlsoap.org/ws/2002/12/secext");
                security.SetAttribute("xmlns:wsse", "http://schemas.xmlsoap.org/ws/2002/12/secext");

                XmlElement usernameToken = doc.CreateElement("wsse", "UsernameToken", "http://schemas.xmlsoap.org/ws/2002/12/secext");
                usernameToken.SetAttribute("wsu:Id", "UsernameToken-1");

                XmlElement usernameElement = doc.CreateElement("wsse", "Username", "http://schemas.xmlsoap.org/ws/2002/12/secext");
                usernameElement.InnerText = this.Username;

                XmlElement passwordElement = doc.CreateElement("wsse", "Password", "http://schemas.xmlsoap.org/ws/2002/12/secext");
                passwordElement.SetAttribute("Type", "http://schemas.xmlsoap.org/ws/2002/12/secext#PasswordText");
                passwordElement.InnerText = this.Password;

                usernameToken.AppendChild(usernameElement);
                usernameToken.AppendChild(passwordElement);
                security.AppendChild(usernameToken);

                MessageHeader securityHeader = MessageHeader.CreateHeader(
                    "Security",
                    "http://schemas.xmlsoap.org/ws/2002/12/secext",
                    security,
                    mustUnderstand: false
                );

                request.Headers.Add(securityHeader);
                return null;
            }


            public void AfterReceiveReply(ref System.ServiceModel.Channels.Message reply, object correlationState) { }
        }

        public class PasswordDigestBehavior : IEndpointBehavior
        {
            public string Username { get; set; }
            public string Password { get; set; }

            public PasswordDigestBehavior(string username, string password)
            {
                Username = username;
                Password = password;
            }

            public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters) { }

            public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
            {
                clientRuntime.ClientMessageInspectors.Add(new PasswordDigestMessageInspector(Username, Password));
            }

            public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) { }

            public void Validate(ServiceEndpoint endpoint) { }
        }
    }
}
