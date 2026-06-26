using Microsoft.EntityFrameworkCore;
using CreditCardRewards.Core.Dtos;
using CreditCardRewards.Core.Interfaces;
using CreditCardRewards.Data.Context;
using CreditCardRewards.Data.Models;

namespace CreditCardRewards.Core.Services
{
    /// <summary>
    /// Calculates rewards for transactions based on card configuration
    /// </summary>
    public class RewardCalculationService : IRewardCalculationService
    {
        private readonly AppDbContext _context;

        public RewardCalculationService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Calculate rewards for a transaction on a specific card
        /// </summary>
        public async Task<RewardCalculationResult> CalculateRewardsAsync(RewardCalculationRequest request)
        {
            var card = await _context.CreditCards
                .Include(c => c.AcceleratedCategories)
                .Include(c => c.RewardCaps)
                .Include(c => c.Milestones)
                .Include(c => c.Offers.Where(o => o.IsActive))
                .FirstOrDefaultAsync(c => c.Id == request.CardId);

            if (card == null)
                throw new InvalidOperationException($"Card with ID {request.CardId} not found");

            var result = new RewardCalculationResult
            {
                CardId = card.Id,
                CardName = card.Name,
                TransactionAmount = request.TransactionAmount
            };

            // Decision Priority Order Implementation
            decimal rewardPoints = 0;
            decimal rewardValueInRupees = 0;
            string reasoning = "";

            // 1. Check for merchant-specific offers (highest priority) — pick best value among active offers
            var merchantOffer = card.Offers
                .Where(o =>
                    !string.IsNullOrEmpty(o.Merchant) &&
                    o.Merchant.Equals(request.Merchant, StringComparison.OrdinalIgnoreCase) &&
                    o.ValidFrom <= DateTime.UtcNow && o.ValidUntil >= DateTime.UtcNow)
                .OrderByDescending(o => CalculateOfferReward(o, request.TransactionAmount))
                .FirstOrDefault();

            if (merchantOffer != null)
            {
                rewardValueInRupees = CalculateOfferReward(merchantOffer, request.TransactionAmount);
                reasoning = $"Merchant-specific offer: {merchantOffer.Title}";
            }
            // 2. Check for accelerated reward category
            else
            {
                var acceleratedCategory = card.AcceleratedCategories.FirstOrDefault(ac =>
                    ac.Category.Equals(request.Category, StringComparison.OrdinalIgnoreCase));

                if (acceleratedCategory != null)
                {
                    // Apply accelerated category rate
                    rewardPoints = (request.TransactionAmount / 100) * card.BaseRewardRate * acceleratedCategory.RewardMultiplier;
                    reasoning = $"Accelerated {request.Category} category: {acceleratedCategory.RewardMultiplier}x multiplier";
                }
                else
                {
                    // 7. Apply base reward rate (lowest priority)
                    rewardPoints = (request.TransactionAmount / 100) * card.BaseRewardRate;
                    reasoning = "Base reward rate";
                }
            }

            // Convert points to rupee value
            if (rewardPoints > 0 && card.BaseRewardPointValue.HasValue)
            {
                rewardValueInRupees = rewardPoints * card.BaseRewardPointValue.Value;
                result.PointsEarned = rewardPoints;
                result.CashbackEarned = rewardValueInRupees;
            }

            // 3. Apply reward caps (don't exceed caps)
            var monthlyCap = card.RewardCaps.FirstOrDefault(rc =>
                rc.CapType == "Monthly" &&
                (rc.Category == "All" || rc.Category == request.Category));

            if (monthlyCap != null)
            {
                if (monthlyCap.Unit.Equals("Points", StringComparison.OrdinalIgnoreCase) &&
                    rewardPoints > monthlyCap.MaxRewardValue)
                {
                    rewardPoints = monthlyCap.MaxRewardValue;
                    rewardValueInRupees = card.BaseRewardPointValue.HasValue
                        ? rewardPoints * card.BaseRewardPointValue.Value
                        : rewardValueInRupees;
                    result.PointsEarned = rewardPoints;
                    result.CashbackEarned = rewardValueInRupees;
                    reasoning += $"; Capped to {rewardPoints} points";
                }
                else if (!monthlyCap.Unit.Equals("Points", StringComparison.OrdinalIgnoreCase) &&
                         rewardValueInRupees > monthlyCap.MaxRewardValue)
                {
                    rewardValueInRupees = monthlyCap.MaxRewardValue;
                    result.CashbackEarned = rewardValueInRupees;
                    reasoning += $"; Capped to INR {rewardValueInRupees}";
                }
            }

            // 5. Calculate milestone contribution value
            var (milestoneValue, milestoneReached) = CalculateMilestoneContribution(card, request);
            if (milestoneValue > 0)
            {
                result.MilestoneContributed = true;
                result.MilestoneContributionValue = milestoneValue;
                reasoning += $"; Progressing toward milestone (₹{milestoneValue} potential)";
            }

            // 6. Calculate fee waiver contribution
            var feeWaiverValue = CalculateFeeWaiverContribution(card, request);
            if (feeWaiverValue > 0)
            {
                result.FeeWaiverContributed = true;
                result.FeeWaiverContributionValue = feeWaiverValue;
                reasoning += $"; Contributing to fee waiver (₹{feeWaiverValue} potential)";
            }

            // Set result values
            result.EstimatedValue = rewardValueInRupees + result.MilestoneContributionValue + result.FeeWaiverContributionValue;
            result.EffectiveReturnPercentage = request.TransactionAmount > 0
                ? (result.EstimatedValue / request.TransactionAmount) * 100
                : 0;
            result.Reasoning = reasoning;

            return result;
        }

        /// <summary>
        /// Calculate rewards for a transaction on all cards
        /// </summary>
        public async Task<List<RewardCalculationResult>> CalculateRewardsForAllCardsAsync(
            List<Guid> cardIds,
            decimal amount,
            string merchant,
            string category,
            Dictionary<Guid, decimal>? currentSpends = null)
        {
            var results = new List<RewardCalculationResult>();

            foreach (var cardId in cardIds)
            {
                var currentSpend = currentSpends != null && currentSpends.TryGetValue(cardId, out var spend)
                    ? spend
                    : 0;

                var request = new RewardCalculationRequest
                {
                    CardId = cardId,
                    TransactionAmount = amount,
                    Merchant = merchant,
                    Category = category,
                    CurrentAnnualSpend = currentSpend,
                    CurrentAnnualFeeWaiverProgress = currentSpend
                };

                var result = await CalculateRewardsAsync(request);
                results.Add(result);
            }

            return results;
        }

        private decimal CalculateOfferReward(RewardOffer offer, decimal amount)
        {
            if (offer.RewardBasis == "Multiplier")
            {
                var reward = (amount / 100) * offer.RewardMultiplier;
                if (offer.MaxRewardPerTransaction.HasValue)
                    reward = Math.Min(reward, offer.MaxRewardPerTransaction.Value);
                return reward;
            }
            else if (offer.RewardBasis == "FlatBonus" && offer.FlatBonusAmount.HasValue)
            {
                return offer.FlatBonusAmount.Value;
            }

            return 0;
        }

        private (decimal value, bool milestoneReached) CalculateMilestoneContribution(
            CreditCard card, RewardCalculationRequest request)
        {
            // Find the next milestone not yet achieved
            var nextMilestone = card.Milestones
                .Where(m => m.EffectiveFrom <= DateTime.UtcNow &&
                           (m.EffectiveUntil == null || m.EffectiveUntil >= DateTime.UtcNow))
                .OrderBy(m => m.SpendThreshold)
                .FirstOrDefault(m => m.SpendThreshold > request.CurrentAnnualSpend);

            if (nextMilestone == null)
                return (0, false);

            var remainingToMilestone = nextMilestone.SpendThreshold - request.CurrentAnnualSpend;
            var newSpend = request.CurrentAnnualSpend + request.TransactionAmount;

            // If this transaction reaches the milestone
            if (newSpend >= nextMilestone.SpendThreshold)
            {
                return (nextMilestone.RewardValue, true);
            }

            // Proportional contribution to next milestone
            var progressPercentage = request.TransactionAmount / remainingToMilestone;
            var proportionalValue = nextMilestone.RewardValue * Math.Min(progressPercentage, 1.0m);

            return (proportionalValue, false);
        }

        private decimal CalculateFeeWaiverContribution(CreditCard card, RewardCalculationRequest request)
        {
            if (card.AnnualFee <= 0 || card.AnnualFeeWaiverSpendThreshold <= 0)
                return 0;

            var remainingToWaiver = card.AnnualFeeWaiverSpendThreshold - request.CurrentAnnualFeeWaiverProgress;
            if (remainingToWaiver <= 0)
                return 0;

            // If this transaction reaches the fee waiver threshold
            var newSpend = request.CurrentAnnualFeeWaiverProgress + request.TransactionAmount;
            if (newSpend >= card.AnnualFeeWaiverSpendThreshold)
            {
                return card.AnnualFee; // Full fee waiver value
            }

            // Proportional contribution
            var progressPercentage = request.TransactionAmount / remainingToWaiver;
            return card.AnnualFee * Math.Min(progressPercentage, 1.0m);
        }
    }
}
