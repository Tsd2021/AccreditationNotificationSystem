using Quartz;
using Quartz.Impl.AdoJobStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Model
{
    public class Tarea
    {
        public string jobKey { get; set; }
        public string trigger { get; set; }
        public DateTime? hora { get; set; }
        public TriggerState triggerKey { get; set; }
        public Tarea()
        {

        }

        public bool terminado()
        {
            return triggerKey == TriggerState.Complete;
        }
    }
}
