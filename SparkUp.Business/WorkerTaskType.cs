namespace SparkUp.Business
{
    public class WorkerTaskType
    {
        public int Id { get; set; }
        public int WorkerProfileId { get; set; }
        public int TaskTypeId { get; set; }
        public decimal HourlyRate { get; set; } // Thêm mức giá theo giờ

        public WorkerProfile WorkerProfile { get; set; }
        public TaskType TaskType { get; set; }
    }
}