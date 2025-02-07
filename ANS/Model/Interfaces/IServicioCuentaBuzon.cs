
namespace ANS.Model.Interfaces
{
    public interface IServicioCuentaBuzon
    {
        List<CuentaBuzon> getAll();
        List<CuentaBuzon> getAllByTipoAcreditacion(string tipoAcreditacion);
        List<CuentaBuzon> getAllByTipoAcreditacionYBanco(string tipoAcreditacion, Banco bank);
        Task acreditarPuntoAPuntoPorBanco(Banco bank);
        Task acreditarTanda1HendersonSantander(TimeSpan horaCierreActual);
        Task acreditarTanda2HendersonSantander(TimeSpan horaCierreActual);
        Task acreditarDiaADiaPorBanco(Banco bank);
        Task acreditarTandaPorBanco(Banco bank);
        Task acretidarPorBanco(Banco bank,TimeSpan horaCierre);
        Task acreditarDiaADiaPorCliente(Cliente cli,Banco bank,TimeSpan horaCierreActual);
        Task enviarExcel(TimeSpan desde,TimeSpan hasta,Cliente cliente, Banco bank);
        Task enviarExcelHenderson(TimeSpan desde, TimeSpan hasta, Cliente henderson, Banco santander);
    }
}
