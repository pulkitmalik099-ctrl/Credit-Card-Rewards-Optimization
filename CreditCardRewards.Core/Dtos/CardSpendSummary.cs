namespace CreditCardRewards.Core.Dtos
{
    /// <summary>
    /// Spend summary for a card
    /// </summary>
    public class CardSpendSummary
    {
        public Guid CardId { get; set; }
        public string CardName { get; set; } = null!;
        public decimal CurrentMonthlySpend { get; set; }
        public decimal CurrentAnnualSpend { get; set; }
        public decimal AnnualFeeWaiverThreshold { get; set; }
        public decimal AnnualFeeWaiverProgress { get; set; }
        public decimal AnnualFeeWaiverRemaining { get; set; }
        public bool IsFeeWaiverAchieved { get; set; }
        public List<MilestoneProgress> MilestoneProgress { get; set; } = new();
        public decimal TotalRewardValueEarned { get; set; }
    }

    /// <summary>
    /// Milestone progress tracking
    /// </summary>
    public class MilestoneProgress
    {
        public Guid MilestoneId { get; set; }
        public string Title { get; set; } = null!;
        public decimal SpendThreshold { get; set; }
        public decimal RewardValue { get; set; }
        public decimal CurrentSpend { get; set; }
        public decimal RemainingSpend { get; set; }
        public bool IsAchieved { get; set; }
    }
}
