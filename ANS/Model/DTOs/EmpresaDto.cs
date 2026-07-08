using ANS.Model;

namespace ANS.Model.DTOs
{
    /// <summary>
    /// DTO para empresas por buzón. Un ítem por combinación Banco+Empresa+Moneda.
    /// </summary>
    public class EmpresaDto
    {
        public string Empresa { get; set; }
        public int IdCuenta { get; set; }
        public string Cuenta { get; set; }
        public string Moneda { get; set; }
        public string Banco { get; set; }
        public int IdBanco { get; set; }

        // Tipo de buzón (CC.TIPO). 3 == PERMAQUIN. Se usa para transportar TipoBuzon al flujo manual.
        public int TipoBuzon { get; set; }

        /// <summary>
        /// Moneda normalizada para mostrar: PESOS, DÓLARES, EUROS, etc.
        /// </summary>
        public string MonedaDisplay => MonedaDisplayHelper.ToMonedaDisplay(Moneda);

        /// <summary>
        /// "{Banco} {Empresa} - {MonedaDisplay}" para el ComboBox (legacy).
        /// </summary>
        public string DisplayName
        {
            get
            {
                var b = (Banco ?? "").Trim();
                var e = (Empresa ?? "").Trim();
                var main = $"{b} {e}".Trim();
                return string.IsNullOrEmpty(main) ? MonedaDisplay : $"{main} - {MonedaDisplay}";
            }
        }

        /// <summary>
        /// "{Empresa} ({Cuenta}) - {MonedaDisplay}" para mostrar en combo de empresas filtradas por banco.
        /// </summary>
        public string DisplayNameConCuenta
        {
            get
            {
                var e = (Empresa ?? "").Trim();
                var c = (Cuenta ?? "").Trim();
                var cuentaPart = string.IsNullOrEmpty(c) ? "" : $" ({c})";
                return $"{e}{cuentaPart} - {MonedaDisplay}";
            }
        }
    }
}
