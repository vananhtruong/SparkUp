namespace SparkUp.MVC.Models
{
    public class FeedbackViewModel
    {
        public string FromUserName { get; set; }
        public string FromUserAvatar { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
