namespace SparkUp.Business
{
    public class WorkerProfile
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Skills { get; set; }
        public string Description { get; set; }
        public int ExperienceYears { get; set; }
        public string PortfolioUrl { get; set; }
        public float RatingAverage { get; set; }
        public bool IsConfirmed { get; set; }
        public string ApprovalStatus { get; set; } = "Pending";

        public string Address { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        
        // Mối quan hệ 1-nhiều: Một thợ chỉ có một loại công việc
        public int TaskTypeId { get; set; }
        public decimal HourlyRate { get; set; } // Mức giá theo giờ

        public User User { get; set; }
        public TaskType TaskType { get; set; } // Navigation property
    }
}