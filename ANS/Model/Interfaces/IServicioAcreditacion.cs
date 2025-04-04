

namespace ANS.Model.Interfaces
{
    public interface IServicioAcreditacion
    {
        void insertar(Acreditacion a);
        Task<List<DtoAcreditacionesPorEmpresa>> getAcreditacionesByFechaYBanco(DateTime desde, DateTime hasta, Banco bank);
    }
}
