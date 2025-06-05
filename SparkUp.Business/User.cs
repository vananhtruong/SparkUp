namespace SparkUp.Business
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }  // Customer / Worker / Admin
        public string AvatarUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public WorkerProfile WorkerProfile { get; set; }
        public Wallet Wallet { get; set; }
        public ICollection<Task> CustomerTasks { get; set; }
        public ICollection<Task> WorkerTasks { get; set; }
        public ICollection<Notification> Notifications { get; set; }
    }

}
