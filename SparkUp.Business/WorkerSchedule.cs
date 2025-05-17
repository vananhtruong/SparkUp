using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkUp.Business
{
    public class WorkerSchedule
    {
        public int Id { get; set; }
        public int WorkerId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Type { get; set; } // Available / Busy / Booked / Break
        public int? TaskId { get; set; }
        public string Note { get; set; }

        public User Worker { get; set; }
        public Task Task { get; set; }
    }

}
