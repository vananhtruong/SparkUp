namespace SparkUp.MVC.Models
{
    public class WorkerDetailViewModel : WorkerViewModel
    {        

        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public int TaskTypeId { get; set; } // Thay đổi từ SelectedTaskTypeId sang TaskTypeId và bỏ nullable
        public List<TaskTypePriceViewModel> TaskTypePrices { get; set; }
        public List<FeedbackViewModel> Feedbacks { get; set; }
    }
}
