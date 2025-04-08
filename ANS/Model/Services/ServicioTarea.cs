

using Quartz;
using Quartz.Impl.Matchers;

namespace ANS.Model.Services
{
    public class ServicioTarea
    {

        public static ServicioTarea instancia { get; set; }

        public static ServicioTarea getInstancia()
        {
            if (instancia == null)
            {
                instancia = new ServicioTarea();
            }
            return instancia;
        }

        public async Task<List<Tarea>> obtenerTareasDelScheduler(IScheduler scheduler)
        {
            if (scheduler != null)
            {
                var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());

                var scheduledJobs = new List<Tarea>();

                foreach (var jobKey in jobKeys)
                {
                    var triggers = await scheduler.GetTriggersOfJob(jobKey);
                    foreach (var trigger in triggers)
                    {
                        // Obtenemos el siguiente fire time del trigger (convertido a hora local)
                        var nextFireTime = trigger.GetNextFireTimeUtc()?.LocalDateTime;

                        if (nextFireTime.HasValue && nextFireTime.Value.Date == DateTime.Today)
                        {
                            var triggerState = await scheduler.GetTriggerState(trigger.Key);

                            scheduledJobs.Add(new Tarea
                            {
                                jobKey = jobKey.Name,
                                trigger = trigger.StartTimeUtc.DateTime.ToString(),
                                triggerKey = triggerState,
                                hora = trigger.StartTimeUtc.DateTime
                            });
                        }
                    }
                }
                return scheduledJobs;
            }
            return null;
        }

    }
}
