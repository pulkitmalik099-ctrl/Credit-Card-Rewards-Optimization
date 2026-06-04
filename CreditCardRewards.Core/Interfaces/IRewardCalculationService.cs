using CreditCardRewards.Core.Dtos;

namespace CreditCardRewards.Core.Interfaces
{
    /// <summary>
    /// Interface for reward calculation logic
    /// </summary>
    public interface IRewardCalculationService
    {
        /// <summary>
        /// Calculate rewards for a transaction on a specific card
        /// </summary>
        Task<RewardCalculationResult> CalculateRewardsAsync(RewardCalculationRequest request);

        /// <summary>
        /// Calculate rewards for a transaction on all cards
        /// </summary>
        Task<List<RewardCalculationResult>> CalculateRewardsForAllCardsAsync(
            List<Guid> cardIds,
            decimal amount,
            string merchant,
            string category,
            Dictionary<Guid, decimal>? currentSpends = null);
    }
}
