namespace SparkUp.MVC.Models
{    public class TaskBookingListViewModel
    {
        public int Id { get; set; }
        public string WorkerName { get; set; } = string.Empty;
        public string WorkerAvatar { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerAvatar { get; set; } = string.Empty;
        public string TaskTypeName { get; set; } = string.Empty;
        public DateTime ScheduledTime { get; set; }
        public string Address { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string EstimatedWork { get; set; } = string.Empty;
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
    }    public class BookingsViewModel
    {
        public string Title { get; set; } = string.Empty;
        public List<TaskBookingListViewModel> Bookings { get; set; } = new();
        public bool IsWorkerView { get; set; } = false;
    }public class TaskBookingDetailViewModel : TaskBookingListViewModel
    {
        public bool IsWorker { get; set; }
        public int WorkerId { get; set; }
        public int? ChatRoomId { get; set; }
        public bool HasChatRoom { get; set; }
        public bool CanStartChat { get; set; }
    }
}
