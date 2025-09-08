using Microsoft.Data.Sqlite;
using Quartz;
using System.IO;

namespace ANS.Scheduling
{
    public sealed class RepositorioJobHistory : IJobHistoryStore
    {
        private readonly string _dbPath;
        public RepositorioJobHistory(string dbPath) => _dbPath = dbPath;

        private SqliteConnection Open()
        {
            var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();
            return conn;
        }

        public async Task InitializeAsync()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
            using var c = Open();
            var cmd = c.CreateCommand();

            cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS JobRun (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            JobName TEXT NOT NULL,
            JobGroup TEXT NOT NULL,
            TriggerName TEXT NOT NULL,
            TriggerGroup TEXT NOT NULL,
            ScheduledFireTimeUtc TEXT NOT NULL,
            ActualFireTimeUtc TEXT NULL,
            CompletedTimeUtc TEXT NULL,
            Status INTEGER NOT NULL,
            Message TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS AppMeta (
            Key TEXT PRIMARY KEY,
            Value TEXT
            );";

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<long> AddPlannedAsync(JobKey jobKey, TriggerKey triggerKey, DateTimeOffset scheduledUtc, JobRunStatus status)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = 
            @"INSERT INTO JobRun (JobName, JobGroup, TriggerName, TriggerGroup, ScheduledFireTimeUtc, Status)
            VALUES ($jn, $jg, $tn, $tg, $s, $st);
            SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("$jn", jobKey.Name);
            cmd.Parameters.AddWithValue("$jg", jobKey.Group ?? "");
            cmd.Parameters.AddWithValue("$tn", triggerKey.Name);
            cmd.Parameters.AddWithValue("$tg", triggerKey.Group ?? "");
            cmd.Parameters.AddWithValue("$s", scheduledUtc.UtcDateTime.ToString("o"));
            cmd.Parameters.AddWithValue("$st", (int)status);
            var id = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
            return id;
        }



        public async Task<JobRun?> GetLastForJobAsync(JobKey jobKey, bool excludeSkipped = true)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
            SELECT Id, JobName, JobGroup, TriggerName, TriggerGroup,
            ScheduledFireTimeUtc, ActualFireTimeUtc, CompletedTimeUtc,
            Status, Message
            FROM JobRun
            WHERE JobName=$jn AND JobGroup=$jg
            " + (excludeSkipped ? "AND Status <> $sk " : "") + @"
            ORDER BY datetime(ScheduledFireTimeUtc) DESC
            LIMIT 1;";
            cmd.Parameters.AddWithValue("$jn", jobKey.Name);
            cmd.Parameters.AddWithValue("$jg", jobKey.Group ?? "");
            cmd.Parameters.AddWithValue("$sk", (int)JobRunStatus.SkippedByShutdown);
            using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                return new JobRun
                {
                    Id = r.GetInt64(0),
                    JobName = r.GetString(1),
                    JobGroup = r.GetString(2),
                    TriggerName = r.GetString(3),
                    TriggerGroup = r.GetString(4),
                    ScheduledFireTimeUtc = DateTimeOffset.Parse(r.GetString(5)),
                    ActualFireTimeUtc = r.IsDBNull(6) ? null : DateTimeOffset.Parse(r.GetString(6)),
                    CompletedTimeUtc = r.IsDBNull(7) ? null : DateTimeOffset.Parse(r.GetString(7)),
                    Status = (JobRunStatus)r.GetInt32(8),
                    Message = r.IsDBNull(9) ? null : r.GetString(9)
                };
            }
            return null;
        }

        public async Task MarkStartAsync(JobKey jobKey, DateTimeOffset scheduledUtc, DateTimeOffset actualUtc)
        {
            using var c = Open();
            using var tx = c.BeginTransaction();

            var update = c.CreateCommand();
            update.CommandText = @"
        UPDATE JobRun
        SET ActualFireTimeUtc=$a, Status=$st
        WHERE JobName=$jn AND JobGroup=$jg AND ScheduledFireTimeUtc=$s";
            update.Parameters.AddWithValue("$a", actualUtc.UtcDateTime.ToString("o"));
            update.Parameters.AddWithValue("$st", (int)JobRunStatus.Pending);
            update.Parameters.AddWithValue("$jn", jobKey.Name);
            update.Parameters.AddWithValue("$jg", jobKey.Group ?? "");
            update.Parameters.AddWithValue("$s", scheduledUtc.UtcDateTime.ToString("o"));

            var rows = await update.ExecuteNonQueryAsync();
            if (rows == 0)
            {
                var insert = c.CreateCommand();
                insert.CommandText = @"
            INSERT INTO JobRun (JobName, JobGroup, TriggerName, TriggerGroup, ScheduledFireTimeUtc, ActualFireTimeUtc, Status)
            VALUES ($jn,$jg,'','',$s,$a,$st)";
                insert.Parameters.AddWithValue("$jn", jobKey.Name);
                insert.Parameters.AddWithValue("$jg", jobKey.Group ?? "");
                insert.Parameters.AddWithValue("$s", scheduledUtc.UtcDateTime.ToString("o"));
                insert.Parameters.AddWithValue("$a", actualUtc.UtcDateTime.ToString("o"));
                insert.Parameters.AddWithValue("$st", (int)JobRunStatus.Pending);
                await insert.ExecuteNonQueryAsync();
            }

            tx.Commit();
        }

        public async Task MarkEndAsync(JobKey jobKey, DateTimeOffset scheduledUtc, bool ok, string? message = null)
        {
            using var c = Open();
            using var tx = c.BeginTransaction();

            var update = c.CreateCommand();
            update.CommandText = @"
        UPDATE JobRun
        SET CompletedTimeUtc=$c, Status=$st, Message=$m
        WHERE JobName=$jn AND JobGroup=$jg AND ScheduledFireTimeUtc=$s";
            update.Parameters.AddWithValue("$c", DateTimeOffset.UtcNow.UtcDateTime.ToString("o"));
            update.Parameters.AddWithValue("$st", (int)(ok ? JobRunStatus.Succeeded : JobRunStatus.Failed));
            update.Parameters.AddWithValue("$m", (object?)message ?? DBNull.Value);
            update.Parameters.AddWithValue("$jn", jobKey.Name);
            update.Parameters.AddWithValue("$jg", jobKey.Group ?? "");
            update.Parameters.AddWithValue("$s", scheduledUtc.UtcDateTime.ToString("o"));

            var rows = await update.ExecuteNonQueryAsync();
            if (rows == 0)
            {
                var insert = c.CreateCommand();
                insert.CommandText = @"
            INSERT INTO JobRun (JobName, JobGroup, TriggerName, TriggerGroup, ScheduledFireTimeUtc, CompletedTimeUtc, Status, Message)
            VALUES ($jn,$jg,'','',$s,$c,$st,$m)";
                insert.Parameters.AddWithValue("$jn", jobKey.Name);
                insert.Parameters.AddWithValue("$jg", jobKey.Group ?? "");
                insert.Parameters.AddWithValue("$s", scheduledUtc.UtcDateTime.ToString("o"));
                insert.Parameters.AddWithValue("$c", DateTimeOffset.UtcNow.UtcDateTime.ToString("o"));
                insert.Parameters.AddWithValue("$st", (int)(ok ? JobRunStatus.Succeeded : JobRunStatus.Failed));
                insert.Parameters.AddWithValue("$m", (object?)message ?? DBNull.Value);
                await insert.ExecuteNonQueryAsync();
            }

            tx.Commit();
        }

        public async Task MarkSkippedAsync(JobKey jobKey, TriggerKey triggerKey, DateTimeOffset scheduledUtc, string reason)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
            INSERT OR REPLACE INTO JobRun (Id, JobName, JobGroup, TriggerName, TriggerGroup, ScheduledFireTimeUtc, Status, Message)
            VALUES (COALESCE((SELECT Id FROM JobRun WHERE JobName=$jn AND JobGroup=$jg AND ScheduledFireTimeUtc=$s), NULL),
            $jn,$jg,$tn,$tg,$s,$st,$m)";
            cmd.Parameters.AddWithValue("$jn", jobKey.Name);
            cmd.Parameters.AddWithValue("$jg", jobKey.Group ?? "");
            cmd.Parameters.AddWithValue("$tn", triggerKey.Name);
            cmd.Parameters.AddWithValue("$tg", triggerKey.Group ?? "");
            cmd.Parameters.AddWithValue("$s", scheduledUtc.UtcDateTime.ToString("o"));
            cmd.Parameters.AddWithValue("$st", (int)JobRunStatus.SkippedByShutdown);
            cmd.Parameters.AddWithValue("$m", reason);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task MarkMisfireAsync(JobKey jobKey, TriggerKey triggerKey, DateTimeOffset scheduledUtc, string? info = null)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
            INSERT OR REPLACE INTO JobRun (Id, JobName, JobGroup, TriggerName, TriggerGroup, ScheduledFireTimeUtc, Status, Message)
            VALUES (COALESCE((SELECT Id FROM JobRun WHERE JobName=$jn AND JobGroup=$jg AND ScheduledFireTimeUtc=$s), NULL),
            $jn,$jg,$tn,$tg,$s,$st,$m)";
            cmd.Parameters.AddWithValue("$jn", jobKey.Name);
            cmd.Parameters.AddWithValue("$jg", jobKey.Group ?? "");
            cmd.Parameters.AddWithValue("$tn", triggerKey.Name);
            cmd.Parameters.AddWithValue("$tg", triggerKey.Group ?? "");
            cmd.Parameters.AddWithValue("$s", scheduledUtc.UtcDateTime.ToString("o"));
            cmd.Parameters.AddWithValue("$st", (int)JobRunStatus.Misfired);
            cmd.Parameters.AddWithValue("$m", info ?? "Trigger misfire");
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<IReadOnlyList<JobRun>> GetRecentAsync(int top = 500)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = $@"
            SELECT Id, JobName, JobGroup, TriggerName, TriggerGroup, ScheduledFireTimeUtc, ActualFireTimeUtc, CompletedTimeUtc, Status, Message
            FROM JobRun
            ORDER BY datetime(ScheduledFireTimeUtc) DESC
            LIMIT {top}";
            var list = new List<JobRun>();
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new JobRun
                {
                    Id = r.GetInt64(0),
                    JobName = r.GetString(1),
                    JobGroup = r.GetString(2),
                    TriggerName = r.GetString(3),
                    TriggerGroup = r.GetString(4),
                    ScheduledFireTimeUtc = DateTimeOffset.Parse(r.GetString(5)),
                    ActualFireTimeUtc = r.IsDBNull(6) ? null : DateTimeOffset.Parse(r.GetString(6)),
                    CompletedTimeUtc = r.IsDBNull(7) ? null : DateTimeOffset.Parse(r.GetString(7)),
                    Status = (JobRunStatus)r.GetInt32(8),
                    Message = r.IsDBNull(9) ? null : r.GetString(9)
                });
            }
            return list;
        }

        public async Task<bool> ExistsAsync(JobKey jobKey, DateTimeOffset scheduledUtc)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
            SELECT 1 FROM JobRun WHERE JobName=$jn AND JobGroup=$jg AND ScheduledFireTimeUtc=$s LIMIT 1";
            cmd.Parameters.AddWithValue("$jn", jobKey.Name);
            cmd.Parameters.AddWithValue("$jg", jobKey.Group ?? "");
            cmd.Parameters.AddWithValue("$s", scheduledUtc.UtcDateTime.ToString("o"));
            return (await cmd.ExecuteScalarAsync()) != null;
        }

        public async Task<DateTimeOffset?> GetLastShutdownUtcAsync()
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT Value FROM AppMeta WHERE Key='LastShutdownUtc' LIMIT 1;";
            var val = (string?)await cmd.ExecuteScalarAsync();
            return string.IsNullOrEmpty(val) ? null : DateTimeOffset.Parse(val);
        }

        public async Task SaveLastShutdownUtcAsync(DateTimeOffset whenUtc)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
            INSERT INTO AppMeta (Key, Value) VALUES ('LastShutdownUtc', $v)
            ON CONFLICT(Key) DO UPDATE SET Value=$v;";
            cmd.Parameters.AddWithValue("$v", whenUtc.UtcDateTime.ToString("o"));
            await cmd.ExecuteNonQueryAsync();
        }
    }
}

