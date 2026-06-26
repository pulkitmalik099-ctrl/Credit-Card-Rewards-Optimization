using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;
using CreditCardRewards.Api.Models;
using CreditCardRewards.Core.Dtos;
using CreditCardRewards.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using CreditCardRewards.Data.Context;

namespace CreditCardRewards.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class RecommendationsController : ControllerBase
    {
        private readonly ITransactionRecommendationService _recommendationService;
        private readonly ISpendTrackingService _spendTrackingService;
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<RecommendationsController> _logger;

        public RecommendationsController(
            ITransactionRecommendationService recommendationService,
            ISpendTrackingService spendTrackingService,
            AppDbContext context,
            IConfiguration config,
            ILogger<RecommendationsController> logger)
        {
            _recommendationService = recommendationService;
            _spendTrackingService = spendTrackingService;
            _context = context;
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Rank all saved portfolio cards for a planned expense
        /// </summary>
        [HttpPost("portfolio")]
        public async Task<ActionResult<List<RecommendationResult>>> RankPortfolioCards(
            [FromBody] PortfolioRecommendationRequest request)
        {
            try
            {
                var portfolio = await _spendTrackingService.GetPortfolioSpendSummaryAsync(request.UserProfileId);

                if (!portfolio.Any())
                {
                    return BadRequest("Add your credit cards first before checking recommendations.");
                }

                var recommendationRequest = new TransactionRecommendationRequest
                {
                    Amount = request.Amount,
                    Merchant = request.Merchant,
                    Category = request.Category,
                    EnableMilestoneMode = request.EnableMilestoneMode,
                    CardSpends = portfolio.Select(card => new CardSpendEntry
                    {
                        CardId = card.CardId,
                        CurrentSpend = card.CurrentAnnualSpend
                    }).ToList()
                };

                var recommendations = await _recommendationService.GetRecommendationsAsync(recommendationRequest);
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting portfolio recommendations");
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get ranked card recommendations for a transaction
        /// </summary>
        [HttpPost("rank")]
        public async Task<ActionResult<List<RecommendationResult>>> RankCardsForTransaction(
            [FromBody] TransactionRecommendationRequest request)
        {
            try
            {
                _logger.LogInformation(
                    "Getting recommendations for transaction: Amount={Amount}, Merchant={Merchant}, Category={Category}",
                    request.Amount, request.Merchant, request.Category);

                var recommendations = await _recommendationService.GetRecommendationsAsync(request);

                if (!recommendations.Any())
                {
                    _logger.LogWarning("No recommendations found for the given transaction");
                    return BadRequest("No cards available for recommendation");
                }

                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommendations");
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Natural-language AI portfolio advice
        /// </summary>
        [HttpPost("advice")]
        public async Task<IActionResult> GetPortfolioAdvice([FromBody] AdviceRequest request)
        {
            var apiKey = _config["OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return StatusCode(503, "AI advice requires an OpenAI API key.");

            var cards = await _context.CreditCards
                .Include(c => c.AcceleratedCategories)
                .Include(c => c.Milestones)
                .Include(c => c.RewardCaps)
                .Where(c => c.UserProfileId == request.UserProfileId)
                .ToListAsync();

            if (!cards.Any())
                return BadRequest("Add cards to your portfolio first.");

            var portfolioSummary = string.Join("\n", cards.Select(c =>
                $"- {c.Name} ({c.Issuer}): {c.BaseRewardRate} pts/₹100, point value ₹{c.BaseRewardPointValue}, " +
                $"annual fee ₹{c.AnnualFee}, accumulated spend ₹{c.AccumulatedSpend}" +
                (c.AcceleratedCategories.Any()
                    ? ", accelerated: " + string.Join(", ", c.AcceleratedCategories.Select(a => $"{a.Category} {a.RewardMultiplier}x"))
                    : "")));

            var prompt = $$"""
                You are a credit card rewards optimization expert for Indian consumers.
                The user has the following credit card portfolio:
                {{portfolioSummary}}

                User question: {{request.Question}}

                Give a concise, actionable answer (3-5 sentences max). Reference specific cards by name.
                Focus on maximizing reward value in INR. Be direct and practical.
                """;

            try
            {
                var client = new ChatClient("gpt-4o", apiKey);
                var completion = await client.CompleteChatAsync(new List<ChatMessage> { new UserChatMessage(prompt) });
                return Ok(new { advice = completion.Value.Content[0].Text });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI advice failed");
                return StatusCode(500, "AI advice unavailable. Try again shortly.");
            }
        }

        /// <summary>
        /// Get the best card for a transaction
        /// </summary>
        [HttpPost("best")]
        public async Task<ActionResult<RecommendationResult>> GetBestCard(
            [FromBody] TransactionRecommendationRequest request)
        {
            try
            {
                _logger.LogInformation(
                    "Getting best card for transaction: Amount={Amount}, Merchant={Merchant}",
                    request.Amount, request.Merchant);

                var recommendation = await _recommendationService.GetBestCardAsync(request);
                return Ok(recommendation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting best card recommendation");
                return BadRequest(ex.Message);
            }
        }
    }

    public record AdviceRequest(Guid UserProfileId, string Question);
}
