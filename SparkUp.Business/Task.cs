using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkUp.Business
{
    public class Task
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int WorkerId { get; set; }
        public int TaskTypeId { get; set; }
        public string Address { get; set; }
        public string EstimatedWork { get; set; }
        public string Description { get; set; }
        public DateTime ScheduledTime { get; set; }
        public string Status { get; set; } // Pending / Confirmed / InProgress / Done / Canceled
        public string PaymentStatus { get; set; } // Unpaid / PAID / Refunded
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public User Customer { get; set; }
        public User Worker { get; set; }
        public TaskType TaskType { get; set; }

        public ICollection<Message> Messages { get; set; }
        public ICollection<Feedback> Feedbacks { get; set; }
        public Payment Payment { get; set; }
    }

}
