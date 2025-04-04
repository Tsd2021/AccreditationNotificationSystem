﻿
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
        Task acreditarDiaADiaPorBanco(Banco bank);
        Task acreditarTandaPorBanco(Banco bank);
        Task acretidarPorBanco(Banco bank,TimeSpan horaCierre);
        Task acreditarDiaADiaPorCliente(Cliente cli,Banco bank,TimeSpan horaCierreActual);
        Task enviarExcel(TimeSpan desde,TimeSpan hasta,Cliente cliente, Banco bank);
        Task enviarExcelHenderson(TimeSpan desde,TimeSpan hasta, Cliente henderson, Banco santander,string city,int numTanda);
        Task enviarExcelSantanderDiaADia(Banco banco, ConfiguracionAcreditacion tipoAcreditacion);
        Task enviarExcelTesoreria(Banco santander, string city, int numTanda, TimeSpan hasta,TimeSpan desde);
        Task checkUltimaConexionByIdBuzon(string nc);
        Task generarExcelDelResumenDelDiaSantander();
    }
}
