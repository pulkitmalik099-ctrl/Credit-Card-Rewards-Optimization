using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CreditCardRewards.Core.Interfaces;

namespace CreditCardRewards.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SpendingController : ControllerBase
    {
        private readonly ISpendTrackingService _spendTrackingService;
        private readonly ILogger<SpendingController> _logger;

        public SpendingController(
            ISpendTrackingService spendTrackingService,
            ILogger<SpendingController> logger)
        {
            _spendTrackingService = spendTrackingService;
            _logger = logger;
        }

        /// <summary>
        /// Get spend summary for a specific card
        /// </summary>
        [HttpGet("card/{cardId}/summary")]
        public async Task<IActionResult> GetCardSpendSummary(Guid cardId, int? year = null, int? month = null)
        {
            try
            {
                _logger.LogInformation("Getting spend summary for card: {CardId}, Year: {Year}, Month: {Month}", cardId, year, month);

                var summary = await _spendTrackingService.GetCardSpendSummaryAsync(cardId, year, month);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting spend summary");
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get portfolio spending summary
        /// </summary>
        [HttpGet("portfolio/summary")]
        public async Task<IActionResult> GetPortfolioSpendSummary([FromQuery] Guid userProfileId, int? year = null)
        {
            try
            {
                _logger.LogInformation("Getting portfolio spend summary for User: {UserProfileId}, Year: {Year}", userProfileId, year);

                var summaries = await _spendTrackingService.GetPortfolioSpendSummaryAsync(userProfileId, year);
                return Ok(new
                {
                    Year = year ?? DateTime.Now.Year,
                    Cards = summaries,
                    TotalAnnualSpend = summaries.Sum(s => s.CurrentAnnualSpend),
                    TotalRewardsEarned = summaries.Sum(s => s.TotalRewardValueEarned)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting portfolio summary");
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Add a transaction
        /// </summary>
        [HttpPost("transaction")]
        public async Task<IActionResult> AddTransaction(
            [FromQuery] Guid cardId,
            [FromQuery] decimal amount,
            [FromQuery] string merchant,
            [FromQuery] string category)
        {
            try
            {
                _logger.LogInformation(
                    "Adding transaction: CardId={CardId}, Amount={Amount}, Merchant={Merchant}, Category={Category}",
                    cardId, amount, merchant, category);

                await _spendTrackingService.AddTransactionAndUpdateSpendAsync(cardId, amount, merchant, category);

                return Ok(new { Message = "Transaction added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding transaction");
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get milestone progress for a card
        /// </summary>
        [HttpGet("card/{cardId}/milestones")]
        public async Task<IActionResult> GetMilestoneProgress(Guid cardId)
        {
            try
            {
                _logger.LogInformation("Getting milestone progress for card: {CardId}", cardId);

                var progress = await _spendTrackingService.GetMilestoneProgressAsync(cardId);
                return Ok(progress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting milestone progress");
                return BadRequest(ex.Message);
            }
        }
    }
}
