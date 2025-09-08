using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Threading.Tasks;

namespace ANS.Scheduling
{
    public static class MissedRunDetector
    {
        public static async Task DetectMissedFirings(
            IScheduler scheduler,
            IJobHistoryStore store,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc)
        {
            var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());

            foreach (var jk in jobKeys)
            {
                var triggers = await scheduler.GetTriggersOfJob(jk);

                foreach (var trig in triggers)
                {
                    if (trig is IDailyTimeIntervalTrigger dt)
                    {
                        var tz = dt.TimeZone ?? QuartzTime.DefaultTz;
                        var startDate = TimeZoneInfo.ConvertTime(fromUtc, tz).Date;
                        var endDate = TimeZoneInfo.ConvertTime(toUtc, tz).Date;

                        for (var d = startDate; d <= endDate; d = d.AddDays(1))
                        {
                            if (!dt.DaysOfWeek.Contains(d.DayOfWeek)) continue;

                            var firstUtc = TriggerUtil.GetWindowStartUtc(dt, d);
                            if (firstUtc > fromUtc && firstUtc <= toUtc)
                            {
                                if (!await store.ExistsAsync(jk, firstUtc))
                                {
                                    await store.MarkSkippedAsync(jk, trig.Key, firstUtc,
                                        "Inicio de ventana omitido (app cerrada). Trigger sigue activo.");
                                }
                            }
                        }
                    }
                    else if (trig is ICronTrigger ct && TriggerUtil.IsOncePerDay(ct))
                    {
                        var cur = trig.GetFireTimeAfter(fromUtc);
                        while (cur.HasValue && cur.Value <= toUtc)
                        {
                            var sch = cur.Value;
                            if (!await store.ExistsAsync(jk, sch))
                            {
                                await store.MarkSkippedAsync(jk, trig.Key, sch, "No ejecutada por app cerrada");
                            }
                            cur = trig.GetFireTimeAfter(sch);
                        }
                    }
                    else
                    {
                        // Cron frecuentes: evitamos “spam”. Si quisieras, marcá solo el primer del día.
                    }
                }
            }
        }
    }
}
