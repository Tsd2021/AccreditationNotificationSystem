using ANS.Scheduling;
using Quartz;
using Quartz.Impl.Matchers;
using Quartz.Impl.Triggers;
using Quartz.Spi;

public static class MissedRunDetector
{
    // Límite para no calcular infinitas ocurrencias si la app estuvo apagada días
    private const int MaxPerTrigger = 5000;

    public static async Task DetectMissedFirings(
    IScheduler scheduler,
    IJobHistoryStore store,
    DateTimeOffset lastShutdownUtc,
    DateTimeOffset nowUtc)
    {
        // Nos aseguramos de no marcar futuros
        if (nowUtc < DateTimeOffset.UtcNow) nowUtc = DateTimeOffset.UtcNow;

        var jobKeys = await scheduler.GetJobKeys(Quartz.Impl.Matchers.GroupMatcher<JobKey>.AnyGroup());

        foreach (var jobKey in jobKeys)
        {
            var triggers = await scheduler.GetTriggersOfJob(jobKey);

            foreach (var trig in triggers)
            {
                if (trig is not IOperableTrigger opt) continue;

                ICalendar? cal = null;
                if (!string.IsNullOrEmpty(trig.CalendarName))
                    cal = await scheduler.GetCalendar(trig.CalendarName);

                // Rango SIEMPRE en UTC
                var missed = ComputeFireTimesBetween(opt, cal, lastShutdownUtc, nowUtc);

                foreach (var ftUtc in missed)
                {
                    if (ftUtc >= DateTimeOffset.UtcNow) continue; // jamás marcar futuros
                    await store.MarkSkippedAsync(jobKey, trig.Key, ftUtc, "Omitido: aplicación apagada");
                }
            }
        }
    }




    public static IReadOnlyList<DateTimeOffset> ComputeFireTimesBetween(
    IOperableTrigger trigg, ICalendar? cal, DateTimeOffset from, DateTimeOffset to)
    {
        var lst = new List<DateTimeOffset>();

        // Clon independiente para no alterar el trigger real
        var t = (IOperableTrigger)trigg.Clone();

        // Fuerza el rango de búsqueda SIEMPRE
        t.StartTimeUtc = from;
        t.EndTimeUtc = to;

        // Recalcular el primer disparo a partir de 'from'
        if (!t.ComputeFirstFireTimeUtc(cal).HasValue)
            return lst.AsReadOnly(); // no hay disparos en el rango

        // Seguridad ante triggers muy frecuentes
        const int hardLimit = 10000;
        int count = 0;

        while (true)
        {
            var d = t.GetNextFireTimeUtc();
            if (!d.HasValue) break;

            // Si está antes del 'from', avanzar
            if (d.Value < from)
            {
                t.Triggered(cal);
                continue;
            }

            // Si se pasa del 'to', cortar
            if (d.Value > to) break;

            lst.Add(d.Value);
            t.Triggered(cal);

            if (++count >= hardLimit) break; // protección
        }

        return lst.AsReadOnly();
    }

}
