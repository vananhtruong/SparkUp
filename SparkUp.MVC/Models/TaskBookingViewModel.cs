using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.ComponentModel.DataAnnotations;

namespace SparkUp.MVC.Models
{
    public class TaskBookingViewModel
    {
        public int WorkerId { get; set; }
        public int TaskTypeId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày làm việc")]
        public DateTime WorkDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thời gian bắt đầu")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập ước tính thời gian làm")]
        [Range(1, 8, ErrorMessage = "Thời gian làm việc phải từ 1-8 giờ")]
        public int EstimatedHours { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        public string Address { get; set; }        
        [Required(ErrorMessage = "Vui lòng mô tả công việc")]
        public string Description { get; set; }

        [BindNever]
        public List<WorkerScheduleSlot> AvailableSlots { get; set; }
    }

    public class WorkerScheduleSlot
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsAvailable { get; set; }
    }
}