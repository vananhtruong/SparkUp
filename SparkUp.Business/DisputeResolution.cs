using System.ComponentModel.DataAnnotations;

namespace SparkUp.Business
{
    public class DisputeResolution
    {
        public int Id { get; set; }
          [Required]
        public int ChatRoomId { get; set; }
        
        [Required]
        public int AdminUserId { get; set; }
        
        [Required]
        [MaxLength(1000)]
        public string Resolution { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string ActionTaken { get; set; } // 'refund_customer', 'penalize_worker', 'no_action', etc.
        
        public decimal? RefundAmount { get; set; }
        
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
          // Navigation properties
        public virtual ChatRoom ChatRoom { get; set; }
        public virtual User AdminUser { get; set; }
    }
}
