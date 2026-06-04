using Microsoft.AspNetCore.Mvc;
using CreditCardRewards.Core.Dtos;
using CreditCardRewards.Core.Interfaces;

namespace CreditCardRewards.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecommendationsController : ControllerBase
    {
        private readonly ITransactionRecommendationService _recommendationService;
        private readonly ILogger<RecommendationsController> _logger;

        public RecommendationsController(
            ITransactionRecommendationService recommendationService,
            ILogger<RecommendationsController> logger)
        {
            _recommendationService = recommendationService;
            _logger = logger;
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
