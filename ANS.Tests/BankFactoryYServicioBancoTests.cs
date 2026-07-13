using ANS.Model;
using ANS.Model.GeneradorArchivoPorBanco;
using ANS.Model.Services;
using Xunit;

namespace ANS.Tests
{
    /// <summary>
    /// Verifica que HSBC (alias legado) y BTG PACTUAL (canónico) resuelven a la MISMA entidad:
    /// mismo generador en la factory y el mismo Banco en memoria (sin duplicar la entidad).
    /// </summary>
    public class BankFactoryYServicioBancoTests
    {
        // 7: la factory devuelve el mismo generador para ambos nombres (y para variantes de caja/espacios).
        [Theory]
        [InlineData("HSBC")]
        [InlineData("hsbc")]
        [InlineData("BTG PACTUAL")]
        [InlineData("btg pactual")]
        [InlineData("  HSBC ")]
        public void Factory_HsbcYBtgPactual_DevuelvenHsbcFileGenerator(string nombre)
        {
            var generador = BankFactory.GetModoAcreditacionByBanco(nombre, VariablesGlobales.diaxdia);
            Assert.IsType<HSBCFileGenerator>(generador);
        }

        [Fact]
        public void Factory_OtroBanco_SigueResolviendoSuGenerador()
        {
            var generador = BankFactory.GetModoAcreditacionByBanco("scotiabank", VariablesGlobales.diaxdia);
            Assert.IsType<ScotiaFileGenerator>(generador);
        }

        // 8: sembrando UNA sola entidad "BTG PACTUAL", getByNombre la encuentra por ambos nombres
        // (no existen dos bancos en memoria para la misma entidad).
        [Fact]
        public void GetByNombre_ResuelveHsbcYBtgPactualAlMismoBanco()
        {
            var servicio = new ServicioBanco();
            var btg = new Banco(3, IdentidadBanco.BtgPactual);
            servicio.agregar(btg);

            var porLegado = servicio.getByNombre(VariablesGlobales.hsbc);   // "hsbc"
            var porCanonico = servicio.getByNombre(VariablesGlobales.btgpactual); // "btg pactual"
            var porVisible = servicio.getByNombre("BTG PACTUAL");

            Assert.NotNull(porLegado);
            Assert.Same(btg, porLegado);
            Assert.Same(btg, porCanonico);
            Assert.Same(btg, porVisible);
        }

        [Fact]
        public void GetByNombre_NoConfundeConOtroBanco()
        {
            var servicio = new ServicioBanco();
            var btg = new Banco(3, IdentidadBanco.BtgPactual);
            var itau = new Banco(7, "ITAU");
            servicio.agregar(btg);
            servicio.agregar(itau);

            Assert.Same(itau, servicio.getByNombre("itau"));
            Assert.Same(btg, servicio.getByNombre("HSBC"));
        }
    }
}
