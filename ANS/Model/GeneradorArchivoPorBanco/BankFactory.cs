using ANS.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Model.GeneradorArchivoPorBanco
{
    public static class BankFactory
    {
        public static IBancoModoAcreditacion GetModoAcreditacionByBanco(string banco,string tipoAcreditacion)
        {

            return banco.ToLower() switch
            {

                VariablesGlobales.scotiabank => new ScotiaFileGenerator(),

                VariablesGlobales.santander => new SantanderFileGenerator(new ConfiguracionAcreditacion(tipoAcreditacion)),

                VariablesGlobales.bbva => new BBVAFileGenerator(new ConfiguracionAcreditacion(tipoAcreditacion)),

                VariablesGlobales.brou => new BROUFileGenerator(),

                VariablesGlobales.heritage => new HeritageFileGenerator(),

                VariablesGlobales.hsbc => new HSBCFileGenerator(),

                VariablesGlobales.itau => new ItauFileGenerator(),

                VariablesGlobales.bandes => new BandesFileGenerator(),

                _ => throw new Exception($"No se encontró un generador para el banco: {banco}")

            };

        }
    }
}
