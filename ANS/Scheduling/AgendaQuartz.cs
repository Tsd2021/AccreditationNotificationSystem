using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANS.Scheduling
{
    public class AgendaQuartz
    {
        public sealed class ScheduledItem
        {
            public string Job { get; set; } = "";
            public string Group { get; set; } = "";
            public string Trigger { get; set; } = "";
            public string TriggerType { get; set; } = "";
            public string TimeZoneId { get; set; } = TimeZoneInfo.Local.Id;

            public DateTimeOffset? NextFireUtc { get; set; }
            public DateTimeOffset? PrevFireUtc { get; set; }
            public DateTimeOffset? NextFireLocal { get; set; }

            public string EstadoUi { get; set; } = "—";

            public DateTimeOffset? LastCompletedUtc { get; set; }
            public DateTimeOffset? LastCompletedLocal { get; set; }
            public string LastStatus { get; set; } = "—";
        }

        public static class SchedulerSnapshotProvider
        {
            public static async Task<IReadOnlyList<ScheduledItem>> GetAsync(
                IScheduler scheduler, IJobHistoryStore store)
            {
                var list = new List<ScheduledItem>();
                var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
                var recent = await store.GetRecentAsync(500);

                foreach (var jk in jobKeys)
                {
                    var triggers = await scheduler.GetTriggersOfJob(jk);
                    foreach (var t in triggers)
                    {
                        TimeZoneInfo tz = (t as ICronTrigger)?.TimeZone
                                          ?? (t as IDailyTimeIntervalTrigger)?.TimeZone
                                          ?? QuartzTime.DefaultTz;

                        var nextUtc = t.GetNextFireTimeUtc();
                        var prevUtc = t.GetPreviousFireTimeUtc();

                        var item = new ScheduledItem
                        {
                            Job = jk.Name,
                            Group = jk.Group ?? "",
                            Trigger = t.Key.Name,
                            TriggerType = t switch
                            {
                                ICronTrigger => "Cron",
                                IDailyTimeIntervalTrigger => "DailyInterval",
                                _ => t.GetType().Name
                            },
                            TimeZoneId = tz.Id,
                            NextFireUtc = nextUtc,
                            PrevFireUtc = prevUtc,
                            NextFireLocal = nextUtc.HasValue ? TimeZoneInfo.ConvertTime(nextUtc.Value, tz) : null
                        };

                        // último run (si hay)
                        var last = await store.GetLastForJobAsync(jk, excludeSkipped: true);

                        if (last != null)
                        {
                            item.LastStatus = last.Status switch
                            {
                                JobRunStatus.Succeeded => "OK",
                                JobRunStatus.Failed => "ERROR",
                                JobRunStatus.Misfired => "MISFIRE",
                                JobRunStatus.SkippedByShutdown => "OMITIDA",
                                JobRunStatus.Pending => "EN CURSO",
                                _ => "—"
                            };
                            item.LastCompletedUtc = last.CompletedTimeUtc ?? last.ActualFireTimeUtc;
                            item.LastCompletedLocal = item.LastCompletedUtc.HasValue
                                ? TimeZoneInfo.ConvertTime(item.LastCompletedUtc.Value, tz)
                                : null;
                        }

                        // ===== Reglas de estado UI =====
                        if (t is IDailyTimeIntervalTrigger dt)
                        {
                            var nowTz = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
                            var start = new TimeSpan(dt.StartTimeOfDay.Hour, dt.StartTimeOfDay.Minute, dt.StartTimeOfDay.Second);
                            var end = new TimeSpan(dt.EndTimeOfDay.Hour, dt.EndTimeOfDay.Minute, dt.EndTimeOfDay.Second);

                            bool hoyActivo = dt.DaysOfWeek.Contains(nowTz.DayOfWeek) &&
                                             nowTz.TimeOfDay >= start && nowTz.TimeOfDay <= end;

                            var todayStartUtc = TriggerUtil.GetWindowStartUtc(dt, nowTz.Date);
                            bool inicioOmitido =
                                todayStartUtc <= DateTimeOffset.UtcNow &&
                                !recent.Any(r => r.JobName == jk.Name &&
                                                 (r.JobGroup ?? "") == (jk.Group ?? "") &&
                                                 r.ScheduledFireTimeUtc == todayStartUtc);

                            if (hoyActivo && inicioOmitido)
                                item.EstadoUi = "Inicio omitido (ventana activa)";
                            else if (hoyActivo)
                                item.EstadoUi = "Ventana activa";
                            else if (item.NextFireLocal.HasValue)
                                item.EstadoUi = "Pendiente";
                            else
                                item.EstadoUi = "—";
                        }
                        else if (t is ICronTrigger ct && TriggerUtil.IsOncePerDay(ct))
                        {
                            var todayUtc = TriggerUtil.TodayScheduledUtc(ct);
                            if (todayUtc.HasValue)
                            {
                                var rec = recent.FirstOrDefault(r => r.JobName == jk.Name &&
                                                                     (r.JobGroup ?? "") == (jk.Group ?? "") &&
                                                                     r.ScheduledFireTimeUtc == todayUtc.Value);
                                if (rec != null)
                                {
                                    item.EstadoUi = rec.Status == JobRunStatus.Succeeded ? "OK" :
                                                    rec.Status == JobRunStatus.SkippedByShutdown ? "No ejecutada por app cerrada" :
                                                    rec.Status == JobRunStatus.Failed ? "ERROR" : "—";
                                }
                                else
                                {
                                    item.EstadoUi = todayUtc.Value > DateTimeOffset.UtcNow
                                        ? "Pendiente"
                                        : "No ejecutada por app cerrada";
                                }
                            }
                            else
                            {
                                item.EstadoUi = "—";
                            }
                        }
                        else
                        {
                            item.EstadoUi = item.NextFireLocal.HasValue ? "Pendiente" : "—";
                        }

                        list.Add(item);
                    }
                }

                return list.OrderBy(x => x.NextFireLocal ?? DateTimeOffset.MaxValue).ToList();
            }
        }
    }
}
