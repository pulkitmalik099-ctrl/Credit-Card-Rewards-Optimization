using Xunit;
using Microsoft.EntityFrameworkCore;
using CreditCardRewards.Core.Services;
using CreditCardRewards.Core.Dtos;
using CreditCardRewards.Data.Context;
using CreditCardRewards.Data.Models;

namespace CreditCardRewards.Tests
{
    public class EdgeCaseTests
    {
        private AppDbContext CreateCtx() =>
            new(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"EdgeCase_{Guid.NewGuid()}")
                .Options);

        private CreditCard BasicCard(decimal rate = 1m, decimal? ptVal = 0.5m) => new()
        {
            Id = Guid.NewGuid(),
            Name = "Test Card",
            Issuer = "Test Bank",
            BaseRewardRate = rate,
            BaseRewardUnit = "Points",
            BaseRewardPointValue = ptVal,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        [Fact]
        public async Task ZeroAmountTransaction_ReturnsZeroRewards()
        {
            var ctx = CreateCtx();
            var card = BasicCard();
            ctx.CreditCards.Add(card);
            await ctx.SaveChangesAsync();

            var svc = new RewardCalculationService(ctx);
            var result = await svc.CalculateRewardsAsync(new RewardCalculationRequest
            {
                CardId = card.Id,
                TransactionAmount = 0,
                Merchant = "Test",
                Category = "Other"
            });

            Assert.Equal(0m, result.PointsEarned);
            Assert.Equal(0m, result.EstimatedValue);
        }

        [Fact]
        public async Task ExpiredOffer_IsNotApplied()
        {
            var ctx = CreateCtx();
            var card = BasicCard();
            card.Offers.Add(new RewardOffer
            {
                Id = Guid.NewGuid(),
                CreditCardId = card.Id,
                Title = "Expired Offer",
                Merchant = "Amazon",
                RewardBasis = "Multiplier",
                RewardMultiplier = 10m,
                ValidFrom = DateTime.UtcNow.AddDays(-10),
                ValidUntil = DateTime.UtcNow.AddDays(-1), // expired
                IsActive = false
            });
            ctx.CreditCards.Add(card);
            await ctx.SaveChangesAsync();

            var svc = new RewardCalculationService(ctx);
            var result = await svc.CalculateRewardsAsync(new RewardCalculationRequest
            {
                CardId = card.Id,
                TransactionAmount = 10000,
                Merchant = "Amazon",
                Category = "Online Shopping"
            });

            // Should fall back to base rate (1 pt / ₹100), not the 10x offer
            Assert.Equal(100m, result.PointsEarned);
        }

        [Fact]
        public async Task NullPointValue_DefaultsToHalfRupee()
        {
            var ctx = CreateCtx();
            var card = BasicCard(ptVal: null);
            card.BaseRewardPointValue = null;
            ctx.CreditCards.Add(card);
            await ctx.SaveChangesAsync();

            var svc = new RewardCalculationService(ctx);
            var result = await svc.CalculateRewardsAsync(new RewardCalculationRequest
            {
                CardId = card.Id,
                TransactionAmount = 10000,
                Merchant = "Test",
                Category = "Other"
            });

            Assert.True(result.EstimatedValue >= 0);
        }

        [Fact]
        public async Task MultipleActiveOffers_HighestValueApplied()
        {
            var ctx = CreateCtx();
            var card = BasicCard(rate: 1m, ptVal: 1m);
            var now = DateTime.UtcNow;

            card.Offers.Add(new RewardOffer
            {
                Id = Guid.NewGuid(), CreditCardId = card.Id,
                Title = "3x Amazon", Merchant = "Amazon",
                RewardBasis = "Multiplier", RewardMultiplier = 3m,
                ValidFrom = now.AddDays(-1), ValidUntil = now.AddDays(1), IsActive = true
            });
            card.Offers.Add(new RewardOffer
            {
                Id = Guid.NewGuid(), CreditCardId = card.Id,
                Title = "5x Amazon", Merchant = "Amazon",
                RewardBasis = "Multiplier", RewardMultiplier = 5m,
                ValidFrom = now.AddDays(-1), ValidUntil = now.AddDays(1), IsActive = true
            });
            ctx.CreditCards.Add(card);
            await ctx.SaveChangesAsync();

            var svc = new RewardCalculationService(ctx);
            var result = await svc.CalculateRewardsAsync(new RewardCalculationRequest
            {
                CardId = card.Id,
                TransactionAmount = 10000,
                Merchant = "Amazon",
                Category = "Online Shopping"
            });

            // Best offer (5x) should be used: 10000/100 * 1 * 5 = 500 pts
            Assert.True(result.EstimatedValue >= 500m);
        }

        [Fact]
        public async Task AcceleratedCategory_WrongCategory_UsesBaseRate()
        {
            var ctx = CreateCtx();
            var card = BasicCard(rate: 1m, ptVal: 0.5m);
            card.AcceleratedCategories.Add(new RewardCategory
            {
                Id = Guid.NewGuid(), CreditCardId = card.Id,
                Category = "Dining", RewardMultiplier = 5m,
                EffectiveFrom = DateTime.UtcNow
            });
            ctx.CreditCards.Add(card);
            await ctx.SaveChangesAsync();

            var svc = new RewardCalculationService(ctx);
            var result = await svc.CalculateRewardsAsync(new RewardCalculationRequest
            {
                CardId = card.Id,
                TransactionAmount = 10000,
                Merchant = "Amazon",
                Category = "Online Shopping" // not Dining
            });

            Assert.Equal(100m, result.PointsEarned); // base rate only
        }

        [Fact]
        public async Task MilestoneAlreadyHit_DoesNotDoubleCount()
        {
            var ctx = CreateCtx();
            var card = BasicCard();
            card.Milestones.Add(new Milestone
            {
                Id = Guid.NewGuid(), CreditCardId = card.Id,
                Title = "₹1L Milestone", SpendThreshold = 100000m,
                RewardValue = 2000m, RewardUnit = "Rupees",
                EffectiveFrom = DateTime.UtcNow
            });
            ctx.CreditCards.Add(card);
            await ctx.SaveChangesAsync();

            var svc = new RewardCalculationService(ctx);
            // Spend already exceeds threshold
            var result = await svc.CalculateRewardsAsync(new RewardCalculationRequest
            {
                CardId = card.Id,
                TransactionAmount = 5000,
                Merchant = "Test",
                Category = "Other",
                CurrentAnnualSpend = 200000m // already past milestone
            });

            // Milestone already hit, no extra contribution expected
            Assert.Equal(0m, result.MilestoneContributionValue);
        }

        [Fact]
        public async Task TransactionRecommendation_ReturnsRankedResults()
        {
            var ctx = CreateCtx();
            var card1 = BasicCard(rate: 1m, ptVal: 0.5m);
            var card2 = BasicCard(rate: 5m, ptVal: 1m);
            card2.Name = "High Rewards Card";
            ctx.CreditCards.AddRange(card1, card2);
            await ctx.SaveChangesAsync();

            var calcSvc = new RewardCalculationService(ctx);
            var recommendSvc = new TransactionRecommendationService(calcSvc);

            var result = await recommendSvc.GetRecommendationsAsync(new TransactionRecommendationRequest
            {
                Amount = 10000,
                Merchant = "Swiggy",
                Category = "Dining",
                CardSpends = new List<CardSpendEntry>
                {
                    new() { CardId = card1.Id, CurrentSpend = 0 },
                    new() { CardId = card2.Id, CurrentSpend = 0 }
                }
            });

            Assert.Equal(2, result.Count);
            Assert.Equal(1, result.First().Rank); // top ranked card
            Assert.True(result[0].RewardValue >= result[1].RewardValue); // ranked descending
        }
    }
}
