using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkUp.Business
{
    public class Feedback
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int FromUserId { get; set; }
        public int ToWorker { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }

        public Task Task { get; set; }
        public User FromUser { get; set; }
        public User ToWorkerUser { get; set; }
    }

}
