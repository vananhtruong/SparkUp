using System.ComponentModel.DataAnnotations;

namespace SparkUp.Business
{
    public class ChatRoom
    {
        public int Id { get; set; }
          [Required]
        public int TaskId { get; set; }
        
        [Required]
        public int CustomerId { get; set; }
        
        [Required]
        public int WorkerId { get; set; }
        
        public ChatRoomStatus Status { get; set; } = ChatRoomStatus.Active;
        
        public int? AdminUserId { get; set; }
        
        public string? DisputeReason { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? DisputedAt { get; set; }
        
        public DateTime? ResolvedAt { get; set; }
        
        public DateTime LastMessageAt { get; set; } = DateTime.Now;
          // Navigation properties
        public virtual Task Task { get; set; }
        public virtual User Customer { get; set; }
        public virtual User Worker { get; set; }
        public virtual User? AdminUser { get; set; }
        public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        public virtual ICollection<DisputeResolution> DisputeResolutions { get; set; } = new List<DisputeResolution>();
    }
    
    public enum ChatRoomStatus
    {
        Active,
        Disputed,
        AdminReviewing,
        Resolved,
        Closed
    }
}
