// ANS.Scheduling/QuartzTriggerBuilderExtensions.cs
using System;
using Quartz;

namespace ANS.Scheduling
{
    public static class QuartzTriggerBuilderExtensions
    {
        public static TriggerBuilder WithCronIn(this TriggerBuilder builder, string cron,
            TimeZoneInfo? tz = null, Action<CronScheduleBuilder>? more = null)
        {
            var csb = CronScheduleBuilder.CronSchedule(cron)
                                         .InTimeZone(tz ?? QuartzTime.DefaultTz);
            more?.Invoke(csb); // opcional: misfire, etc.
            return builder.WithSchedule(csb);
        }

        public static TriggerBuilder WithDailyIntervalIn(this TriggerBuilder builder,
            Action<DailyTimeIntervalScheduleBuilder> setup,
            TimeZoneInfo? tz = null)
        {
            var dsb = DailyTimeIntervalScheduleBuilder.Create();
            setup(dsb);
            dsb.InTimeZone(tz ?? QuartzTime.DefaultTz);
            return builder.WithSchedule(dsb);
        }
    }
}
