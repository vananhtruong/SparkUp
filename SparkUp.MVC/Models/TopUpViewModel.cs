using System.ComponentModel.DataAnnotations;

namespace SparkUp.MVC.Models
{
    public class TopUpViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập số tiền.")]
        [Range(1000, 100000000, ErrorMessage = "Số tiền tối thiểu 1.000đ, tối đa 100.000.000đ")]
        public decimal? Amount { get; set; }
    }
}
