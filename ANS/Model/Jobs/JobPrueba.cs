using ANS.Model.Services;
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
            string jobName = context.JobDetail.Key.Name;
            string jobGroup = context.JobDetail.Key.Group ?? "DEFAULT";
            DateTimeOffset scheduledTime = context.ScheduledFireTimeUtc ?? DateTimeOffset.UtcNow;

            ServicioLog.instancia.WriteInfo(
                $"Iniciando ejecución del job | ScheduledTime: {scheduledTime:yyyy-MM-dd HH:mm:ss} UTC",
                $"Job: {jobName} | Group: {jobGroup} | Clase: {GetType().Name}");

            try
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

                ServicioLog.instancia.WriteInfo(
                    $"Job completado exitosamente | Duración: {(DateTimeOffset.UtcNow - scheduledTime).TotalSeconds:F2} segundos",
                    $"Job: {jobName} | Group: {jobGroup} | Clase: {GetType().Name}");
            }
            catch (Exception ex)
            {
                ServicioLog.instancia.WriteLog(ex, "JobPrueba", "Ejecución JobPrueba");
                throw;
            }
        }
    }
}
