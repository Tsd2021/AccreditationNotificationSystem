using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Scheduling
{
    public interface IJobHistoryStore
    {
        Task InitializeAsync();
        Task<long> AddPlannedAsync(JobKey jobKey, TriggerKey triggerKey, DateTimeOffset scheduledUtc, JobRunStatus status);
        Task MarkStartAsync(JobKey jobKey, DateTimeOffset scheduledUtc, DateTimeOffset actualUtc);
        Task MarkEndAsync(JobKey jobKey, DateTimeOffset scheduledUtc, bool ok, string? message = null);
        Task MarkSkippedAsync(JobKey jobKey, TriggerKey triggerKey, DateTimeOffset scheduledUtc, string reason);
        Task MarkMisfireAsync(JobKey jobKey, TriggerKey triggerKey, DateTimeOffset scheduledUtc, string? info = null);

        Task<DateTimeOffset?> GetLastShutdownUtcAsync();
        Task SaveLastShutdownUtcAsync(DateTimeOffset whenUtc);
        Task<JobRun?> GetLastForJobAsync(JobKey jobKey, bool excludeSkipped = true);
        Task<IReadOnlyList<JobRun>> GetRecentAsync(int top = 500);
        Task<bool> ExistsAsync(JobKey jobKey, DateTimeOffset scheduledUtc);
    }
}
