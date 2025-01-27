using ANS.Model.Interfaces;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Model.Jobs.BBVA
{

    [DisallowConcurrentExecution]
    public class AcreditarPuntoAPuntoBBVAJob : IJob
    {
        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;
        public AcreditarPuntoAPuntoBBVAJob(IServicioCuentaBuzon servicioCuentaBuzon)
        {
            _servicioCuentaBuzon = servicioCuentaBuzon;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _servicioCuentaBuzon.acreditarPuntoAPuntoPorBanco(VariablesGlobales.bbva);

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
