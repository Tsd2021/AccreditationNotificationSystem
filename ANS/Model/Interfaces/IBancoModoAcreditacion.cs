
namespace ANS.Model.Interfaces
{
    public interface IBancoModoAcreditacion
    {
        Task GenerarArchivo(List<CuentaBuzon> cb);
      
    }
}
