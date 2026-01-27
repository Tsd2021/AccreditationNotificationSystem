using ANS.Model.Interfaces;
using ANS.Runtime.Guards;
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
        // ✅ Thread-safe: Lazy<T> garantiza inicialización única
        // Usado para comunicación con servicios web de Santander, thread-safety previene múltiples conexiones
        private static readonly Lazy<ServicioSantander> _lazy = 
            new Lazy<ServicioSantander>(() => new ServicioSantander());
        
        public static ServicioSantander Instance => _lazy.Value;
        
        public static ServicioSantander getInstancia()
        {
            return _lazy.Value;
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
                        // ✅ Logging mejorado: Archivo no encontrado
                        ServicioLog.instancia.WriteError($"El archivo no existe: {filePath}", 
                            "ServicioSantander | EnviarArchivoVacioConCliente");
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

                    // ✅ Logging informativo: Inicio de envío
                    ServicioLog.instancia.WriteInfo("Enviando solicitud al servicio Santander", 
                        "ServicioSantander | EnviarArchivoVacioConCliente");

                    // ✅ GUARDIA: Bloquear operaciones de red en TEST (punto más bajo posible)
                    WebServiceGuard.EnsureNetworkAllowed("Santander WebService", "EnviarArchivoVacioConCliente");

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
                        // ✅ Logging informativo: Respuesta recibida
                        ServicioLog.instancia.WriteInfo($"Respuesta Santander | Código: {response.result.code} | Descripción: {response.result.description}", 
                            "ServicioSantander | EnviarArchivoVacioConCliente");

                        if (response.result.code == "0")
                        {
                            ServicioLog.instancia.WriteInfo("El archivo vacío se envió correctamente a Santander", 
                                "ServicioSantander | EnviarArchivoVacioConCliente");
                        }
                        else
                        {
                            // ✅ Logging de advertencia: Respuesta con código de error
                            ServicioLog.instancia.WriteWarning($"Problema al enviar archivo vacío | Código: {response.result.code} | Descripción: {response.result.description}", 
                                "ServicioSantander | EnviarArchivoVacioConCliente");
                        }
                    }
                    else
                    {
                        // ✅ Logging de error: Respuesta nula
                        ServicioLog.instancia.WriteError("La respuesta del servicio Santander fue nula", 
                            "ServicioSantander | EnviarArchivoVacioConCliente");
                    }
                }
            }
            catch (TimeoutException tex)
            {
                // ✅ Logging mejorado: Timeout específico
                ServicioLog.instancia.WriteError($"Error de tiempo de espera: {tex.Message}", 
                    "ServicioSantander | EnviarArchivoVacioConCliente");
            }
            catch (FaultException faultEx)
            {
                // ✅ Logging mejorado: FaultException con código
                string faultCode = faultEx.Code != null ? faultEx.Code.Name : "Sin código";
                ServicioLog.instancia.WriteError($"FaultException en servicio Santander | Mensaje: {faultEx.Message} | Código: {faultCode}", 
                    "ServicioSantander | EnviarArchivoVacioConCliente");
            }
            catch (CommunicationException cex)
            {
                // ✅ Logging mejorado: Error de comunicación
                ServicioLog.instancia.WriteError($"Error de comunicación: {cex.Message}", 
                    "ServicioSantander | EnviarArchivoVacioConCliente");
            }
            catch (Exception ex)
            {
                // ✅ Logging mejorado: Error general con stack trace completo
                ServicioLog.instancia.WriteLog(ex, "Santander", "Enviar Archivo Vacío con Cliente");
            }
        }
        /// <summary>
        /// ✅ Envía un archivo al servicio SOAP de Santander usando los parámetros recibidos.
        /// Basado en la estructura y buenas prácticas de EnviarArchivoVacioConCliente.
        /// </summary>
        /// <param name="nombreArchivo">Nombre del archivo a enviar</param>
        /// <param name="archivo">Contenido del archivo en bytes</param>
        /// <returns>True si el envío fue exitoso (código de respuesta "0"), False en caso contrario</returns>
        public async Task<bool> EnviarArchivoConClienteWS(string nombreArchivo, byte[] archivo)
        {
            // ✅ GUARDIA: Bloquear WebService de Santander en modo TEST
            WebServiceGuard.EnsureSantanderWebServiceAllowed($"EnviarArchivoConClienteWS - Archivo: {nombreArchivo}");

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
                // ✅ Validación de parámetros
                if (string.IsNullOrWhiteSpace(nombreArchivo))
                {
                    ServicioLog.instancia.WriteError("El nombre del archivo no puede estar vacío", 
                        "ServicioSantander | EnviarArchivoConClienteWS");
                    return false;
                }

                if (archivo == null || archivo.Length == 0)
                {
                    ServicioLog.instancia.WriteError("El archivo no puede ser null o vacío", 
                        "ServicioSantander | EnviarArchivoConClienteWS");
                    return false;
                }

                // ✅ Logging informativo: Inicio del proceso
                ServicioLog.instancia.WriteInfo($"Iniciando envío de archivo: {nombreArchivo} | Tamaño: {archivo.Length} bytes", 
                    "ServicioSantander | EnviarArchivoConClienteWS");

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

                    // ✅ Crear el objeto lotFile con los parámetros recibidos
                    TensStdr.lotFile newLotFile = new TensStdr.lotFile()
                    {
                        fileName = nombreArchivo,
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

                    // ✅ Logging informativo: Inicio de envío
                    ServicioLog.instancia.WriteInfo("Enviando solicitud al servicio Santander", 
                        "ServicioSantander | EnviarArchivoConClienteWS");

                    // ✅ GUARDIA: Bloquear operaciones de red en TEST (punto más bajo posible)
                    WebServiceGuard.EnsureNetworkAllowed("Santander WebService", $"HttpClient para {nombreArchivo}");

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
                        // ✅ Logging informativo: Respuesta recibida
                        ServicioLog.instancia.WriteInfo($"Respuesta Santander | Código: {response.result.code} | Descripción: {response.result.description}", 
                            "ServicioSantander | EnviarArchivoConClienteWS");

                        if (response.result.code == "0")
                        {
                            ServicioLog.instancia.WriteInfo($"El archivo {nombreArchivo} se envió correctamente a Santander", 
                                "ServicioSantander | EnviarArchivoConClienteWS");
                            return true; // ✅ Éxito: código "0"
                        }
                        else
                        {
                            // ✅ Logging de advertencia: Respuesta con código de error
                            ServicioLog.instancia.WriteWarning($"Problema al enviar archivo {nombreArchivo} | Código: {response.result.code} | Descripción: {response.result.description}", 
                                "ServicioSantander | EnviarArchivoConClienteWS");
                            return false; // ❌ Error: código diferente de "0"
                        }
                    }
                    else
                    {
                        // ✅ Logging de error: Respuesta nula
                        ServicioLog.instancia.WriteError("La respuesta del servicio Santander fue nula", 
                            "ServicioSantander | EnviarArchivoConClienteWS");
                        return false; // ❌ Error: respuesta nula
                    }
                }
            }
            catch (TimeoutException tex)
            {
                // ✅ Logging mejorado: Timeout específico
                ServicioLog.instancia.WriteError($"Error de tiempo de espera: {tex.Message}", 
                    "ServicioSantander | EnviarArchivoConClienteWS");
                return false; // ❌ Error: timeout
            }
            catch (FaultException faultEx)
            {
                // ✅ Logging mejorado: FaultException con código
                string faultCode = faultEx.Code != null ? faultEx.Code.Name : "Sin código";
                ServicioLog.instancia.WriteError($"FaultException en servicio Santander | Mensaje: {faultEx.Message} | Código: {faultCode}", 
                    "ServicioSantander | EnviarArchivoConClienteWS");
                return false; // ❌ Error: FaultException
            }
            catch (CommunicationException cex)
            {
                // ✅ Logging mejorado: Error de comunicación
                ServicioLog.instancia.WriteError($"Error de comunicación: {cex.Message}", 
                    "ServicioSantander | EnviarArchivoConClienteWS");
                return false; // ❌ Error: comunicación
            }
            catch (Exception ex)
            {
                // ✅ Logging mejorado: Error general con stack trace completo
                ServicioLog.instancia.WriteLog(ex, "Santander", $"Enviar Archivo con Cliente WS | Archivo: {nombreArchivo}");
                return false; // ❌ Error: excepción general
            }
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
