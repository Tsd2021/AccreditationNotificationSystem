using System;
using System.Collections.Generic;

namespace ANS.Runtime
{
    /// <summary>
    /// Configuración tipada por ambiente (Production/Test)
    /// </summary>
    public class AppSettings
    {
        public ConnectionStringsSettings ConnectionStrings { get; set; } = new ConnectionStringsSettings();
        public SqliteSettings Sqlite { get; set; } = new SqliteSettings();
        public PathsSettings Paths { get; set; } = new PathsSettings();
        public EmailSettings Email { get; set; } = new EmailSettings();
        public WebServiceSettings WebServices { get; set; } = new WebServiceSettings();
    }

    public class ConnectionStringsSettings
    {
        /// <summary>Connection string para BD TSD de producción</summary>
        public string SqlServerProd { get; set; }

        /// <summary>Connection string para BD TSD de test (réplica)</summary>
        public string SqlServerTest { get; set; }

        /// <summary>Connection string para BD TSD22 de producción</summary>
        public string SqlServer22Prod { get; set; }

        /// <summary>Connection string para BD TSD22 de test</summary>
        public string SqlServer22Test { get; set; }

        /// <summary>Connection string para BD ENCUESTA de producción</summary>
        public string EncuestaProd { get; set; }

        /// <summary>Connection string para BD ENCUESTA de test</summary>
        public string EncuestaTest { get; set; }

        /// <summary>Connection string para BD WEBBUZONES de producción</summary>
        public string WebBuzonesProd { get; set; }

        /// <summary>Connection string para BD WEBBUZONES de test</summary>
        public string WebBuzonesTest { get; set; }
    }

    public class SqliteSettings
    {
        /// <summary>Carpeta base para SQLite en producción</summary>
        public string BaseFolderProd { get; set; }

        /// <summary>Carpeta base para SQLite en test (ej: %LocalAppData%\ANS\Test\sqlite\)</summary>
        public string BaseFolderTest { get; set; }

        /// <summary>Nombre de archivo SQLite en producción</summary>
        public string FileNameProd { get; set; } = "QuartzRuns.db";

        /// <summary>Nombre de archivo SQLite en test</summary>
        public string FileNameTest { get; set; } = "ans_test.db";
    }

    public class PathsSettings
    {
        /// <summary>Ruta raíz para outputs de bancos en producción (shares/red)</summary>
        public string BankOutputRootProd { get; set; }

        /// <summary>Ruta raíz para outputs de bancos en test (local, ej: %LocalAppData%\ANS\Test\Bancos\)</summary>
        public string BankOutputRootTest { get; set; }

        /// <summary>Ruta base para Excel en producción</summary>
        public string ExcelOutputRootProd { get; set; }

        /// <summary>Ruta base para Excel en test</summary>
        public string ExcelOutputRootTest { get; set; }

        /// <summary>Ruta base para logs en producción</summary>
        public string LogsRootProd { get; set; }

        /// <summary>Ruta base para logs en test</summary>
        public string LogsRootTest { get; set; }

        // Rutas específicas por banco (se resuelven desde BankOutputRoot + subcarpetas)
        public Dictionary<string, string> SantanderPathsProd { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> SantanderPathsTest { get; set; } = new Dictionary<string, string>();

        public Dictionary<string, string> ScotiabankPathsProd { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> ScotiabankPathsTest { get; set; } = new Dictionary<string, string>();

        // Otros bancos si aplican
        public Dictionary<string, Dictionary<string, string>> OtherBanksPathsProd { get; set; } = new Dictionary<string, Dictionary<string, string>>();
        public Dictionary<string, Dictionary<string, string>> OtherBanksPathsTest { get; set; } = new Dictionary<string, Dictionary<string, string>>();
    }

    public class EmailSettings
    {
        /// <summary>Destinatario único para todos los emails en modo TEST (whitelist)</summary>
        public string OverrideRecipientInTest { get; set; } = "acreditaciones@tecnisegur.com.uy";

        /// <summary>Si true, en TEST se agrega prefijo al subject indicando modo TEST</summary>
        public bool AddTestPrefixToSubject { get; set; } = true;

        /// <summary>Si true, en TEST se agrega al body la lista de destinatarios originales</summary>
        public bool IncludeOriginalRecipientsInBody { get; set; } = true;

        /// <summary>Si true, permite conexión SMTP real en modo TEST (default: false, seguro)</summary>
        public bool TestAllowSmtp { get; set; } = false;

        /// <summary>Lista de emails permitidos en TEST (whitelist, CSV, default: acreditaciones@tecnisegur.com.uy)</summary>
        public string TestEmailWhitelist { get; set; } = "acreditaciones@tecnisegur.com.uy";
    }

    public class WebServiceSettings
    {
        /// <summary>HARD FALSE: Santander WebService NUNCA debe consumirse en TEST</summary>
        public bool SantanderEnabledInTest { get; set; } = false;

        /// <summary>Endpoint de Santander en producción</summary>
        public string SantanderEndpointProd { get; set; }

        /// <summary>Endpoint de Santander en test (no se usa, pero se define para completitud)</summary>
        public string SantanderEndpointTest { get; set; }

        /// <summary>Credenciales de Santander (usuario)</summary>
        public string SantanderUsername { get; set; }

        /// <summary>Credenciales de Santander (password)</summary>
        public string SantanderPassword { get; set; }
    }
}
