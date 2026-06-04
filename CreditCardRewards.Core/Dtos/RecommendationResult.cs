namespace CreditCardRewards.Core.Dtos
{
    /// <summary>
    /// Ranked recommendation result
    /// </summary>
    public class RecommendationResult
    {
        public int Rank { get; set; }
        public Guid CardId { get; set; }
        public string CardName { get; set; } = null!;
        public decimal RewardValue { get; set; }
        public decimal EffectiveReturnPercentage { get; set; }
        public string Reasoning { get; set; } = null!;
        public RewardCalculationResult Details { get; set; } = null!;
    }
}
