using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task = SparkUp.Business.Task;


namespace SparkUp.Business
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public AppDbContext() { }
        // DbSet declarations
        public DbSet<User> Users { get; set; }
        public DbSet<WorkerProfile> WorkerProfiles { get; set; }
        public DbSet<Task> Tasks { get; set; }
        public DbSet<TaskType> TaskTypes { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<WorkerSchedule> WorkerSchedules { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }
        public DbSet<ChatRoom> ChatRooms { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<DisputeResolution> DisputeResolutions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Table names (optional if you want pluralized)
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<WorkerProfile>().ToTable("WorkerProfiles");
            modelBuilder.Entity<Task>().ToTable("Tasks");
            modelBuilder.Entity<TaskType>().ToTable("TaskTypes");
            modelBuilder.Entity<Payment>().ToTable("Payments");
            modelBuilder.Entity<Feedback>().ToTable("Feedbacks");
            modelBuilder.Entity<Message>().ToTable("Messages");
            modelBuilder.Entity<Notification>().ToTable("Notifications");
            modelBuilder.Entity<WorkerSchedule>().ToTable("WorkerSchedules");
            modelBuilder.Entity<Wallet>().ToTable("Wallets");
            modelBuilder.Entity<WalletTransaction>().ToTable("WalletTransactions");
            modelBuilder.Entity<ChatRoom>().ToTable("ChatRooms");
            modelBuilder.Entity<ChatMessage>().ToTable("ChatMessages");
            modelBuilder.Entity<DisputeResolution>().ToTable("DisputeResolutions");

            // User <-> WorkerProfile 1:1
            modelBuilder.Entity<WorkerProfile>()
                .HasOne(wp => wp.User)
                .WithOne(u => u.WorkerProfile)
                .HasForeignKey<WorkerProfile>(wp => wp.UserId);

            // User <-> Wallet 1:1
            modelBuilder.Entity<Wallet>()
                .HasOne(w => w.User)
                .WithOne(u => u.Wallet)
                .HasForeignKey<Wallet>(w => w.UserId);

            // User <-> Task (Customer & Worker)
            modelBuilder.Entity<Task>()
                .HasOne(t => t.Customer)
                .WithMany(u => u.CustomerTasks)
                .HasForeignKey(t => t.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Task>()
                .HasOne(t => t.Worker)
                .WithMany(u => u.WorkerTasks)
                .HasForeignKey(t => t.WorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Task <-> TaskType
            modelBuilder.Entity<Task>()
                .HasOne(t => t.TaskType)
                .WithMany(tt => tt.Tasks)
                .HasForeignKey(t => t.TaskTypeId);

            // Task <-> Payment 1:1
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Task)
                .WithOne(t => t.Payment)
                .HasForeignKey<Payment>(p => p.TaskId);

            // Feedback relations
            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.Task)
                .WithMany(t => t.Feedbacks)
                .HasForeignKey(f => f.TaskId);

            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.FromUser)
                .WithMany()
                .HasForeignKey(f => f.FromUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.ToWorkerUser)
                .WithMany()
                .HasForeignKey(f => f.ToWorker)
                .OnDelete(DeleteBehavior.Restrict);

            // Message relations
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Task)
                .WithMany(t => t.Messages)
                .HasForeignKey(m => m.TaskId);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Notification
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId);

            // WorkerSchedule
            modelBuilder.Entity<WorkerSchedule>()
                .HasOne(ws => ws.Worker)
                .WithMany()
                .HasForeignKey(ws => ws.WorkerId);
            
            modelBuilder.Entity<WorkerSchedule>()
                .HasOne(ws => ws.Task)
                .WithMany()
                .HasForeignKey(ws => ws.TaskId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // WalletTransaction
            modelBuilder.Entity<WalletTransaction>()
                .HasOne(wt => wt.Wallet)
                .WithMany(w => w.Transactions)
                .HasForeignKey(wt => wt.WalletId);

            modelBuilder.Entity<WalletTransaction>()
                .HasOne(wt => wt.Task)
                .WithMany()
                .HasForeignKey(wt => wt.TaskId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // WorkerProfile <-> TaskType (một thợ chỉ có một loại công việc)
            modelBuilder.Entity<WorkerProfile>()
                .HasOne(wp => wp.TaskType)
                .WithMany(tt => tt.WorkerProfiles)
                .HasForeignKey(wp => wp.TaskTypeId);

            // Chat system relationships
            // Task <-> ChatRoom 1:1
            modelBuilder.Entity<ChatRoom>()
                .HasOne(cr => cr.Task)
                .WithOne(t => t.ChatRoom)
                .HasForeignKey<ChatRoom>(cr => cr.TaskId);

            // ChatRoom <-> User (Customer)
            modelBuilder.Entity<ChatRoom>()
                .HasOne(cr => cr.Customer)
                .WithMany()
                .HasForeignKey(cr => cr.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // ChatRoom <-> User (Worker)
            modelBuilder.Entity<ChatRoom>()
                .HasOne(cr => cr.Worker)
                .WithMany()
                .HasForeignKey(cr => cr.WorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            // ChatRoom <-> User (Admin) - optional
            modelBuilder.Entity<ChatRoom>()
                .HasOne(cr => cr.AdminUser)
                .WithMany()
                .HasForeignKey(cr => cr.AdminUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // ChatMessage <-> ChatRoom
            modelBuilder.Entity<ChatMessage>()
                .HasOne(cm => cm.ChatRoom)
                .WithMany(cr => cr.Messages)
                .HasForeignKey(cm => cm.ChatRoomId);            // ChatMessage <-> User (Sender)
            modelBuilder.Entity<ChatMessage>()
                .HasOne(cm => cm.Sender)
                .WithMany()
                .HasForeignKey(cm => cm.SenderId)
                .OnDelete(DeleteBehavior.SetNull);

            // DisputeResolution <-> ChatRoom
            modelBuilder.Entity<DisputeResolution>()
                .HasOne(dr => dr.ChatRoom)
                .WithMany(cr => cr.DisputeResolutions)
                .HasForeignKey(dr => dr.ChatRoomId);

            // DisputeResolution <-> User (Admin)
            modelBuilder.Entity<DisputeResolution>()
                .HasOne(dr => dr.AdminUser)
                .WithMany()
                .HasForeignKey(dr => dr.AdminUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure chat message content max length
            modelBuilder.Entity<ChatMessage>()
                .Property(cm => cm.Content)
                .HasMaxLength(2000);
            
            // Configure dispute resolution notes max length
            modelBuilder.Entity<DisputeResolution>()
                .Property(dr => dr.Resolution)
                .HasMaxLength(1000);            // Configure decimal precision for refund amount
            modelBuilder.Entity<DisputeResolution>()
                .Property(dr => dr.RefundAmount)
                .HasColumnType("decimal(18,2)");
            
            // Configure decimal precision for Payment Amount
            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");
            
            // Configure decimal precision for Wallet Balance
            modelBuilder.Entity<Wallet>()
                .Property(w => w.Balance)
                .HasColumnType("decimal(18,2)");
            
            // Configure decimal precision for WalletTransaction Amount
            modelBuilder.Entity<WalletTransaction>()
                .Property(wt => wt.Amount)
                .HasColumnType("decimal(18,2)");
            
            // Configure decimal precision for WorkerProfile HourlyRate
            modelBuilder.Entity<WorkerProfile>()
                .Property(wp => wp.HourlyRate)
                .HasColumnType("decimal(18,2)");
        }
    }

}