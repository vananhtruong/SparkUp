using System;
using System.Collections.Generic;

namespace SparkUp.MVC.Models
{
    public class WorkerSearchViewModel
    {
        // Thông tin tìm kiếm cơ bản
        public string Location { get; set; }
        public int? TaskTypeId { get; set; }
        public string SelectedTaskType { get; set; }
        
        // Lọc theo giá và xếp hạng
        public decimal? MaxPrice { get; set; }
        public string SortBy { get; set; } = "Rating"; // Rating, PriceLowToHigh, PriceHighToLow

        public List<WorkerViewModel> Workers { get; set; } = new();
    }
}
