using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CreditCardRewards.Api.Models;
using CreditCardRewards.Data.Context;
using CreditCardRewards.Data.Models;
using CreditCardRewards.DataRefresh.Interfaces;
using CreditCardRewards.DataRefresh.Models;

namespace CreditCardRewards.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CardsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICardOfferRefreshService _cardOfferRefreshService;
        private readonly ICardLookupService _cardLookupService;
        private readonly ILogger<CardsController> _logger;

        public CardsController(
            AppDbContext context,
            ICardOfferRefreshService cardOfferRefreshService,
            ICardLookupService cardLookupService,
            ILogger<CardsController> logger)
        {
            _context = context;
            _cardOfferRefreshService = cardOfferRefreshService;
            _cardLookupService = cardLookupService;
            _logger = logger;
        }

        /// <summary>
        /// Auto-fetch reward rates for a card using OpenAI
        /// </summary>
        [HttpGet("lookup")]
        public async Task<ActionResult<CardLookupResult>> LookupCardRates([FromQuery] string name, [FromQuery] string issuer)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Card name is required.");

            _logger.LogInformation("Looking up reward rates for {CardName} / {Issuer}", name, issuer);
            var result = await _cardLookupService.LookupCardAsync(name, issuer ?? "Unknown");

            if (result == null)
                return StatusCode(503, "Card lookup unavailable. Check OpenAI API key configuration.");

            return Ok(result);
        }

        /// <summary>
        /// Get all credit cards in the portfolio
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<CreditCard>>> GetAllCards([FromQuery] Guid userProfileId)
        {
            _logger.LogInformation("Fetching all credit cards for user {UserProfileId}", userProfileId);
            var cards = await _context.CreditCards
                .Include(c => c.AcceleratedCategories)
                .Include(c => c.RewardCaps)
                .Include(c => c.Milestones)
                .Where(c => c.UserProfileId == userProfileId)
                .ToListAsync();

            return Ok(cards);
        }

        /// <summary>
        /// Get a specific credit card by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<CreditCard>> GetCardById(Guid id)
        {
            _logger.LogInformation("Fetching card with ID: {CardId}", id);
            var card = await _context.CreditCards
                .Include(c => c.AcceleratedCategories)
                .Include(c => c.RewardCaps)
                .Include(c => c.Milestones)
                .Include(c => c.Offers)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (card == null)
            {
                _logger.LogWarning("Card not found with ID: {CardId}", id);
                return NotFound();
            }

            return Ok(card);
        }

        /// <summary>
        /// Add a new credit card
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<CreditCard>> AddCard([FromBody] CreateCreditCardRequest request)
        {
            _logger.LogInformation("Adding new card: {CardName}", request.Name);

            var card = new CreditCard
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Issuer = string.IsNullOrWhiteSpace(request.Issuer) ? "Unknown" : request.Issuer,
                TotalLimit = request.TotalLimit,
                BaseRewardRate = request.BaseRewardRate ?? 1m,
                BaseRewardPointValue = request.BaseRewardPointValue ?? 0.5m,
                BaseRewardUnit = "Points",
                AccumulatedSpend = request.AccumulatedSpend,
                AccumulatedRewardPoints = request.AccumulatedRewardPoints,
                UserProfileId = request.UserProfileId,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };

            _context.CreditCards.Add(card);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Card added successfully with ID: {CardId}", card.Id);
            return CreatedAtAction(nameof(GetCardById), new { id = card.Id }, card);
        }

        /// <summary>
        /// Initial onboarding: enter all cards you own before campaign refresh runs
        /// </summary>
        [HttpPost("onboarding")]
        public async Task<ActionResult<List<CreditCard>>> OnboardCards([FromBody] OnboardCreditCardsRequest request)
        {
            _logger.LogInformation("Onboarding {CardCount} credit cards", request.Cards.Count);

            var now = DateTime.UtcNow;
            var cards = request.Cards.Select(cardRequest => new CreditCard
            {
                Id = Guid.NewGuid(),
                Name = cardRequest.Name,
                Issuer = string.IsNullOrWhiteSpace(cardRequest.Issuer) ? "Unknown" : cardRequest.Issuer,
                TotalLimit = cardRequest.TotalLimit,
                BaseRewardRate = cardRequest.BaseRewardRate ?? 1m,
                BaseRewardPointValue = cardRequest.BaseRewardPointValue ?? 0.5m,
                BaseRewardUnit = "Points",
                AccumulatedSpend = cardRequest.AccumulatedSpend,
                AccumulatedRewardPoints = cardRequest.AccumulatedRewardPoints,
                UserProfileId = request.UserProfileId,
                CreatedAt = now,
                LastUpdatedAt = now
            }).ToList();

            _context.CreditCards.AddRange(cards);
            await _context.SaveChangesAsync();

            return Ok(cards);
        }

        /// <summary>
        /// Build a card-specific campaign and offer refresh plan
        /// </summary>
        [HttpPost("offers/refresh-plan")]
        public async Task<IActionResult> BuildOfferRefreshPlan([FromBody] List<Guid>? cardIds = null)
        {
            _logger.LogInformation("Building offer refresh plan");

            var refreshPlan = await _cardOfferRefreshService.BuildRefreshPlanAsync(cardIds);
            if (!refreshPlan.Any())
            {
                return BadRequest("Add your credit cards first so relevant campaigns and offers can be fetched.");
            }

            return Ok(refreshPlan);
        }

        /// <summary>
        /// Update an existing credit card
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCard(Guid id, [FromBody] CreditCard updatedCard)
        {
            _logger.LogInformation("Updating card with ID: {CardId}", id);

            var card = await _context.CreditCards.FindAsync(id);
            if (card == null)
            {
                _logger.LogWarning("Card not found for update with ID: {CardId}", id);
                return NotFound();
            }

            card.Name = updatedCard.Name;
            card.Issuer = updatedCard.Issuer;
            card.TotalLimit = updatedCard.TotalLimit;
            card.JoiningFee = updatedCard.JoiningFee;
            card.AnnualFee = updatedCard.AnnualFee;
            card.AnnualFeeWaiverSpendThreshold = updatedCard.AnnualFeeWaiverSpendThreshold;
            card.BaseRewardRate = updatedCard.BaseRewardRate;
            card.BaseRewardUnit = updatedCard.BaseRewardUnit;
            card.BaseRewardPointValue = updatedCard.BaseRewardPointValue;
            card.TransferPartners = updatedCard.TransferPartners;
            card.AccumulatedSpend = updatedCard.AccumulatedSpend;
            card.AccumulatedRewardPoints = updatedCard.AccumulatedRewardPoints;
            card.LastUpdatedAt = DateTime.UtcNow;

            _context.CreditCards.Update(card);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Card updated successfully with ID: {CardId}", id);
            return Ok(card);
        }

        /// <summary>
        /// Delete a credit card
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCard(Guid id)
        {
            _logger.LogInformation("Deleting card with ID: {CardId}", id);

            var card = await _context.CreditCards.FindAsync(id);
            if (card == null)
            {
                _logger.LogWarning("Card not found for deletion with ID: {CardId}", id);
                return NotFound();
            }

            _context.CreditCards.Remove(card);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Card deleted successfully with ID: {CardId}", id);
            return NoContent();
        }

        /// <summary>
        /// Get portfolio summary
        /// </summary>
        [HttpGet("portfolio/summary")]
        public async Task<ActionResult<dynamic>> GetPortfolioSummary([FromQuery] Guid userProfileId)
        {
            _logger.LogInformation("Fetching portfolio summary for user {UserProfileId}", userProfileId);

            var cards = await _context.CreditCards
                .Include(c => c.Transactions)
                .Where(c => c.UserProfileId == userProfileId)
                .ToListAsync();

            var summary = new
            {
                TotalCards = cards.Count,
                TotalAnnualFees = cards.Sum(c => c.AnnualFee),
                TotalAnnualSpend = cards.Sum(c => c.AccumulatedSpend) + cards.SelectMany(c => c.Transactions)
                    .Where(t => t.TransactionDate.Year == DateTime.Now.Year)
                    .Sum(t => t.Amount),
                TotalRewardsEarned = cards.Sum(c => c.AccumulatedRewardPoints * (c.BaseRewardPointValue ?? 0.5m)) + cards.SelectMany(c => c.Transactions)
                    .Where(t => t.TransactionDate.Year == DateTime.Now.Year)
                    .Sum(t => t.RewardValueInRupees),
                Cards = cards.Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Issuer,
                    c.TotalLimit,
                    CurrentYearSpend = c.AccumulatedSpend + c.Transactions
                        .Where(t => t.TransactionDate.Year == DateTime.Now.Year)
                        .Sum(t => t.Amount),
                    CurrentYearRewards = (c.AccumulatedRewardPoints * (c.BaseRewardPointValue ?? 0.5m)) + c.Transactions
                        .Where(t => t.TransactionDate.Year == DateTime.Now.Year)
                        .Sum(t => t.RewardValueInRupees)
                }).ToList()
            };

            return Ok(summary);
        }
    }
}
