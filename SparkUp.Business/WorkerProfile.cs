namespace SparkUp.Business
{
    public class WorkerProfile
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Description { get; set; }
        public int ExperienceYears { get; set; }
        public float RatingAverage { get; set; }
        public bool IsConfirmed { get; set; }
        public string ApprovalStatus { get; set; } = "Pending";

        public string Address { get; set; }
        public string City { get; set; }
        public string District { get; set; }

        public User User { get; set; }
        public ICollection<WorkerTaskType> WorkerTaskTypes { get; set; }
        public ICollection<WorkerCertificate> WorkerCertificates { get; set; }

    }

}