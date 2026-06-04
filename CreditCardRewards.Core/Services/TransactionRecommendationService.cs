using CreditCardRewards.Core.Dtos;
using CreditCardRewards.Core.Interfaces;

namespace CreditCardRewards.Core.Services
{
    /// <summary>
    /// Recommends the best credit card for a transaction
    /// </summary>
    public class TransactionRecommendationService : ITransactionRecommendationService
    {
        private readonly IRewardCalculationService _rewardCalculationService;

        public TransactionRecommendationService(IRewardCalculationService rewardCalculationService)
        {
            _rewardCalculationService = rewardCalculationService;
        }

        /// <summary>
        /// Get ranked card recommendations for a transaction
        /// </summary>
        public async Task<List<RecommendationResult>> GetRecommendationsAsync(
            TransactionRecommendationRequest request)
        {
            if (!request.CardSpends.Any())
                throw new InvalidOperationException("No cards provided for recommendation");

            var cardIds = request.CardSpends.Select(cs => cs.CardId).ToList();
            var spendDict = request.CardSpends.ToDictionary(cs => cs.CardId, cs => cs.CurrentSpend);

            // Calculate rewards for all cards
            var calculations = await _rewardCalculationService.CalculateRewardsForAllCardsAsync(
                cardIds,
                request.Amount,
                request.Merchant,
                request.Category,
                spendDict);

            // Sort by reward value (descending)
            var ranked = calculations
                .OrderByDescending(c => c.EstimatedValue)
                .Select((calc, index) => new RecommendationResult
                {
                    Rank = index + 1,
                    CardId = calc.CardId,
                    CardName = calc.CardName,
                    RewardValue = calc.EstimatedValue,
                    EffectiveReturnPercentage = calc.EffectiveReturnPercentage,
                    Reasoning = calc.Reasoning,
                    Details = calc
                })
                .ToList();

            return ranked;
        }

        /// <summary>
        /// Get the best card for a transaction
        /// </summary>
        public async Task<RecommendationResult> GetBestCardAsync(TransactionRecommendationRequest request)
        {
            var recommendations = await GetRecommendationsAsync(request);
            return recommendations.FirstOrDefault() ?? throw new InvalidOperationException("No recommendations available");
        }
    }
}
