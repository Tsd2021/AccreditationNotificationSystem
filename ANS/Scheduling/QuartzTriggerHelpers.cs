// ANS.Scheduling/QuartzTriggerHelpers.cs
using System;
using Quartz;

namespace ANS.Scheduling
{
    public static class QuartzTriggerHelpers
    {
        public static TimeZoneInfo GetTriggerTimeZone(ITrigger t)
        {
            if (t is ICronTrigger ct && ct.TimeZone != null) return ct.TimeZone;
            if (t is IDailyTimeIntervalTrigger dt && dt.TimeZone != null) return dt.TimeZone;
            return QuartzTime.DefaultTz;
        }

        public static DateTimeOffset? ToTriggerLocal(this DateTimeOffset? utc, ITrigger t)
        {
            if (!utc.HasValue) return null;
            var tz = GetTriggerTimeZone(t);
            return TimeZoneInfo.ConvertTime(utc.Value, tz);
        }
    }
}
