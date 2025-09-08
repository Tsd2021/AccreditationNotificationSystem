// ANS.Scheduling/QuartzTime.cs
using System;
using System.Collections.Generic;

namespace ANS.Scheduling
{
    public static class QuartzTime
    {
        // por defecto, Montevideo (cambialo si querés)
        private static TimeZoneInfo _defaultTz = Resolve("Montevideo Standard Time");

        public static TimeZoneInfo DefaultTz => _defaultTz;

        /// <summary>Setea la TZ por defecto (IANA o Windows ID).</summary>
        public static void SetDefault(string tzId)
        {
            _defaultTz = Resolve(tzId);
        }

        /// <summary>Resuelve una TZ intentando IANA y Windows; fallback a Local.</summary>
        public static TimeZoneInfo Resolve(string tzId)
        {
            // 1) Windows ID directo
            try { return TimeZoneInfo.FindSystemTimeZoneById(tzId); }
            catch { /* ignorar y probar abajo */ }

            // 2) Mapeo mínimo IANA -> Windows (agregá los que necesites)
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["America/Montevideo"] = "Montevideo Standard Time",
                ["America/Buenos_Aires"] = "Argentina Standard Time",
                ["America/Sao_Paulo"] = "E. South America Standard Time"
            };
            if (map.TryGetValue(tzId, out var windowsId))
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(windowsId); }
                catch { /* continuar */ }
            }

            // 3) Fallback
            return TimeZoneInfo.Local;
        }
    }
}
