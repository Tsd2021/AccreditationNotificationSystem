using ANS.Model.Interfaces;
using ANS.Model.Services;
using Quartz;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ANS.Scheduling
{
    /// <summary>
    /// Listener que veta la ejecución de jobs BBVA cuando la fecha del fire es un feriado activo.
    /// Identificación BBVA: JobKey.Group == ServicioFeriadosTAAS.GrupoBBVA.
    /// </summary>
    public sealed class FeriadoBBVAGateListener : ITriggerListener
    {
        public const string ListenerName = "FeriadoBBVAGateListener";
        private readonly IFeriadosProvider _feriadosProvider;

        public FeriadoBBVAGateListener(IFeriadosProvider feriadosProvider)
        {
            _feriadosProvider = feriadosProvider ?? throw new ArgumentNullException(nameof(feriadosProvider));
        }

        public string Name => ListenerName;

        public Task TriggerFired(ITrigger trigger, IJobExecutionContext context, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<bool> VetoJobExecution(ITrigger trigger, IJobExecutionContext context, CancellationToken ct = default)
        {
            var jobKey = context.JobDetail.Key;
            if (jobKey.Group != Model.Services.ServicioFeriadosTAAS.GrupoBBVA)
                return Task.FromResult(false);

            var fireTimeUtc = context.ScheduledFireTimeUtc ?? context.FireTimeUtc;
            var fechaLocal = fireTimeUtc.ToLocalTime().DateTime;
            if (!_feriadosProvider.IsFeriadoActivo(fechaLocal))
                return Task.FromResult(false);

            try
            {
                ServicioLog.instancia.WriteInfo(
                    $"[FeriadoBBVA] Job vetado por feriado activo. JobKey={jobKey.Name}/{jobKey.Group}, Fecha={fechaLocal:yyyy-MM-dd}, TriggerKey={trigger.Key.Name}");
            }
            catch
            {
                // no fallar por log
            }

            return Task.FromResult(true);
        }

        public Task TriggerMisfired(ITrigger trigger, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task TriggerComplete(ITrigger trigger, IJobExecutionContext context,
            SchedulerInstruction instructionCode, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
