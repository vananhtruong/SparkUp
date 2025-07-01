using System;
using System.Collections.Generic;

namespace SparkUp.Business
{
    public class SupportChatSession
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int? AdminId { get; set; } // Nullable - assigned when admin joins
        public string Subject { get; set; } = string.Empty; // Brief description of the issue
        public string Status { get; set; } = "Open"; // Open / InProgress / Resolved / Closed
        public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public bool IsCustomerOnline { get; set; }
        public bool IsAdminOnline { get; set; }

        // Navigation properties
        public User Customer { get; set; } = null!;
        public User? Admin { get; set; }
        public ICollection<SupportChatMessage> Messages { get; set; } = new List<SupportChatMessage>();
    }
}