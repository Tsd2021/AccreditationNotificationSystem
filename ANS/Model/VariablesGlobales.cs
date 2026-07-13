
namespace ANS.Model
{
    public static class VariablesGlobales
    {
        public const string p2p = "PuntoAPunto";
        public const string tanda = "Tanda";
        public const string diaxdia = "DiaADia";
        public const string bbva = "bbva";
        public const string santander = "santander";
        public const string itau = "itau";
        public const string heritage = "heritage";
        public const string hsbc = "hsbc";            // ALIAS LEGADO temporal (ex nombre del banco). Ver IdentidadBanco.
        public const string btgpactual = "btg pactual"; // Nombre canónico nuevo del banco (ex-HSBC). Ver IdentidadBanco.
        public const string bandes = "bandes";
        public const string brou = "brou";
        public const string scotiabank = "scotiabank";
        public const string uyu = "UYU";
        public const string usd = "USD";
        public const string pesos = "PESOS";
        public const string dolares = "DOLARES";
        public const string montevideo = "MONTEVIDEO";
        public const string maldonado = "MALDONADO";
        public const string cashoffice = "CASHOFFICE";
        public const string endPointTens = "https://uyasdmz02.uy.corp:9982/TenSOnlineTxnWS/services/tenSOnlineTxn";

        // ── Tipo de cuenta (columna TIPO en CUENTASBUZONES) ──────────────────
        public const string tipoCuentaCorriente = "CuentaCorriente";
        public const string tipoCajaAhorro      = "CajaAhorro";

        // BBVA: flag "Producto" columna 19 del layout
        public const char bbvaProductoCC = '1'; // Cuenta Corriente
        public const char bbvaProductoCA = '2'; // Caja de Ahorro

        // Scotia: prefijo del número de cuenta
        public const string scotiaPrefijoCC = "2101"; // Cuenta Corriente
        public const string scotiaPrefijoCA = "2201"; // Caja de Ahorro
        // ─────────────────────────────────────────────────────────────────────
       

        // SANTANDER //
        public static TimeSpan horaCierreSantander_TXT = new TimeSpan(15,30, 0); // no debe incluir Henderson ni de la sierra 
        public static TimeSpan horaCierreSantander_EXCEL = new TimeSpan(15, 35, 0); // incluye todo
        public static TimeSpan horaFinPuntoAPuntoSantander = new TimeSpan(15, 30, 0);
        /////////////////////////////////////////////////////////////////////////////////////////////////////////
   
        // BBVA //
        public static TimeSpan horaCierreBBVA_TXT = new TimeSpan(0, 0, 0);
        public static TimeSpan horaCierreBBVA_EXCEL = new TimeSpan(0, 0, 0);
        /////////////////////////////////////////////////////////////////////////////////////////////////////////

        // SCOTIABANK //
        public static TimeSpan horaCierreScotiabank_TXT = new TimeSpan(0, 0, 0);

        public static TimeSpan horaCierreScotiabank_EXCEL = new TimeSpan(0, 0, 0);

        public static TimeSpan horaCierreScotiabankHendersonTanda1_TXT = new TimeSpan(7, 0, 0);

        public static TimeSpan horaCierreScotiabankHendersonTanda1_EXCEL = new TimeSpan(7, 5, 0);

        public static TimeSpan horaCierreScotiabankHendersonTanda2_TXT = new TimeSpan(14, 30, 0);

        public static TimeSpan horaCierreScotiabankHendersonTanda2_EXCEL = new TimeSpan(14, 35, 0);

        public static TimeSpan horaCierreScotiabankCoboe_TXT = new TimeSpan(2, 0, 0);

        // SCOTIABANK - ABASTO EL PLACER (ID 1015) - DIA A DIA - EXCEPCION DE MAÑANA (de tarde acredita con el DXD normal) //
        public static TimeSpan horaCierreScotiabankAbastoElPlacer_TXT = new TimeSpan(7, 0, 0);
        /////////////////////////////////////////////////////////////////////////////////////////////////////////

        // ITAU //

        ///////////////////////////////////


        // SANTANDER - DE LAS SIERRAS - DIA A DIA //
        public static TimeSpan horaCierreSantanderDeLaSierras_TXT = new TimeSpan(7, 0, 0);
        /////////////////////////////////////////////////////////////////////////////////////////////////////////


        // SANTANDER - HENDERSON - TANDAS //

        public static TimeSpan horaCierreSantanderHENDERSON_TANDA1_TXT = new TimeSpan(7, 0, 0);

        public static TimeSpan horaCierreSantanderHENDERSON_TANDA1_EXCEL = new TimeSpan(7,10,0);

        public static TimeSpan horaCierreSantanderHENDERSON_TANDA2_TXT = new TimeSpan(14, 30, 0);

        public static TimeSpan horaCierreSantanderHENDERSON_TANDA2_EXCEL = new TimeSpan(14, 40, 0);

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
