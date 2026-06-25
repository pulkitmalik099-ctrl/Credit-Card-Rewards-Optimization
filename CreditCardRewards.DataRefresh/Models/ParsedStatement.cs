namespace CreditCardRewards.DataRefresh.Models
{
    public class ParsedStatement
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FileName { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        public string? DetectedIssuer { get; set; }
        public string? DetectedCardName { get; set; }
        public DateTime? StatementPeriodStart { get; set; }
        public DateTime? StatementPeriodEnd { get; set; }
        public decimal TotalSpend { get; set; }
        public decimal RewardPointsEarned { get; set; }
        public List<ParsedTransaction> Transactions { get; set; } = new();
        public ParsedStatementStatus Status { get; set; } = ParsedStatementStatus.Pending;
        public DateTime ParsedAt { get; set; } = DateTime.UtcNow;
        public string? ParseError { get; set; }
    }

    public class ParsedTransaction
    {
        public DateTime Date { get; set; }
        public string Merchant { get; set; } = null!;
        public decimal Amount { get; set; }
        public decimal RewardPoints { get; set; }
        public string? Category { get; set; }
    }

    public enum ParsedStatementStatus
    {
        Pending,
        Confirmed,
        Failed
    }
}