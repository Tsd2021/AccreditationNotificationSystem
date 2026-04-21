using ANS.Model;
using ANS.Model.Interfaces;

namespace ANS.Model.GeneradorArchivoPorBanco
{
    public class HeritageFileGenerator : IBancoModoAcreditacion
    {
        public Task<GeneracionArchivoBancoResult> GenerarArchivo(List<CuentaBuzon> cb)
        {
            Console.WriteLine("Heritage no hace nada");
            return Task.FromResult(GeneracionArchivoBancoResult.ExitoSinRestricciones());
        }
    }
}