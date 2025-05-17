namespace SparkUp.Business
{
    public class WalletTransaction
    {
        public int Id { get; set; }
        public int WalletId { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } // Deposit / Withdraw / Payment / Refund
        public int? TaskId { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } // Success / Pending / Failed

        public Wallet Wallet { get; set; }
        public Task Task { get; set; }
    }

}