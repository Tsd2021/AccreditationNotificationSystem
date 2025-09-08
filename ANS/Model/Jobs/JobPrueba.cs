using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Model.Jobs
{
    /// <summary>
    /// Job simple para pruebas:
    /// - steps: cantidad de pasos simulados
    /// - delayMs: milisegundos por paso
    /// - shouldFail: true => tira excepción al final
    /// </summary>
    public class JobPrueba : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var map = context.MergedJobDataMap;

            int steps = map.ContainsKey("steps") ? map.GetInt("steps") : 10;
            int delayMs = map.ContainsKey("delayMs") ? map.GetInt("delayMs") : 200;
            bool shouldFail = map.ContainsKey("shouldFail") && map.GetBoolean("shouldFail");

            // Simula trabajo (esto mantiene la ProgressBar "indeterminada" gracias al polling del VM)
            for (int i = 0; i < steps; i++)
            {
                await Task.Delay(delayMs, context.CancellationToken);
                // Si más adelante querés progreso determinístico:
                // ProgressHub.Report(context.JobDetail.Key, (i + 1) / (double)steps);
            }

            if (shouldFail)
                throw new Exception("Fallo intencional de JobPrueba.");
        }
    }
}
