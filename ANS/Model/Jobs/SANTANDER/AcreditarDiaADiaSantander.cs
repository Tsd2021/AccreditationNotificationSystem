using ANS.Model.Interfaces;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Model.Jobs.SANTANDER
{
    public class AcreditarDiaADiaSantander : IJob
    {
        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;
        public AcreditarDiaADiaSantander(IServicioCuentaBuzon servicioCuentaBuzon)
        {
            _servicioCuentaBuzon = servicioCuentaBuzon;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _servicioCuentaBuzon.acreditarDiaADiaPorBanco(VariablesGlobales.santander);

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
