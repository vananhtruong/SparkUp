using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace SparkUp.MVC.Models
{
    public class WorkerRegistrationViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn loại công việc")]
        public int TaskTypeId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập kỹ năng của bạn")]
        public string Skills { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mô tả về bản thân")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số năm kinh nghiệm")]
        [Range(0, 50, ErrorMessage = "Số năm kinh nghiệm phải từ 0 đến 50")]
        public int ExperienceYears { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập thành phố")]
        public string City { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập quận/huyện")]
        public string District { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá theo giờ")]
        [Range(0, 10000000, ErrorMessage = "Giá phải từ 0 đến 10.000.000")]
        public decimal HourlyRate { get; set; }

        public string PortfolioUrl { get; set; }
    }
}
