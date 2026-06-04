namespace CreditCardRewards.Core.Dtos
{
    /// <summary>
    /// Request to calculate rewards for a transaction
    /// </summary>
    public class RewardCalculationRequest
    {
        public Guid CardId { get; set; }
        public decimal TransactionAmount { get; set; }
        public string Merchant { get; set; } = null!;
        public string Category { get; set; } = null!;
        public decimal CurrentMonthlySpend { get; set; } = 0;
        public decimal CurrentAnnualSpend { get; set; } = 0;
        public decimal CurrentAnnualFeeWaiverProgress { get; set; } = 0;
    }
}
