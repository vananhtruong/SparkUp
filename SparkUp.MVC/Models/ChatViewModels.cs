using SparkUp.Business;

namespace SparkUp.MVC.Models
{
    public class ChatStatisticsViewModel
    {
        public int TotalChatRooms { get; set; }
        public int ActiveChats { get; set; }
        public int DisputedChats { get; set; }
        public int ResolvedDisputes { get; set; }
        public int ClosedChats { get; set; }
        public int PendingAdminReview { get; set; }
        public double AverageResolutionTimeHours { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    public class ChatRoomViewModel
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string WorkerName { get; set; } = string.Empty;
        public string? AdminName { get; set; }
     
        public ChatRoomStatus Status { get; set; }
        public string? DisputeReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DisputedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
        public string? LastMessage { get; set; }
        public bool CanAccess { get; set; }
        public string UserRole { get; set; } = string.Empty; // "Customer", "Worker", "Admin"
    }

    public class ChatMessageViewModel
    {
        public int Id { get; set; }
        public int ChatRoomId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public bool IsDeleted { get; set; }
        public ChatMessageType MessageType { get; set; }
        public bool IsOwnMessage { get; set; }
        public string SenderRole { get; set; } = string.Empty; // "Customer", "Worker", "Admin"
    }    public class SendMessageRequest
    {
        public int ChatRoomId { get; set; }
        public string Content { get; set; } = string.Empty;
        public ChatMessageType? MessageType { get; set; } = SparkUp.Business.ChatMessageType.Text;
    }

    public class InitiateDisputeRequest
    {
        public int ChatRoomId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class ResolveDisputeRequest
    {
        public int ChatRoomId { get; set; }
        public string Resolution { get; set; } = string.Empty;
        public string ActionTaken { get; set; } = string.Empty;
        public decimal? RefundAmount { get; set; }        public string? Notes { get; set; }
    }

    public class FloatingChatViewModel
    {
        public bool CanShowChat { get; set; }
        public int? ChatRoomId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string WorkerName { get; set; } = string.Empty;
        public bool IsWorker { get; set; }
        public int UnreadCount { get; set; }
        public int CurrentUserId { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
