using System;

namespace SparkUp.Business
{
    public class SupportChatMessage
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public int SenderId { get; set; }
        public string MessageText { get; set; } = string.Empty;
        public string MessageType { get; set; } = "Text"; // Text / Image / File
        public string? AttachmentUrl { get; set; } // For file/image attachments
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }

        // Navigation properties
        public SupportChatSession Session { get; set; } = null!;
        public User Sender { get; set; } = null!;
    }
}