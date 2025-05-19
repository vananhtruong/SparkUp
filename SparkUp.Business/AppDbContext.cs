using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //    => optionsBuilder.UseSqlServer("server=db16450.public.databaseasp.net;database=db16450;uid=db16450;pwd=12345678;TrustServerCertificate=True;");

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
        }
    }

}