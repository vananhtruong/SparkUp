namespace SparkUp.MVC.Models
{
    public class WorkerViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string AvatarUrl { get; set; }
        public float Rating { get; set; }
        public int ExperienceYears { get; set; }
        public string Skills { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public decimal HourlyRate { get; set; } // Giá theo giờ

        public List<string> TaskTypes { get; set; }
    }
}
