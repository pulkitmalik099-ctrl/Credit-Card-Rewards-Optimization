namespace CreditCardRewards.Core.Dtos
{
    /// <summary>
    /// Request for transaction recommendation across portfolio
    /// </summary>
    public class TransactionRecommendationRequest
    {
        public decimal Amount { get; set; }
        public string Merchant { get; set; } = null!;
        public string Category { get; set; } = null!;
        public bool EnableMilestoneMode { get; set; } = false;
        public List<CardSpendEntry> CardSpends { get; set; } = new();
    }
}
