namespace SparkUp.MVC.Models
{
    public class WorkerSearchViewModel
    {
        public string Location { get; set; }
        public int? TaskTypeId { get; set; }
        public string SearchTask { get; set; }
        public string SelectedTaskType { get; set; }
        public List<WorkerViewModel> Workers { get; set; }
    }
}
