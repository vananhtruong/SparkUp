namespace SparkUp.MVC.Models
{    public class TaskTypePriceViewModel
    {
        public int TaskTypeId { get; set; }
        public string TaskTypeName { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; }
    }
}
