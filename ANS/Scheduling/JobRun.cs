using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Scheduling
{
    public enum JobRunStatus
    {
        Planned = 0,
        Pending = 1,
        Succeeded = 2,
        Failed = 3,
        Misfired = 4,
        SkippedByShutdown = 5
    }


    public sealed class JobRun
    {
        public long Id { get; set; }
        public string JobName { get; set; } = "";
        public string JobGroup { get; set; } = "";
        public string TriggerName { get; set; } = "";
        public string TriggerGroup { get; set; } = "";

        public DateTimeOffset ScheduledFireTimeUtc { get; set; }
        public DateTimeOffset? ActualFireTimeUtc { get; set; }
        public DateTimeOffset? CompletedTimeUtc { get; set; }

        public JobRunStatus Status { get; set; }
        public string? Message { get; set; }  // error/observación
    }
}
