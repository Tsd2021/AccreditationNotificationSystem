
namespace ANS.Model.Interfaces
{
    public interface IServicioCuentaBuzon
    {
        List<CuentaBuzon> getAll();
        List<CuentaBuzon> getAllByTipoAcreditacion(string tipoAcreditacion);
        List<CuentaBuzon> getAllByTipoAcreditacionYBanco(string tipoAcreditacion, Banco bank);
        Task<List<CuentaBuzon>> getCuentasPorClienteBancoYTipoAcreditacion(int idCliente,Banco bank,ConfiguracionAcreditacion configuracionAcreditacion);
        Task acreditarPuntoAPuntoPorBanco(Banco bank);
        Task acreditarTandaHendersonSantander(TimeSpan horaCierreActual,int numTanda);
        Task acreditarTandaHendersonScotiabank(TimeSpan horaCierreActual, int numTanda);
        Task acreditarDiaADiaPorBanco(Banco bank);
        Task acreditarTandaPorBanco(Banco bank);
        Task acretidarPorBanco(Banco bank,TimeSpan horaCierre);
        Task acreditarDiaADiaPorCliente(Cliente cli,Banco bank,TimeSpan horaCierreActual);
        Task enviarExcel(TimeSpan desde,TimeSpan hasta,Cliente cliente, Banco bank);
        Task enviarExcelFormatoTanda(TimeSpan desde,TimeSpan hasta, Cliente cliente, Banco banco,string city,int numTanda,string tarea);
        Task enviarExcelDiaADiaPorBanco(Banco banco, ConfiguracionAcreditacion tipoAcreditacion,string tarea);
        Task enviarExcelTesoreria(Banco santander, string city, int numTanda, TimeSpan desde,TimeSpan hasta,string tarea);
        Task checkUltimaConexionByIdBuzon(string nc);
        Task generarExcelDelResumenDelDiaSantander(string tarea);
    }
}
