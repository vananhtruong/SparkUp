using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkUp.Business
{
    public class Message
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int SenderId { get; set; }
        public string MessageText { get; set; }
        public DateTime SentAt { get; set; }

        public Task Task { get; set; }
        public User Sender { get; set; }
    }

}
