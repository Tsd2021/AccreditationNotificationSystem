namespace ANS.Runtime
{
    /// <summary>
    /// Modo de ejecución de la aplicación
    /// </summary>
    public enum RuntimeMode
    {
        /// <summary>
        /// Modo de producción: usa recursos reales (BD prod, shares, WS, emails reales)
        /// </summary>
        Production = 0,

        /// <summary>
        /// Modo de test: usa recursos locales/aislados (BD test, carpetas locales, NO WS, emails a acreditaciones@tecnisegur.com.uy)
        /// </summary>
        Test = 1
    }
}
