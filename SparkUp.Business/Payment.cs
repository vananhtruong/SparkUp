using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkUp.Business
{
    public class Payment
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentTime { get; set; }
        public string Method { get; set; } // VNPay, Momo, Cash
        public string Status { get; set; } // Success / Failed / Pending

        public Task Task { get; set; }
    }

}
