using CreditCardRewards.Core.Dtos;

namespace CreditCardRewards.Core.Interfaces
{
    /// <summary>
    /// Interface for spend tracking and milestone management
    /// </summary>
    public interface ISpendTrackingService
    {
        /// <summary>
        /// Get spend summary for a card
        /// </summary>
        Task<CardSpendSummary> GetCardSpendSummaryAsync(Guid cardId, int? year = null, int? month = null);

        /// <summary>
        /// Get spend summary for all cards
        /// </summary>
        Task<List<CardSpendSummary>> GetPortfolioSpendSummaryAsync(Guid userProfileId, int? year = null);

        /// <summary>
        /// Add a transaction and update spend tracking
        /// </summary>
        Task AddTransactionAndUpdateSpendAsync(Guid cardId, decimal amount, string merchant, string category);

        /// <summary>
        /// Get milestone progress for a card
        /// </summary>
        Task<List<MilestoneProgress>> GetMilestoneProgressAsync(Guid cardId);
    }
}
