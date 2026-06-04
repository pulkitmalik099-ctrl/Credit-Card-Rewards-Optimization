using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using CreditCardRewards.Core.Services;
using CreditCardRewards.Core.Dtos;
using CreditCardRewards.Data.Context;
using CreditCardRewards.Data.Models;

namespace CreditCardRewards.Tests
{
    public class RewardCalculationServiceTests
    {
        private AppDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            return new AppDbContext(options);
        }

        private CreditCard CreateTestCard(decimal baseRewardRate = 1m, decimal? pointValue = 0.5m)
        {
            return new CreditCard
            {
                Id = Guid.NewGuid(),
                Name = "Test Card",
                Issuer = "Test Bank",
                BaseRewardRate = baseRewardRate,
                BaseRewardUnit = "Points",
                BaseRewardPointValue = pointValue,
                AnnualFee = 5000,
                AnnualFeeWaiverSpendThreshold = 200000,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task CalculateRewardsAsync_WithBaseRate_ReturnsCorrectPoints()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var card = CreateTestCard(baseRewardRate: 1m, pointValue: 0.5m);
            context.CreditCards.Add(card);
            await context.SaveChangesAsync();

            var service = new RewardCalculationService(context);
            var request = new RewardCalculationRequest
            {
                CardId = card.Id,
                TransactionAmount = 10000,
                Merchant = "Amazon",
                Category = "Online Shopping"
            };

            // Act
            var result = await service.CalculateRewardsAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(100m, result.PointsEarned); // 10000 / 100 * 1 = 100 points
            Assert.Equal(50m, result.EstimatedValue); // 100 * 0.5 = ₹50
            Assert.Equal(0.5m, result.EffectiveReturnPercentage); // 50 / 10000 = 0.5%
        }

        [Fact]
        public async Task CalculateRewardsAsync_WithAcceleratedCategory_AppliesMultiplier()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var card = CreateTestCard(baseRewardRate: 1m, pointValue: 0.5m);
            card.AcceleratedCategories.Add(new RewardCategory
            {
                Id = Guid.NewGuid(),
                CreditCardId = card.Id,
                Category = "Dining",
                RewardMultiplier = 5m,
                EffectiveFrom = DateTime.UtcNow
            });

            context.CreditCards.Add(card);
            await context.SaveChangesAsync();

            var service = new RewardCalculationService(context);
            var request = new RewardCalculationRequest
            {
                CardId = card.Id,
                TransactionAmount = 5000,
                Merchant = "Swiggy",
                Category = "Dining"
            };

            // Act
            var result = await service.CalculateRewardsAsync(request);

            // Assert
            Assert.Equal(250m, result.PointsEarned); // 5000 / 100 * 1 * 5 = 250 points
            Assert.Equal(125m, result.EstimatedValue); // 250 * 0.5 = ₹125
            Assert.Equal(2.5m, result.EffectiveReturnPercentage); // 125 / 5000 = 2.5%
        }

        [Fact]
        public async Task CalculateRewardsAsync_WithMerchantOffer_AppliesOfferReward()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var card = CreateTestCard();
            card.Offers.Add(new RewardOffer
            {
                Id = Guid.NewGuid(),
                CreditCardId = card.Id,
                Title = "5% Cashback at Amazon",
                Merchant = "Amazon",
                RewardBasis = "Multiplier",
                RewardMultiplier = 1.05m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidUntil = DateTime.UtcNow.AddDays(1),
                IsActive = true
            });

            context.CreditCards.Add(card);
            await context.SaveChangesAsync();

            var service = new RewardCalculationService(context);
            var request = new RewardCalculationRequest
            {
                CardId = card.Id,
                TransactionAmount = 10000,
                Merchant = "Amazon",
                Category = "Online Shopping"
            };

            // Act
            var result = await service.CalculateRewardsAsync(request);

            // Assert - offer-based reward (merchant offer priority over base)
            Assert.NotNull(result.Reasoning);
            Assert.Contains("Merchant-specific offer", result.Reasoning);
        }

        [Fact]
        public async Task CalculateRewardsAsync_WithRewardCap_DoesNotExceedCap()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var card = CreateTestCard(baseRewardRate: 10m); // Very high rate
            card.RewardCaps.Add(new RewardCap
            {
                Id = Guid.NewGuid(),
                CreditCardId = card.Id,
                CapType = "Monthly",
                Category = "All",
                MaxRewardValue = 500m,
                Unit = "Points",
                EffectiveFrom = DateTime.UtcNow
            });

            context.CreditCards.Add(card);
            await context.SaveChangesAsync();

            var service = new RewardCalculationService(context);
            var request = new RewardCalculationRequest
            {
                CardId = card.Id,
                TransactionAmount = 100000, // Large transaction
                Merchant = "Amazon",
                Category = "Online Shopping"
            };

            // Act
            var result = await service.CalculateRewardsAsync(request);

            // Assert - capped at 500 points
            Assert.True(result.PointsEarned <= 500m);
            Assert.Contains("Capped", result.Reasoning);
        }

        [Fact]
        public async Task CalculateRewardsAsync_WithMilestone_CalculatesMilestoneValue()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var card = CreateTestCard();
            card.Milestones.Add(new Milestone
            {
                Id = Guid.NewGuid(),
                CreditCardId = card.Id,
                Title = "₹3 Lakh Spend",
                SpendThreshold = 300000m,
                RewardValue = 5000m,
                RewardUnit = "Rupees",
                EffectiveFrom = DateTime.UtcNow
            });

            context.CreditCards.Add(card);
            await context.SaveChangesAsync();

            var service = new RewardCalculationService(context);
            var request = new RewardCalculationRequest
            {
                CardId = card.Id,
                TransactionAmount = 50000,
                Merchant = "Amazon",
                Category = "Online Shopping",
                CurrentAnnualSpend = 250000m // ₹50k away from milestone
            };

            // Act
            var result = await service.CalculateRewardsAsync(request);

            // Assert
            Assert.True(result.MilestoneContributed);
            Assert.True(result.MilestoneContributionValue > 0);
        }

        [Fact]
        public async Task CalculateRewardsAsync_WithFeeWaiverProgress_CalculatesFeeWaiverValue()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var card = CreateTestCard();
            card.AnnualFee = 5000m;
            card.AnnualFeeWaiverSpendThreshold = 200000m;

            context.CreditCards.Add(card);
            await context.SaveChangesAsync();

            var service = new RewardCalculationService(context);
            var request = new RewardCalculationRequest
            {
                CardId = card.Id,
                TransactionAmount = 50000,
                Merchant = "Amazon",
                Category = "Online Shopping",
                CurrentAnnualFeeWaiverProgress = 150000m // ₹50k away from fee waiver
            };

            // Act
            var result = await service.CalculateRewardsAsync(request);

            // Assert
            Assert.True(result.FeeWaiverContributed);
            Assert.Equal(5000m, result.FeeWaiverContributionValue); // Should reach fee waiver threshold
        }

        [Fact]
        public async Task CalculateRewardsForAllCardsAsync_RanksMultipleCards()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var card1 = CreateTestCard(baseRewardRate: 1m, pointValue: 0.5m);
            var card2 = CreateTestCard(baseRewardRate: 2m, pointValue: 0.75m);
            card2.Name = "Premium Card";

            card2.AcceleratedCategories.Add(new RewardCategory
            {
                Id = Guid.NewGuid(),
                CreditCardId = card2.Id,
                Category = "Dining",
                RewardMultiplier = 3m,
                EffectiveFrom = DateTime.UtcNow
            });

            context.CreditCards.AddRange(card1, card2);
            await context.SaveChangesAsync();

            var service = new RewardCalculationService(context);

            // Act
            var results = await service.CalculateRewardsForAllCardsAsync(
                new List<Guid> { card1.Id, card2.Id },
                5000,
                "Swiggy",
                "Dining",
                null);

            // Assert
            Assert.Equal(2, results.Count);
            // Card2 should have higher value due to accelerated dining category
            var card2Result = results.First(r => r.CardId == card2.Id);
            var card1Result = results.First(r => r.CardId == card1.Id);
            Assert.True(card2Result.EstimatedValue > card1Result.EstimatedValue);
        }
    }
}
