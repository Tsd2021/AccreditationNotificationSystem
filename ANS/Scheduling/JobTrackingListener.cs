using ANS.Scheduling;
using Quartz;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public sealed class JobTrackingListener : IJobListener, ITriggerListener
{
    private readonly IJobHistoryStore _repo;
    public JobTrackingListener(IJobHistoryStore repo) => _repo = repo;

    public string Name => "JobTrackingListener";

    // >>> Evento para notificar cambios a la UI (asíncrono)
    public event Func<Task>? Changed;

    private static async Task SafeInvoke(Func<Task> h)
    {
        try { await h().ConfigureAwait(false); } catch { /* log opcional */ }
    }

    private Task NotifyChangedAsync()
    {
        var handler = Changed;
        if (handler is null) return Task.CompletedTask;

        // Ejecuta todos los suscriptores de forma segura
        var list = handler.GetInvocationList().Cast<Func<Task>>();
        return Task.WhenAll(list.Select(SafeInvoke));
    }

    // ===== Trigger =====
    public async Task TriggerFired(ITrigger trigger, IJobExecutionContext context, CancellationToken ct = default)
    {
        try
        {
            var scheduled = context.ScheduledFireTimeUtc ?? context.FireTimeUtc;
            await _repo.AddPlannedAsync(context.JobDetail.Key, trigger.Key, scheduled, JobRunStatus.Pending);
        }
        finally
        {
            await NotifyChangedAsync();
        }
    }

    public Task<bool> VetoJobExecution(ITrigger trigger, IJobExecutionContext context, CancellationToken ct = default)
        => Task.FromResult(false);

    public async Task TriggerMisfired(ITrigger trigger, CancellationToken ct = default)
    {
        try
        {
            var when = trigger.GetPreviousFireTimeUtc()
                    ?? trigger.GetNextFireTimeUtc()
                    ?? DateTimeOffset.UtcNow;
            await _repo.MarkMisfireAsync(trigger.JobKey, trigger.Key, when, "Trigger misfire");
        }
        finally
        {
            await NotifyChangedAsync();
        }
    }

    public Task TriggerComplete(ITrigger trigger, IJobExecutionContext context,
                                SchedulerInstruction instructionCode, CancellationToken ct = default)
        => Task.CompletedTask;

    // ===== Job =====
    public async Task JobToBeExecuted(IJobExecutionContext context, CancellationToken ct = default)
    {
        try
        {
            await _repo.MarkStartAsync(context.JobDetail.Key,
                context.ScheduledFireTimeUtc ?? context.FireTimeUtc,
                context.FireTimeUtc);
        }
        finally
        {
            await NotifyChangedAsync();
        }
    }

    public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken ct = default)
        => Task.CompletedTask;

    public async Task JobWasExecuted(IJobExecutionContext context, JobExecutionException? ex, CancellationToken ct = default)
    {
        try
        {
            await _repo.MarkEndAsync(context.JobDetail.Key,
                context.ScheduledFireTimeUtc ?? context.FireTimeUtc,
                ok: ex is null,
                message: ex?.Message);
        }
        finally
        {
            await NotifyChangedAsync();
        }
    }
}
