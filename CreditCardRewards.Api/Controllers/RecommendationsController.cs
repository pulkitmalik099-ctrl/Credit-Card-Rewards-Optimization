using Microsoft.AspNetCore.Mvc;
using CreditCardRewards.Api.Models;
using CreditCardRewards.Core.Dtos;
using CreditCardRewards.Core.Interfaces;

namespace CreditCardRewards.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecommendationsController : ControllerBase
    {
        private readonly ITransactionRecommendationService _recommendationService;
        private readonly ISpendTrackingService _spendTrackingService;
        private readonly ILogger<RecommendationsController> _logger;

        public RecommendationsController(
            ITransactionRecommendationService recommendationService,
            ISpendTrackingService spendTrackingService,
            ILogger<RecommendationsController> logger)
        {
            _recommendationService = recommendationService;
            _spendTrackingService = spendTrackingService;
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
                var portfolio = await _spendTrackingService.GetPortfolioSpendSummaryAsync();

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
}
