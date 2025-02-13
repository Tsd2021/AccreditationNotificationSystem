using ANS.Model.Interfaces;
using System.Net;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel;
using System.Xml;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security;
using System.Net.Http;


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

                // ✅ Binding corregido para autenticación básica
                var binding = new BasicHttpsBinding
            {
                Security = new BasicHttpsSecurity
                {
                    Mode = BasicHttpsSecurityMode.Transport,
                    Message = new BasicHttpMessageSecurity
                    {
                        ClientCredentialType = BasicHttpMessageCredentialType.UserName
                    }
                },
                MaxReceivedMessageSize = 20000000,
                ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max
            };

            // ✅ Definir el endpoint correcto
            var endpoint = new EndpointAddress("https://uyasdmz02.uy.corp:9982/TenSOnlineTxnWS/services/tenSOnlineTxn");

            try
            {
                using (var client = new TensStdr.TenSOnlineTxnServiceClient(binding, endpoint))
                {
                    // ✅ Configurar credenciales de usuario
                    client.ClientCredentials.UserName.UserName = credenciales.UserName;
                    client.ClientCredentials.UserName.Password = credenciales.Password;

                    var pwdBehavior = new PasswordDigestBehavior(credenciales.UserName, credenciales.Password);
                    client.Endpoint.EndpointBehaviors.Add(pwdBehavior);

                    // ✅ Ignorar la validación del certificado del servicio
                    client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication = new X509ServiceCertificateAuthentication()
                    {
                        CertificateValidationMode = X509CertificateValidationMode.None,
                        RevocationMode = X509RevocationMode.NoCheck
                    };

                    // ✅ Leer archivo
                    string filePath = @"C:\TEC_005_20230303021903.dat";
                    if (!File.Exists(filePath))
                    {
                        Console.WriteLine("❌ El archivo no existe.");
                        return;
                    }

                    byte[] archivo = File.ReadAllBytes(filePath);


                    TensStdr.lotFile newLotFile = new TensStdr.lotFile()
                    {
                        fileName = "TEC_005_20230303021903.dat",
                        fileBytes = archivo
                    };


                    // ✅ Crear la solicitud SOAP
                    var txService = new TensStdr.txservice
                    {
                        lotFile = newLotFile,
                        refNumber = "1",
                        waitProcess = true,
                        method = "uploadLotFile"
                    };

                    var request = new TensStdr.execute(txService);

                    Console.WriteLine("📤 Enviando solicitud al servicio...");

                    // ✅ Enviar solicitud
                    AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    HttpClientHandler handler = new HttpClientHandler();
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;
                    HttpClient client2 = new HttpClient(handler);


                    var response = await client.executeAsync(request);

                    // ✅ Procesar la respuesta
                    if (response?.result != null)
                    {
                        Console.WriteLine($"✅ Código de respuesta: {response.result.code}");
                        Console.WriteLine($"✅ Descripción: {response.result.description}");

                        if (response.result.code == "0")
                        {
                            Console.WriteLine("✅ El archivo vacío se envió correctamente.");
                        }
                        else
                        {
                            Console.WriteLine("⚠️ Hubo un problema al enviar el archivo vacío.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("❌ La respuesta del servicio fue nula.");
                    }
                }
            }
            catch (TimeoutException tex)
            {
                Console.WriteLine($"⏳ Error de tiempo de espera: {tex.Message}");
            }
            catch (FaultException faultEx)
            {
                Console.WriteLine("❌ Se produjo un FaultException en el servicio.");
                Console.WriteLine($"Mensaje: {faultEx.Message}");
                if (faultEx.Code != null)
                {
                    Console.WriteLine($"Código del Fault: {faultEx.Code.Name}");
                }
                // Si existe información adicional en el detail, se puede intentar mostrar
                // Nota: Si se utiliza FaultException<T> se puede extraer el detail de forma tipada.
            }
            catch (CommunicationException cex)
            {
                Console.WriteLine($"📡 Error de comunicación: {cex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al enviar el archivo vacío: {ex.Message}");
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
        public class PasswordDigestMessageInspector : IClientMessageInspector
        {
            public string Username { get; set; }
            public string Password { get; set; }

            public PasswordDigestMessageInspector(string username, string password)
            {
                Username = username;
                Password = password;
            }

            public object BeforeSendRequest(ref Message request, IClientChannel channel)
            {
                XmlDocument doc = new XmlDocument();

                // Crear el elemento Security con el espacio de nombres WS-Security
                XmlElement security = doc.CreateElement("wsse", "Security", "http://schemas.xmlsoap.org/ws/2002/12/secext");
                // Declarar los espacios de nombres para wsse y wsu
                security.SetAttribute("xmlns:wsse", "http://schemas.xmlsoap.org/ws/2002/12/secext");
                security.SetAttribute("xmlns:wsu", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");

                // Crear el UsernameToken
                XmlElement usernameToken = doc.CreateElement("wsse", "UsernameToken", "http://schemas.xmlsoap.org/ws/2002/12/secext");

                // En lugar de SetAttribute con prefijo, creamos el atributo con CreateAttribute:
                XmlAttribute idAttr = doc.CreateAttribute("wsu", "Id", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");
                idAttr.Value = "UsernameToken-1";
                usernameToken.Attributes.Append(idAttr);

                // Crear el elemento Username
                XmlElement usernameElement = doc.CreateElement("wsse", "Username", "http://schemas.xmlsoap.org/ws/2002/12/secext");
                usernameElement.InnerText = this.Username;

                // Crear el elemento Password
                XmlElement passwordElement = doc.CreateElement("wsse", "Password", "http://schemas.xmlsoap.org/ws/2002/12/secext");
                passwordElement.SetAttribute("Type", "http://schemas.xmlsoap.org/ws/2002/12/secext#PasswordText");
                passwordElement.InnerText = this.Password;

                // Construir la estructura
                usernameToken.AppendChild(usernameElement);
                usernameToken.AppendChild(passwordElement);
                security.AppendChild(usernameToken);

                // Crear el header de seguridad y agregarlo al mensaje
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
