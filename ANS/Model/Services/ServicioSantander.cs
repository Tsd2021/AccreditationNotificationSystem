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
        public void EnviarArchivoVacioConCliente()
        {
            try
            {
                var credenciales = new NetworkCredential("urprmaetecnisegur", "9Nw$d9aQ", "");

                var behavior = new PasswordDigestBehavior(credenciales.UserName, credenciales.Password);
                
                TensStdr.TenSOnlineTxnServiceClient ClienteTens = new TensStdr.TenSOnlineTxnServiceClient("TenSOnlineTxnServicePort");

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(
                 delegate
                 {
                     return true;
                 });

                ClienteTens.Endpoint.EndpointBehaviors.Add(behavior);

                string filePath = @"C:\TEC_005_20230303021903.dat";

                byte[] archivo = File.ReadAllBytes(filePath);

                TensStdr.lotFile lotFile = new TensStdr.lotFile()
                {
                    fileName = "TEC_005_20230303021903.dat",

                    fileBytes = archivo
                };

                TensStdr.txservice Consulta = new TensStdr.txservice();
                Consulta.lotFile = lotFile;
                Consulta.refNumber = "1";
                Consulta.waitProcess = true;
                Consulta.method = "uploadLotFile";

                TensStdr.transactionResponse Response = ClienteTens.execute(Consulta);

                MessageBox.Show(Response.description);

            }
            catch (FaultException greetingFault)
            {
                Console.WriteLine(greetingFault.Message);
                Console.ReadLine();
            }
        }

        public TensStdr.transactionResponse EnviarArchivoConClienteWS(string NombreCSV, byte[] Archivo)
        {
            TensStdr.transactionResponse Response = new TensStdr.transactionResponse();

            try
            {
                var credenciales = new NetworkCredential("urprmaetecnisegur", "9Nw$d9aQ", "");

                var behavior = new PasswordDigestBehavior(credenciales.UserName, credenciales.Password);

                TensStdr.TenSOnlineTxnServiceClient ClienteTens = new TensStdr.TenSOnlineTxnServiceClient("TenSOnlineTxnServicePort");
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(
                  delegate
                  {
                      return true;
                  });

                ClienteTens.Endpoint.EndpointBehaviors.Add(behavior);

                TensStdr.lotFile lotFile = new TensStdr.lotFile()
                {
                    fileName = NombreCSV,
                    fileBytes = Archivo
                };

                TensStdr.txservice Consulta = new TensStdr.txservice();
                Consulta.lotFile = lotFile;
                Consulta.refNumber = "1";
                Consulta.waitProcess = true;
                Consulta.method = "uploadLotFile";

                Response = ClienteTens.execute(Consulta);

                return Response;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static TensStdr.transactionResponse TESTEnviarArchivoConClienteWS(string NombreCSV, byte[] Archivo)
        {
            TensStdr.transactionResponse Response = new TensStdr.transactionResponse();

            try
            {
                Response.code = "0";
                return Response;
            }
            catch (Exception ex)
            {
                return null;
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
                UsernameToken token = new UsernameToken(this.Username, this.Password, PasswordOption.SendPlainText);

                XmlElement securityToken = token.GetXml(new XmlDocument());

                MessageHeader securityHeader = MessageHeader.CreateHeader("Security", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd", securityToken, false);

                request.Headers.Add(securityHeader);

                return Convert.DBNull;
            }

            public void AfterReceiveReply(ref System.ServiceModel.Channels.Message reply, object correlationState)
            {
                return;
            }
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

            public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
            {
                return;
            }

            public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
            {
                clientRuntime.ClientMessageInspectors.Add(new PasswordDigestMessageInspector(Username, Password));
            }

            public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
            {
                return;
            }

            public void Validate(ServiceEndpoint endpoint)
            {
                return;
            }
        }
    }
}
