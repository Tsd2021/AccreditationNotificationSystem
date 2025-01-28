using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ANS.Model.Interfaces;
using Quartz;

namespace ANS.Model.Jobs.SANTANDER
{
    [DisallowConcurrentExecution]
    public class AcreditarPuntoAPuntoSantander : IJob
    {
        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;

        public AcreditarPuntoAPuntoSantander(IServicioCuentaBuzon servicioCuentaBuzon)
        {
            _servicioCuentaBuzon = servicioCuentaBuzon;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                await _servicioCuentaBuzon.acreditarPuntoAPuntoPorBanco(VariablesGlobales.santander);

                Console.WriteLine("Tarea de SANTANDER ejecutada con éxito.");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al ejecutar la tarea de SANTANDER: {ex.Message}");
            }

            await Task.CompletedTask;
        }
    }
}
