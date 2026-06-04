using CreditCardRewards.Core.Dtos;

namespace CreditCardRewards.Core.Interfaces
{
    /// <summary>
    /// Interface for transaction recommendation logic
    /// </summary>
    public interface ITransactionRecommendationService
    {
        /// <summary>
        /// Get ranked card recommendations for a transaction
        /// </summary>
        Task<List<RecommendationResult>> GetRecommendationsAsync(TransactionRecommendationRequest request);

        /// <summary>
        /// Get the best card for a transaction
        /// </summary>
        Task<RecommendationResult> GetBestCardAsync(TransactionRecommendationRequest request);
    }
}
