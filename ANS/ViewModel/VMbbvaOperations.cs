
using ANS.Model;
using ANS.Model.GeneradorArchivoPorBanco;
using ANS.Model.Interfaces;
using ANS.Model.Services;
using ANS.Views;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Diagnostics;
using System.Windows.Input;
using static System.Windows.Forms.MonthCalendar;

namespace ANS.ViewModel
{
    public class VMbbvaOperations : ViewModelBase
    {
        private Banco banco { get; set; }
        private bool _isLoading;
        private ServicioCuentaBuzon _servicioCuentaBuzon;
        public bool IsLoading
        {
            get => _isLoading;
            set => Set(ref _isLoading, value);
        }

        #region Commands
        public ICommand EjecutarPuntoAPuntoTXTCommand { get; }
        public ICommand EjecutarDiaADiaTXTCommand { get; }
        public ICommand EjecutarDiaADiaNikeTXTCommand { get; }
        public ICommand EjecutarDiaADiaMansTXTCommand { get; }
        public ICommand EjecutarDiaADiaRobleFuerteTXTCommand { get; }
        public ICommand EjecutarDiaADiaRutaDoceTXTCommand { get; }
        public ICommand EjecutarDiaADiaArocenaTXTCommand { get; }
        public ICommand EjecutarExcelTataMvdCommand { get; }
        public ICommand EjecutarExcelTataMldCommand { get; }
        public ICommand EjecutarExcelDiaADiaMvdCommand { get; }
        public ICommand EjecutarExcelDiaADiaMldCommand { get; }
        public ICommand EjecutarExcelDedicadosCommand { get; }
        public ICommand EjecutarAltaEmailDestinoCommand { get; }
        public ICommand EjecutarTxtTest { get; }
        #endregion

        public VMbbvaOperations(Banco b)
        {

            banco = b;

            _servicioCuentaBuzon = new ServicioCuentaBuzon();

            EjecutarPuntoAPuntoTXTCommand = new RelayCommand(async () => await ejecutarPuntoAPuntoTXT());

            EjecutarDiaADiaTXTCommand = new RelayCommand(async () => await ejecutarDiaADiaTXT());

            EjecutarDiaADiaNikeTXTCommand = new RelayCommand(async () => await ejecutarDiaADiaNikeTXT());

            EjecutarDiaADiaMansTXTCommand = new RelayCommand(async () => await ejecutarDiaADiaMansTXT());

            EjecutarDiaADiaRobleFuerteTXTCommand = new RelayCommand(async () => await ejecutarDiaADiaRobleFuerteTXT());

            EjecutarDiaADiaRutaDoceTXTCommand = new RelayCommand(async () => await ejecutarDiaADiaRutaDoceTXT());

            EjecutarDiaADiaArocenaTXTCommand = new RelayCommand(async () => await ejecutarDiaADiaArocenaTXT());

            EjecutarExcelTataMvdCommand = new RelayCommand(async () => await ejecutarExcelTata("MONTEVIDEO"));

            EjecutarExcelTataMldCommand = new RelayCommand(async () => await ejecutarExcelTata("MALDONADO"));

            EjecutarExcelDiaADiaMvdCommand = new RelayCommand(async () => await ejecutarExcelDiaADia("MONTEVIDEO"));

            EjecutarExcelDiaADiaMldCommand = new RelayCommand(async () => await ejecutarExcelDiaADia("MALDONADO"));

            EjecutarExcelDedicadosCommand = new RelayCommand(async () => await ejecutarExcelDedicados());

            EjecutarAltaEmailDestinoCommand = new RelayCommand(async () => await ejecutarAltaEmailDestino());

            EjecutarTxtTest = new RelayCommand(async () => await ejecutarTxtDePrueba());

        }

        private async Task ejecutarTxtDePrueba()
        {

            Banco bbva = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.bbva);

            BBVAFileGenerator bbvaGenerator = new BBVAFileGenerator();
            try
            {

            await bbvaGenerator.generarArchivoTest();

            }
            catch(Exception ex)
            {

                throw ex;

            }

        }

        private async Task ejecutarAltaEmailDestino()
        {

            try
            {
                Banco b = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.bbva);

                Cliente c = null;

                ConfiguracionAcreditacion t = new ConfiguracionAcreditacion(VariablesGlobales.diaxdia);

                var alta = new AltaEmailDestino(b, c, t);

                alta.ShowDialog();
            }
            catch(Exception ex)
            {
              throw ex;
            }

        }
        private async Task ejecutarPuntoAPuntoTXT()
        {

            IsLoading = true;

            try
            {
                await Task.Run(async () =>
                {

                    await _servicioCuentaBuzon.acreditarPuntoAPuntoPorBanco(banco);

                });


            }
            catch (Exception e)
            {
                Debug.WriteLine("Hubo un error: " + e.Message);
                ServicioLog.instancia.WriteLog(e, "BBVA", "[MANUAL] Ejecutar P2P TXT");
                throw;
            }
            finally
            {
                IsLoading = false;
            }

        }
        private async Task ejecutarDiaADiaTXT()
        {
            IsLoading = true;

            try
            {

                await Task.Run(async () =>
                {

                    await _servicioCuentaBuzon.acreditarDiaADiaPorBanco(banco);

                });
            }
            catch (Exception e)
            {
                Debug.WriteLine("Hubo un error: " + e.Message);
                ServicioLog.instancia.WriteLog(e, "BBVA", "[MANUAL] Ejecutar DXD TXT");

                throw;
            }
            finally
            {
                IsLoading = false;
            }

        }
        private async Task ejecutarDiaADiaNikeTXT()
        {
            IsLoading = true;

            try
            {
                Cliente nike = ServicioCliente.getInstancia().getById(998); // Nike

                await Task.Run(async () =>
                {
                    // TimeSpan.Zero replica el comportamiento del job BBVA Nike (usa cierre de BD).
                    await _servicioCuentaBuzon.acreditarDiaADiaPorCliente(nike, banco, TimeSpan.Zero);
                });
            }
            catch (Exception e)
            {
                Debug.WriteLine("Hubo un error: " + e.Message);
                ServicioLog.instancia.WriteLog(e, "BBVA", "[MANUAL] Ejecutar DXD TXT NIKE");
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Acreditación manual día a día por cliente dedicado de BBVA.
        /// Replica exactamente lo que hace el job Quartz correspondiente:
        /// getById(idCliente) + acreditarDiaADiaPorCliente(..., TimeSpan.Zero).
        ///
        /// TimeSpan.Zero NO significa "sin corte": hace que acreditarDiaADiaPorCliente
        /// use la hora de cierre de CADA buzón (cu.Cierre, ServicioCuentaBuzon.cs:1415-1418).
        /// Es la misma semántica que usan los 4 jobs dedicados de BBVA.
        /// </summary>
        private async Task ejecutarDiaADiaPorClienteDedicado(int idCliente, string nombreParaLog)
        {
            IsLoading = true;

            try
            {
                Cliente cli = ServicioCliente.getInstancia().getById(idCliente);

                if (cli == null)
                    throw new Exception($"No se encontró el cliente {idCliente} ({nombreParaLog}).");

                await Task.Run(async () =>
                {
                    await _servicioCuentaBuzon.acreditarDiaADiaPorCliente(cli, banco, TimeSpan.Zero);
                });
            }
            catch (Exception e)
            {
                Debug.WriteLine("Hubo un error: " + e.Message);
                ServicioLog.instancia.WriteLog(e, "BBVA", $"[MANUAL] Ejecutar DXD TXT {nombreParaLog}");
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Mismos IdCliente que los jobs: AcreditarDiaADiaBBVAMans.cs:47,
        // AcreditarDiaADiaBBVARobleFuerte.cs:47 y AcreditarDiaADiaBBVARutaDoce.cs:47.
        private Task ejecutarDiaADiaMansTXT() => ejecutarDiaADiaPorClienteDedicado(1016, "MANS SRL");

        private Task ejecutarDiaADiaRobleFuerteTXT() => ejecutarDiaADiaPorClienteDedicado(976, "ROBLEFUERTE");

        private Task ejecutarDiaADiaRutaDoceTXT() => ejecutarDiaADiaPorClienteDedicado(977, "RUTADOCE");

        private Task ejecutarDiaADiaArocenaTXT() => ejecutarDiaADiaPorClienteDedicado(732, "AROCENA");

        /// <summary>
        /// Excel consolidado de los clientes con job día a día dedicado de BBVA.
        /// Mismos parámetros que el job de las 14:25 (tarea 'ConsolidadoDedicados').
        /// </summary>
        private async Task ejecutarExcelDedicados()
        {
            IsLoading = true;

            try
            {
                // Task.Run es obligatorio, no cosmético: enviarExcelConsolidadoDedicadosBBVA
                // corre sincrónicamente y adentro ServicioEmail.enviarExcelPorMail hace
                // enviarExcelPorMailAsync(...).GetAwaiter().GetResult().
                //
                // Ese GetResult() bloquea el hilo que lo llama mientras el método async de
                // adentro intenta volver a ese mismo hilo para continuar tras sus await. Si
                // el hilo es el de UI (el del click), se esperan mutuamente y la app queda
                // colgada sin excepción. Task.Run saca la ejecución del SynchronizationContext
                // de WPF y el deadlock desaparece.
                //
                // Es el mismo motivo por el que ejecutarExcelDiaADia y los DXD usan Task.Run.
                await Task.Run(async () =>
                {
                    await _servicioCuentaBuzon.enviarExcelConsolidadoDedicadosBBVA("ConsolidadoDedicados", null, "MONTEVIDEO");
                });
            }
            catch (Exception e)
            {
                Debug.WriteLine("Hubo un error: " + e.Message);
                ServicioLog.instancia.WriteLog(e, "BBVA", "[MANUAL] Excel consolidado dedicados");
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ejecutarExcelTata(string soloCiudad)
        {
            IsLoading = true;

            try
            {
                Banco bbva = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.bbva);

                TimeSpan desde = new TimeSpan(6, 30, 0);

                // Maldonado cierra ventana a las 20:00 (igual al job); el resto mantiene 20:30.
                TimeSpan hasta = string.Equals(soloCiudad, "MALDONADO", StringComparison.OrdinalIgnoreCase)
                    ? new TimeSpan(20, 0, 0)
                    : new TimeSpan(20, 30, 0);

                string tarea = "ExcelTata";

                // ID TATA : 242

                Cliente tata = ServicioCliente.getInstancia().getById(242);

                int numTanda = 1;

                await _servicioCuentaBuzon.enviarExcelFormatoTanda(desde, hasta, tata, bbva, "MONTEVIDEO", numTanda, tarea, soloCiudad: soloCiudad);


            }
            catch (Exception e)
            {
                Debug.WriteLine("Hubo un error: " + e.Message);
                ServicioLog.instancia.WriteLog(e, "BBVA", $"[MANUAL] ENVIAR EXCEL TATA ({soloCiudad})");
                throw;
            }
            finally
            {
                IsLoading = false;
            }

        }
        private async Task ejecutarExcelDiaADia(string soloCiudad)
        {
            IsLoading = true;

            ConfiguracionAcreditacion config = new ConfiguracionAcreditacion(VariablesGlobales.diaxdia);

            string tarea = "ReporteDiario";

            try
            {

                await Task.Run(async () =>
                {

                    await _servicioCuentaBuzon.enviarExcelDiaADiaPorBanco(banco, config, tarea, soloCiudad: soloCiudad);

                });
            }

            catch (Exception e)
            {
                Debug.WriteLine("Hubo un error: " + e.Message);
                ServicioLog.instancia.WriteLog(e, "BBVA", $"[MANUAL] ENVIAR EXCEL reporte diario ({soloCiudad})");
                throw;
            }

            finally
            {
                IsLoading = false;
            }

        }
    }
}
