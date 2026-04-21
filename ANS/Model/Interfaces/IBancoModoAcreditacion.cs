using ANS.Model;

namespace ANS.Model.Interfaces
{
    public interface IBancoModoAcreditacion
    {
        Task<GeneracionArchivoBancoResult> GenerarArchivo(List<CuentaBuzon> cb);
    }
}
