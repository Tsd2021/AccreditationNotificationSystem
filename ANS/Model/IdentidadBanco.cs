namespace ANS.Model
{
    /// <summary>
    /// Identidad canónica de bancos y normalización centralizada de nombres.
    ///
    /// Migración HSBC -> BTG PACTUAL (el banco fue adquirido y cambió de nombre comercial):
    ///   - Nombre canónico / visible NUEVO: "BTG PACTUAL".
    ///   - Alias legado TEMPORAL: "HSBC" (valor histórico en la BD y en logs anteriores).
    ///   - Ambos nombres resuelven al MISMO banco lógico (no se duplican jobs, cuentas ni acreditaciones).
    ///
    /// Esta clase es el único lugar donde vive el mapeo del alias. Cuando la base de datos
    /// termine de migrarse (ver "Contrato de migración de base de datos"), se retira el alias
    /// "HSBC" de <see cref="Normalizar"/> y <see cref="AliasLegado"/> y el resto del código
    /// sigue funcionando sin tocarse (Etapa D del plan de migración).
    /// </summary>
    public static class IdentidadBanco
    {
        /// <summary>Nombre canónico nuevo. Es también el nombre visible para el usuario (UI, emails, mensajes).</summary>
        public const string BtgPactual = "BTG PACTUAL";

        /// <summary>Alias legado temporal. Valor histórico almacenado en la BD y en los logs previos a la migración.</summary>
        public const string HsbcLegado = "HSBC";

        /// <summary>
        /// Normaliza un nombre de banco a su forma canónica (MAYÚSCULAS, sin espacios sobrantes).
        /// Todas las variantes de HSBC y de BTG PACTUAL colapsan a <see cref="BtgPactual"/>.
        /// Los demás bancos se devuelven en upper/trim (la normalización es un no-op para ellos).
        /// Nunca devuelve null.
        /// </summary>
        public static string Normalizar(string banco)
        {
            if (string.IsNullOrWhiteSpace(banco))
                return string.Empty;

            var b = banco.Trim().ToUpperInvariant();
            switch (b)
            {
                case "HSBC":
                case "BTG":
                case "BTG PACTUAL":
                case "BTGPACTUAL":
                    return BtgPactual;
                default:
                    return b;
            }
        }

        /// <summary>¿El nombre recibido (en cualquier variante) refiere al banco BTG PACTUAL (ex-HSBC)?</summary>
        public static bool EsBtgPactual(string banco) => Normalizar(banco) == BtgPactual;

        /// <summary>
        /// Alias legado del banco para usar como segundo término en filtros SQL <c>IN (@banco, @bancoAlias)</c>,
        /// de modo que las consultas toleren la BD sin migrar (valor histórico) y la migrada a la vez.
        /// Para BTG PACTUAL devuelve "HSBC"; para cualquier otro banco devuelve el mismo nombre
        /// (el segundo término del IN queda igual al primero: inocuo, no cambia el resultado).
        /// </summary>
        public static string AliasLegado(string banco)
            => EsBtgPactual(banco) ? HsbcLegado : (banco ?? string.Empty);

        /// <summary>Nombre visible para UI, emails y mensajes. Para HSBC/BTG devuelve "BTG PACTUAL"; para el resto, el nombre tal cual.</summary>
        public static string NombreVisible(string banco)
            => EsBtgPactual(banco) ? BtgPactual : (banco ?? string.Empty);
    }
}
