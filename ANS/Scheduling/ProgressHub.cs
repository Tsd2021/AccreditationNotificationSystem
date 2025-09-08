using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Scheduling
{
    public static class ProgressHub
    {
        // 0..1 (porcentaje). Si no reportas, la UI muestra indeterminado
        public static event Action<JobKey, double>? ProgressChanged;
        public static event Action<JobKey, bool>? RunningChanged;

        public static void Start(JobKey key) => RunningChanged?.Invoke(key, true);
        public static void Report(JobKey key, double value01) => ProgressChanged?.Invoke(key, value01);
        public static void Finish(JobKey key) => RunningChanged?.Invoke(key, false);
    }
}
