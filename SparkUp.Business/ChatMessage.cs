using System.ComponentModel.DataAnnotations;

namespace SparkUp.Business
{
    public class ChatMessage
    {
        public int Id { get; set; }
          [Required]
        public int ChatRoomId { get; set; }
        
        public int? SenderId { get; set; }
          [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.Now;
        
        public bool IsRead { get; set; } = false;
        
        public bool IsDeleted { get; set; } = false;
        
        public ChatMessageType MessageType { get; set; } = ChatMessageType.Text;
        
        // Navigation properties
        public virtual ChatRoom ChatRoom { get; set; }
        public virtual User Sender { get; set; }
    }
      public enum ChatMessageType
    {
        Text,
        System,
        AdminNote,
        DisputeInitiated,
        DisputeResolved,
        TaskStatusUpdate
    }
}
