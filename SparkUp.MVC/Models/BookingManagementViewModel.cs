using System.Collections.Generic;

namespace SparkUp.MVC.Models
{
    public class BookingManagementViewModel
    {
        public bool IsWorker { get; set; }
        public List<TaskBookingListViewModel> UpcomingBookings { get; set; }
        public List<TaskBookingListViewModel> PendingBookings { get; set; }
        public List<TaskBookingListViewModel> CompletedBookings { get; set; }

        public BookingManagementViewModel()
        {
            UpcomingBookings = new List<TaskBookingListViewModel>();
            PendingBookings = new List<TaskBookingListViewModel>();
            CompletedBookings = new List<TaskBookingListViewModel>();
        }
    }
}