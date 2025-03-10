
namespace ANS.Model.Interfaces
{
    public interface IServicioCuentaBuzon
    {
        List<CuentaBuzon> getAll();
        List<CuentaBuzon> getAllByTipoAcreditacion(string tipoAcreditacion);
        List<CuentaBuzon> getAllByTipoAcreditacionYBanco(string tipoAcreditacion, Banco bank);
        Task acreditarPuntoAPuntoPorBanco(Banco bank);
        Task acreditarTandaHendersonSantander(TimeSpan horaCierreActual);
        Task acreditarDiaADiaPorBanco(Banco bank);
        Task acreditarTandaPorBanco(Banco bank);
        Task acretidarPorBanco(Banco bank,TimeSpan horaCierre);
        Task acreditarDiaADiaPorCliente(Cliente cli,Banco bank,TimeSpan horaCierreActual);
        Task enviarExcel(TimeSpan desde,TimeSpan hasta,Cliente cliente, Banco bank);
        Task enviarExcelHenderson(TimeSpan hasta, Cliente henderson, Banco santander,string city,int numTanda);
        Task checkUltimaConexionByIdBuzon(string nc);
    }
}
