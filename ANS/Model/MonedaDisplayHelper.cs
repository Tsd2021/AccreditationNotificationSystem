using System;

namespace ANS.Model
{
    /// <summary>
    /// Convierte códigos de moneda/divisa a texto para mostrar en UI.
    /// UYU/PESOS => PESOS; USD/U$S/DOLARES => DÓLARES; EUR => EUROS.
    /// </summary>
    public static class MonedaDisplayHelper
    {
        public static string ToMonedaDisplay(string moneda)
        {
            if (string.IsNullOrWhiteSpace(moneda))
                return moneda ?? string.Empty;

            var m = moneda.Trim();
            if (m.Equals("UYU", StringComparison.OrdinalIgnoreCase) || m.Equals("PESOS", StringComparison.OrdinalIgnoreCase))
                return "PESOS";
            if (m.Equals("USD", StringComparison.OrdinalIgnoreCase) || m.Equals("U$S", StringComparison.OrdinalIgnoreCase)
                || m.Equals("US$", StringComparison.OrdinalIgnoreCase) || m.Equals("DOLARES", StringComparison.OrdinalIgnoreCase))
                return "DÓLARES";
            if (m.Equals("EUR", StringComparison.OrdinalIgnoreCase) || m.Equals("EUROS", StringComparison.OrdinalIgnoreCase))
                return "EUROS";

            return m;
        }
    }
}
