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
        public ICollection<Task> Tasks { get; set; }
    }
        

}