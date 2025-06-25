using System.ComponentModel.DataAnnotations;

namespace SparkUp.MVC.Models
{
    public class UserProfileViewModel
    {
        public int Id { get; set; }
          [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;
        
        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [Display(Name = "Ảnh đại diện")]
        public string AvatarUrl { get; set; } = string.Empty;
        
        [Display(Name = "Ảnh đại diện mới")]
        public string NewAvatarUrl { get; set; } = string.Empty;
        
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsWorker { get; set; }
    }
}
