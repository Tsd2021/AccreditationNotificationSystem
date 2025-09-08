using Quartz;
using System;

namespace ANS.Scheduling
{
    public static class TriggerUtil
    {
        public static bool IsOncePerDay(ICronTrigger ct)
        {
            var tz = ct.TimeZone ?? QuartzTime.DefaultTz;
            var nowTz = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
            var startDayTz = new DateTimeOffset(nowTz.Date, tz.GetUtcOffset(nowTz.Date));
            var endDayTz = startDayTz.AddDays(1);

            int count = 0;
            var cur = ct.GetFireTimeAfter(startDayTz.AddSeconds(-1));
            while (cur.HasValue && cur.Value < endDayTz)
            {
                count++;
                if (count > 1) return false;
                cur = ct.GetFireTimeAfter(cur.Value);
            }
            return count == 1;
        }

        public static DateTimeOffset? TodayScheduledUtc(ICronTrigger ct)
        {
            var tz = ct.TimeZone ?? QuartzTime.DefaultTz;
            var nowTz = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
            var startDayTz = new DateTimeOffset(nowTz.Date, tz.GetUtcOffset(nowTz.Date));
            var endDayTz = startDayTz.AddDays(1);

            var first = ct.GetFireTimeAfter(startDayTz.AddSeconds(-1));
            return (first.HasValue && first.Value < endDayTz) ? first.Value : null;
        }

        public static DateTimeOffset GetWindowStartUtc(IDailyTimeIntervalTrigger dt, DateTime date)
        {
            var tz = dt.TimeZone ?? QuartzTime.DefaultTz;
            var startLocal = date.Add(new TimeSpan(dt.StartTimeOfDay.Hour, dt.StartTimeOfDay.Minute, dt.StartTimeOfDay.Second));
            return new DateTimeOffset(startLocal, tz.GetUtcOffset(startLocal));
        }
    }
}
