using Microsoft.EntityFrameworkCore;
using CreditCardRewards.Core.Dtos;
using CreditCardRewards.Core.Interfaces;
using CreditCardRewards.Data.Context;
using CreditCardRewards.Data.Models;

namespace CreditCardRewards.Core.Services
{
    /// <summary>
    /// Tracks spending and milestone progress
    /// </summary>
    public class SpendTrackingService : ISpendTrackingService
    {
        private readonly AppDbContext _context;
        private readonly IRewardCalculationService _rewardCalculationService;

        public SpendTrackingService(AppDbContext context, IRewardCalculationService rewardCalculationService)
        {
            _context = context;
            _rewardCalculationService = rewardCalculationService;
        }

        /// <summary>
        /// Get spend summary for a card
        /// </summary>
        public async Task<CardSpendSummary> GetCardSpendSummaryAsync(Guid cardId, int? year = null, int? month = null)
        {
            var card = await _context.CreditCards
                .Include(c => c.Milestones)
                .Include(c => c.Transactions)
                .FirstOrDefaultAsync(c => c.Id == cardId);

            if (card == null)
                throw new InvalidOperationException($"Card with ID {cardId} not found");

            year ??= DateTime.Now.Year;
            month ??= DateTime.Now.Month;

            var transactions = card.Transactions
                .Where(t => t.TransactionDate.Year == year);

            if (month.HasValue)
                transactions = transactions.Where(t => t.TransactionDate.Month == month.Value);

            var monthlySpend = transactions
                .Where(t => t.TransactionDate.Month == DateTime.Now.Month && t.TransactionDate.Year == DateTime.Now.Year)
                .Sum(t => t.Amount);

            var annualSpend = card.Transactions
                .Where(t => t.TransactionDate.Year == year)
                .Sum(t => t.Amount);

            var remainingToWaiver = Math.Max(0, card.AnnualFeeWaiverSpendThreshold - annualSpend);
            var feeWaiverAchieved = remainingToWaiver == 0;

            // Calculate milestone progress
            var milestoneProgress = card.Milestones
                .Where(m => m.EffectiveFrom <= DateTime.UtcNow && 
                           (m.EffectiveUntil == null || m.EffectiveUntil >= DateTime.UtcNow))
                .OrderBy(m => m.SpendThreshold)
                .Select(m =>
                {
                    var isAchieved = annualSpend >= m.SpendThreshold;
                    var remaining = Math.Max(0, m.SpendThreshold - annualSpend);

                    return new MilestoneProgress
                    {
                        MilestoneId = m.Id,
                        Title = m.Title,
                        SpendThreshold = m.SpendThreshold,
                        RewardValue = m.RewardValue,
                        CurrentSpend = annualSpend,
                        RemainingSpend = remaining,
                        IsAchieved = isAchieved
                    };
                })
                .ToList();

            var totalRewardValue = card.Transactions
                .Where(t => t.TransactionDate.Year == year)
                .Sum(t => t.RewardValueInRupees);

            return new CardSpendSummary
            {
                CardId = card.Id,
                CardName = card.Name,
                CurrentMonthlySpend = monthlySpend,
                CurrentAnnualSpend = annualSpend,
                AnnualFeeWaiverThreshold = card.AnnualFeeWaiverSpendThreshold,
                AnnualFeeWaiverProgress = annualSpend,
                AnnualFeeWaiverRemaining = remainingToWaiver,
                IsFeeWaiverAchieved = feeWaiverAchieved,
                MilestoneProgress = milestoneProgress,
                TotalRewardValueEarned = totalRewardValue
            };
        }

        /// <summary>
        /// Get spend summary for all cards
        /// </summary>
        public async Task<List<CardSpendSummary>> GetPortfolioSpendSummaryAsync(int? year = null)
        {
            var cards = await _context.CreditCards.ToListAsync();
            var summaries = new List<CardSpendSummary>();

            foreach (var card in cards)
            {
                var summary = await GetCardSpendSummaryAsync(card.Id, year ?? DateTime.Now.Year);
                summaries.Add(summary);
            }

            return summaries;
        }

        /// <summary>
        /// Add a transaction and update spend tracking
        /// </summary>
        public async Task AddTransactionAndUpdateSpendAsync(
            Guid cardId, decimal amount, string merchant, string category)
        {
            var card = await _context.CreditCards
                .Include(c => c.Transactions)
                .FirstOrDefaultAsync(c => c.Id == cardId);

            if (card == null)
                throw new InvalidOperationException($"Card with ID {cardId} not found");

            // Calculate rewards for this transaction
            var rewardRequest = new Dtos.RewardCalculationRequest
            {
                CardId = cardId,
                TransactionAmount = amount,
                Merchant = merchant,
                Category = category,
                CurrentAnnualSpend = card.Transactions
                    .Where(t => t.TransactionDate.Year == DateTime.Now.Year)
                    .Sum(t => t.Amount)
            };

            var rewardResult = await _rewardCalculationService.CalculateRewardsAsync(rewardRequest);

            // Create transaction entity
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                CreditCardId = cardId,
                TransactionDate = DateTime.UtcNow,
                Amount = amount,
                Merchant = merchant,
                Category = category,
                PointsEarned = rewardResult.PointsEarned,
                CashbackEarned = rewardResult.CashbackEarned,
                RewardValueInRupees = rewardResult.EstimatedValue,
                EffectiveReturnPercentage = rewardResult.EffectiveReturnPercentage,
                ContributedToMilestone = rewardResult.MilestoneContributed,
                ContributedToFeeWaiver = rewardResult.FeeWaiverContributed,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Get milestone progress for a card
        /// </summary>
        public async Task<List<MilestoneProgress>> GetMilestoneProgressAsync(Guid cardId)
        {
            var card = await _context.CreditCards
                .Include(c => c.Milestones)
                .Include(c => c.Transactions)
                .FirstOrDefaultAsync(c => c.Id == cardId);

            if (card == null)
                throw new InvalidOperationException($"Card with ID {cardId} not found");

            var annualSpend = card.Transactions
                .Where(t => t.TransactionDate.Year == DateTime.Now.Year)
                .Sum(t => t.Amount);

            var progress = card.Milestones
                .Where(m => m.EffectiveFrom <= DateTime.UtcNow && 
                           (m.EffectiveUntil == null || m.EffectiveUntil >= DateTime.UtcNow))
                .OrderBy(m => m.SpendThreshold)
                .Select(m =>
                {
                    var isAchieved = annualSpend >= m.SpendThreshold;
                    var remaining = Math.Max(0, m.SpendThreshold - annualSpend);

                    return new MilestoneProgress
                    {
                        MilestoneId = m.Id,
                        Title = m.Title,
                        SpendThreshold = m.SpendThreshold,
                        RewardValue = m.RewardValue,
                        CurrentSpend = annualSpend,
                        RemainingSpend = remaining,
                        IsAchieved = isAchieved
                    };
                })
                .ToList();

            return progress;
        }
    }
}
