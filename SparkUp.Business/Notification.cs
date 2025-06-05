using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkUp.Business
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; } // Nội dung chi tiết của thông báo
        public string Type { get; set; } // e.g., "Booking", "Payment", "System", "TaskUpdate"
        public string ReferenceId { get; set; } // ID liên kết (TaskId, PaymentId, etc.)
        public string Action { get; set; } // Hành động khi click: "view", "accept", "reject", etc.
        public string RedirectUrl { get; set; } // URL chuyển hướng khi click vào thông báo
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; } // Thời gian đã đọc thông báo

        public User User { get; set; }
    }
    
}
