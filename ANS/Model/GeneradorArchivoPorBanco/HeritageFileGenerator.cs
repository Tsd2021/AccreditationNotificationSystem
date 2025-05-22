using ANS.Model.Interfaces;

namespace ANS.Model.GeneradorArchivoPorBanco
{
    public class HeritageFileGenerator : IBancoModoAcreditacion
    {
        public async Task GenerarArchivo(List<CuentaBuzon> cb)
        {
            Console.WriteLine("Heritage no hace nada");
        }
    }
}