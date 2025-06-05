using System.ComponentModel.DataAnnotations;

namespace SparkUp.MVC.Models
{
    public class UserProfileViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }
        
        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; }
        
        [Display(Name = "Ảnh đại diện")]
        public string AvatarUrl { get; set; }
        
        [Display(Name = "Ảnh đại diện mới")]
        public string NewAvatarUrl { get; set; }
        
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsWorker { get; set; }
    }
}
