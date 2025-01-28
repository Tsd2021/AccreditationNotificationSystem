using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ANS.Model.Interfaces;
using ANS.Model.Services;

namespace ANS.Model.Jobs.BBVA
{
    [DisallowConcurrentExecution]
    public class AcreditarDiaADiaBBVAJob : IJob
    {

        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;
        public AcreditarDiaADiaBBVAJob(IServicioCuentaBuzon servicioCuentaBuzon)
        {
            _servicioCuentaBuzon = servicioCuentaBuzon;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                await _servicioCuentaBuzon.acreditarDiaADiaPorBanco(VariablesGlobales.bbva);

                Console.WriteLine("Tarea de BBVA ejecutada con éxito.");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al ejecutar la tarea de BBVA: {ex.Message}");
            }

            await Task.CompletedTask;
        }
    }
}
