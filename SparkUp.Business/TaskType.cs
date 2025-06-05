namespace SparkUp.Business
{
    public class TaskType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public string ImageUrl { get; set; }
        public string Icon { get; set; }
        
        // Mối quan hệ với Task
        public ICollection<Task> Tasks { get; set; }
        
        // Mối quan hệ 1-nhiều: Một loại công việc có thể có nhiều thợ
        public ICollection<WorkerProfile> WorkerProfiles { get; set; }
    }
}