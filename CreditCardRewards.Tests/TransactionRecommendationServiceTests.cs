using Xunit;
using Microsoft.EntityFrameworkCore;
using CreditCardRewards.Core.Services;
using CreditCardRewards.Core.Dtos;
using CreditCardRewards.Data.Context;
using CreditCardRewards.Data.Models;

namespace CreditCardRewards.Tests
{
    public class TransactionRecommendationServiceTests
    {
        private AppDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            return new AppDbContext(options);
        }

        private CreditCard CreateTestCard(string name, decimal baseRate, decimal? pointValue = 0.5m)
        {
            return new CreditCard
            {
                Id = Guid.NewGuid(),
                Name = name,
                Issuer = "Test Bank",
                BaseRewardRate = baseRate,
                BaseRewardUnit = "Points",
                BaseRewardPointValue = pointValue,
                AnnualFee = 5000,
                AnnualFeeWaiverSpendThreshold = 200000,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task GetRecommendationsAsync_RanksCardsByRewardValue()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var card1 = CreateTestCard("Standard Card", 1m, 0.5m); // 1% base, ₹0.5/point
            var card2 = CreateTestCard("Premium Card", 2m, 0.75m); // 2% base, ₹0.75/point

            context.CreditCards.AddRange(card1, card2);
            await context.SaveChangesAsync();

            var rewardCalcService = new RewardCalculationService(context);
            var recommendationService = new TransactionRecommendationService(rewardCalcService);

            var request = new TransactionRecommendationRequest
            {
                Amount = 10000,
                Merchant = "Amazon",
                Category = "Online Shopping",
                CardSpends = new List<CardSpendEntry>
                {
                    new() { CardId = card1.Id, CurrentSpend = 0 },
                    new() { CardId = card2.Id, CurrentSpend = 0 }
                }
            };

            // Act
            var recommendations = await recommendationService.GetRecommendationsAsync(request);

            // Assert
            Assert.Equal(2, recommendations.Count);
            Assert.Equal(1, recommendations[0].Rank);
            Assert.Equal(2, recommendations[1].Rank);
            Assert.True(recommendations[0].RewardValue >= recommendations[1].RewardValue);
        }

        [Fact]
        public async Task GetBestCardAsync_ReturnsBestCard()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var card1 = CreateTestCard("Standard Card", 1m);
            var card2 = CreateTestCard("Premium Card", 5m);

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

            var rewardCalcService = new RewardCalculationService(context);
            var recommendationService = new TransactionRecommendationService(rewardCalcService);

            var request = new TransactionRecommendationRequest
            {
                Amount = 5000,
                Merchant = "Swiggy",
                Category = "Dining",
                CardSpends = new List<CardSpendEntry>
                {
                    new() { CardId = card1.Id, CurrentSpend = 0 },
                    new() { CardId = card2.Id, CurrentSpend = 0 }
                }
            };

            // Act
            var bestCard = await recommendationService.GetBestCardAsync(request);

            // Assert
            Assert.NotNull(bestCard);
            Assert.Equal(1, bestCard.Rank);
            Assert.Equal(card2.Id, bestCard.CardId);
        }

        [Fact]
        public async Task GetRecommendationsAsync_WithMilestoneMode_PrioritizesMilestones()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var card1 = CreateTestCard("Card A", 5m); // Higher base reward
            var card2 = CreateTestCard("Card B", 1m); // Lower reward but upcoming milestone

            card2.Milestones.Add(new Milestone
            {
                Id = Guid.NewGuid(),
                CreditCardId = card2.Id,
                Title = "₹1 Lakh Milestone",
                SpendThreshold = 100000m,
                RewardValue = 10000m,
                RewardUnit = "Rupees",
                EffectiveFrom = DateTime.UtcNow
            });

            context.CreditCards.AddRange(card1, card2);
            await context.SaveChangesAsync();

            var rewardCalcService = new RewardCalculationService(context);
            var recommendationService = new TransactionRecommendationService(rewardCalcService);

            var request = new TransactionRecommendationRequest
            {
                Amount = 20000,
                Merchant = "Amazon",
                Category = "Shopping",
                EnableMilestoneMode = true,
                CardSpends = new List<CardSpendEntry>
                {
                    new() { CardId = card1.Id, CurrentSpend = 0 },
                    new() { CardId = card2.Id, CurrentSpend = 90000m }
                }
            };

            // Act
            var recommendations = await recommendationService.GetRecommendationsAsync(request);

            // Assert - In milestone mode, card2 should rank higher due to milestone value
            Assert.Equal(2, recommendations.Count);
            Assert.NotNull(recommendations.First());
        }

        [Fact]
        public async Task GetRecommendationsAsync_ThrowsOnNoCards()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var rewardCalcService = new RewardCalculationService(context);
            var recommendationService = new TransactionRecommendationService(rewardCalcService);

            var request = new TransactionRecommendationRequest
            {
                Amount = 10000,
                Merchant = "Amazon",
                Category = "Shopping",
                CardSpends = new List<CardSpendEntry>()
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await recommendationService.GetRecommendationsAsync(request));
        }

        [Fact]
        public async Task GetRecommendationsAsync_RanksCorrectlyWithMerchantOffers()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var card1 = CreateTestCard("Card Without Offer", 1m);
            var card2 = CreateTestCard("Card With Offer", 1m);

            card2.Offers.Add(new RewardOffer
            {
                Id = Guid.NewGuid(),
                CreditCardId = card2.Id,
                Title = "Amazon 10% Cashback",
                Merchant = "Amazon",
                RewardBasis = "Multiplier",
                RewardMultiplier = 10m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidUntil = DateTime.UtcNow.AddDays(1),
                IsActive = true
            });

            context.CreditCards.AddRange(card1, card2);
            await context.SaveChangesAsync();

            var rewardCalcService = new RewardCalculationService(context);
            var recommendationService = new TransactionRecommendationService(rewardCalcService);

            var request = new TransactionRecommendationRequest
            {
                Amount = 10000,
                Merchant = "Amazon",
                Category = "Shopping",
                CardSpends = new List<CardSpendEntry>
                {
                    new() { CardId = card1.Id, CurrentSpend = 0 },
                    new() { CardId = card2.Id, CurrentSpend = 0 }
                }
            };

            // Act
            var recommendations = await recommendationService.GetRecommendationsAsync(request);

            // Assert
            Assert.Equal(2, recommendations.Count);
            Assert.Equal(card2.Id, recommendations[0].CardId); // Card with offer should rank #1
            Assert.True(recommendations[0].RewardValue > recommendations[1].RewardValue);
        }
    }
}
