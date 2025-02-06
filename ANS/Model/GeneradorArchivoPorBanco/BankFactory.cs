using ANS.Model.Interfaces;

namespace ANS.Model.GeneradorArchivoPorBanco
{
    public static class BankFactory
    {
        public static IBancoModoAcreditacion GetModoAcreditacionByBanco(string banco, string tipoAcreditacion)
        {

            return banco.ToLower() switch
            {

                VariablesGlobales.scotiabank => new ScotiaFileGenerator(new ConfiguracionAcreditacion(tipoAcreditacion)),

                VariablesGlobales.santander => new SantanderFileGenerator(new ConfiguracionAcreditacion(tipoAcreditacion)),

                VariablesGlobales.bbva => new BBVAFileGenerator(new ConfiguracionAcreditacion(tipoAcreditacion)),

                VariablesGlobales.brou => new BROUFileGenerator(new ConfiguracionAcreditacion(tipoAcreditacion)),

                VariablesGlobales.heritage => new HeritageFileGenerator(),

                VariablesGlobales.hsbc => new HSBCFileGenerator(new ConfiguracionAcreditacion(tipoAcreditacion)),

                VariablesGlobales.itau => new ItauFileGenerator(new ConfiguracionAcreditacion(tipoAcreditacion)),

                VariablesGlobales.bandes => new BandesFileGenerator(new ConfiguracionAcreditacion(tipoAcreditacion)),

                _ => throw new Exception($"No se encontró un generador para el banco: {banco}")

            };

        }
    }
}
