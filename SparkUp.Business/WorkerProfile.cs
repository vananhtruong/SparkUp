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

        public User User { get; set; }
    }

}