using ANS.Model;
using Xunit;

namespace ANS.Tests
{
    /// <summary>
    /// Contrato de la normalización centralizada HSBC -> BTG PACTUAL.
    /// Estos tests fijan el comportamiento retrocompatible: ambos nombres son el MISMO banco lógico.
    /// </summary>
    public class IdentidadBancoTests
    {
        // 1-5: HSBC / hsbc / BTG PACTUAL / btg pactual (+ variantes de caja) resuelven al canónico.
        [Theory]
        [InlineData("HSBC")]
        [InlineData("hsbc")]
        [InlineData("Hsbc")]
        [InlineData("BTG PACTUAL")]
        [InlineData("btg pactual")]
        [InlineData("Btg Pactual")]
        [InlineData("BTGPACTUAL")]
        [InlineData("BTG")]
        public void Normalizar_VariantesDelBanco_ColapsanAlCanonico(string entrada)
        {
            Assert.Equal(IdentidadBanco.BtgPactual, IdentidadBanco.Normalizar(entrada));
            Assert.True(IdentidadBanco.EsBtgPactual(entrada));
        }

        // 6: espacios accidentales (Trim).
        [Theory]
        [InlineData("  HSBC  ")]
        [InlineData("\tBTG PACTUAL ")]
        [InlineData(" hsbc")]
        public void Normalizar_ConEspacios_SeManejaConTrim(string entrada)
        {
            Assert.Equal(IdentidadBanco.BtgPactual, IdentidadBanco.Normalizar(entrada));
        }

        // Otros bancos NO se ven afectados (la normalización es no-op: upper/trim).
        [Theory]
        [InlineData("santander", "SANTANDER")]
        [InlineData("Scotiabank", "SCOTIABANK")]
        [InlineData(" bbva ", "BBVA")]
        [InlineData("itau", "ITAU")]
        [InlineData("bandes", "BANDES")]
        public void Normalizar_OtrosBancos_NoSeVenAfectados(string entrada, string esperado)
        {
            Assert.Equal(esperado, IdentidadBanco.Normalizar(entrada));
            Assert.False(IdentidadBanco.EsBtgPactual(entrada));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Normalizar_NuloOVacio_DevuelveVacioYNoRompe(string? entrada)
        {
            Assert.Equal(string.Empty, IdentidadBanco.Normalizar(entrada!));
            Assert.False(IdentidadBanco.EsBtgPactual(entrada!));
        }

        // Contrato del alias legado para el IN (@banco, @bancoAlias) de las queries.
        [Fact]
        public void AliasLegado_ParaBtgPactual_DevuelveHsbc()
        {
            Assert.Equal(IdentidadBanco.HsbcLegado, IdentidadBanco.AliasLegado(IdentidadBanco.BtgPactual));
            Assert.Equal("HSBC", IdentidadBanco.AliasLegado("BTG PACTUAL"));
        }

        [Theory]
        [InlineData("SANTANDER")]
        [InlineData("BBVA")]
        [InlineData("ITAU")]
        public void AliasLegado_ParaOtrosBancos_DevuelveElMismoNombre_InIndocuo(string banco)
        {
            // Para bancos sin alias el segundo término del IN es igual al primero: no cambia el resultado.
            Assert.Equal(banco, IdentidadBanco.AliasLegado(banco));
        }

        // 10: el nombre VISIBLE siempre es BTG PACTUAL para el ex-HSBC.
        [Theory]
        [InlineData("HSBC")]
        [InlineData("hsbc")]
        [InlineData("BTG PACTUAL")]
        public void NombreVisible_DelExHsbc_EsBtgPactual(string entrada)
        {
            Assert.Equal("BTG PACTUAL", IdentidadBanco.NombreVisible(entrada));
        }

        [Fact]
        public void NombreVisible_DeOtroBanco_QuedaIgual()
        {
            Assert.Equal("Santander", IdentidadBanco.NombreVisible("Santander"));
        }
    }
}
