using Quartz;
using Quartz.Impl.Matchers;

namespace ANS.Scheduling
{
    public static class MissedExecutionDetector
    {
        public static async Task DetectAsync(IScheduler scheduler, IJobHistoryStore store, DateTimeOffset? lastShutdownUtc, DateTimeOffset nowUtc)
        {
            if (lastShutdownUtc is null) return;

            var triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
            foreach (var tk in triggerKeys)
            {
                var trig = await scheduler.GetTrigger(tk);
                if (trig is null || trig.GetNextFireTimeUtc() is null) continue;

                // Solo soportamos cron y daily interval (lo que usas)
                if (trig is ICronTrigger cron)
                {
                    await DetectCronMisses(cron, store, lastShutdownUtc.Value, nowUtc);
                }
                else if (trig is IDailyTimeIntervalTrigger daily)
                {
                    await DetectDailyMisses(daily, store, lastShutdownUtc.Value, nowUtc);
                }
            }
        }

        private static async Task DetectCronMisses(ICronTrigger cron, IJobHistoryStore store, DateTimeOffset fromUtc, DateTimeOffset toUtc)
        {
            var expr = new CronExpression(cron.CronExpressionString);
            var cursor = expr.GetNextValidTimeAfter(fromUtc);
            while (cursor is not null && cursor.Value <= toUtc)
            {
                var sch = cursor.Value;
                if (!await store.ExistsAsync(cron.JobKey, sch))
                {
                    await store.MarkSkippedAsync(cron.JobKey, cron.Key, sch, "No ejecutada por cierre de aplicación");
                }
                cursor = expr.GetNextValidTimeAfter(sch);
            }
        }


        private static async Task DetectDailyMisses(
            IDailyTimeIntervalTrigger daily,
            IJobHistoryStore store,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc)
        {
            // Zona horaria del trigger (o local)
            var tz = daily.TimeZone ?? TimeZoneInfo.Local;

            // Convertimos los límites a la TZ del trigger y nos quedamos con la fecha (día calendario)
            var startDate = TimeZoneInfo.ConvertTime(fromUtc, tz).Date;
            var endDate = TimeZoneInfo.ConvertTime(toUtc, tz).Date;

            // Armamos el paso (TimeSpan) según la unidad del trigger
            TimeSpan step = daily.RepeatIntervalUnit switch
            {
                IntervalUnit.Second => TimeSpan.FromSeconds(daily.RepeatInterval),
                IntervalUnit.Minute => TimeSpan.FromMinutes(daily.RepeatInterval),
                IntervalUnit.Hour => TimeSpan.FromHours(daily.RepeatInterval),
                _ => throw new NotSupportedException(
                        $"Unidad no soportada en DailyTimeIntervalTrigger: {daily.RepeatIntervalUnit}")
            };

            for (var d = startDate; d <= endDate; d = d.AddDays(1))
            {
                // Solo días activos
                var dow = d.DayOfWeek;
                if (!daily.DaysOfWeek.Contains(dow)) continue;

                // Rango diario (en hora local de la TZ del trigger)
                var startLocal = d.Add(new TimeSpan(daily.StartTimeOfDay.Hour, daily.StartTimeOfDay.Minute, daily.StartTimeOfDay.Second));
                var endLocal = d.Add(new TimeSpan(daily.EndTimeOfDay.Hour, daily.EndTimeOfDay.Minute, daily.EndTimeOfDay.Second));

                if (endLocal < startLocal) continue; // protección ante rangos mal configurados

                // Iteramos cada “disparo” esperado del día
                for (var t = startLocal; t <= endLocal; t = t.Add(step))
                {
                    // Creamos un DateTimeOffset con el offset correcto de esa TZ en ese instante
                    var schLocal = new DateTimeOffset(t, tz.GetUtcOffset(t));

                    // ¿Quedó dentro del período [fromUtc, toUtc]? (comparación en UTC es segura con DateTimeOffset)
                    if (schLocal <= toUtc && schLocal > fromUtc)
                    {
                        if (!await store.ExistsAsync(daily.JobKey, schLocal))
                        {
                            await store.MarkSkippedAsync(daily.JobKey, daily.Key, schLocal,
                                "No ejecutada por cierre de aplicación");
                        }
                    }
                }
            }
        }

    }
}
