namespace CreditCardRewards.Core.Dtos
{
    /// <summary>
    /// Result of reward calculation for a transaction on a specific card
    /// </summary>
    public class RewardCalculationResult
    {
        public Guid CardId { get; set; }
        public string CardName { get; set; } = null!;
        public decimal TransactionAmount { get; set; }
        public decimal PointsEarned { get; set; }
        public decimal CashbackEarned { get; set; } // In rupees
        public decimal EstimatedValue { get; set; } // Total reward value in ₹
        public decimal EffectiveReturnPercentage { get; set; }
        public bool MilestoneContributed { get; set; }
        public decimal MilestoneContributionValue { get; set; }
        public bool FeeWaiverContributed { get; set; }
        public decimal FeeWaiverContributionValue { get; set; }
        public string Reasoning { get; set; } = null!;
    }
}
