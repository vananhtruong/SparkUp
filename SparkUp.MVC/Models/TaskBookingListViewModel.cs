namespace SparkUp.MVC.Models
{
    public class TaskBookingListViewModel
    {
        public int Id { get; set; }
        public string WorkerName { get; set; }
        public string WorkerAvatar { get; set; }
        public string CustomerName { get; set; }
        public string CustomerAvatar { get; set; }
        public string TaskTypeName { get; set; }
        public DateTime ScheduledTime { get; set; }
        public string Address { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string PaymentStatus { get; set; }
        public string EstimatedWork { get; set; }
        public DateTime CreatedAt { get; set; }

        // Helper properties để hiển thị trạng thái
        public string StatusBadgeClass => Status switch
        {
            "Pending" => "bg-warning",
            "Accepted" => "bg-info",
            "Rejected" => "bg-danger",
            "Completed" => "bg-success",
            _ => "bg-secondary"
        };

        public string PaymentStatusBadgeClass => PaymentStatus switch
        {
            "Paid" => "bg-success",
            "Unpaid" => "bg-danger",
            _ => "bg-secondary"
        };
    }

    public class BookingsViewModel
    {
        public string Title { get; set; }
        public List<TaskBookingListViewModel> Bookings { get; set; }
        public bool IsWorkerView { get; set; } = false;
    }    public class TaskBookingDetailViewModel : TaskBookingListViewModel
    {
        public bool IsWorker { get; set; }
        public int WorkerId { get; set; }
    }
}
