using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CreditCardRewards.Data.Context;
using CreditCardRewards.Data.Models;
using CreditCardRewards.DataRefresh.Interfaces;
using CreditCardRewards.DataRefresh.Models;

namespace CreditCardRewards.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatementsController : ControllerBase
    {
        private readonly IStatementParserService _parser;
        private readonly AppDbContext _context;
        private readonly ILogger<StatementsController> _logger;

        public StatementsController(
            IStatementParserService parser,
            AppDbContext context,
            ILogger<StatementsController> logger)
        {
            _parser = parser;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// List all parsed statements waiting for user confirmation
        /// </summary>
        [HttpGet("pending")]
        public ActionResult<IReadOnlyList<ParsedStatement>> GetPending()
        {
            return Ok(_parser.GetPending());
        }

        /// <summary>
        /// Manually upload and parse a statement file
        /// </summary>
        [HttpPost("upload")]
        public async Task<ActionResult<ParsedStatement>> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".pdf" && ext != ".csv")
                return BadRequest("Only PDF and CSV files are supported.");

            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{ext}");
            await using (var stream = System.IO.File.Create(tempPath))
            {
                await file.CopyToAsync(stream);
            }

            var result = await _parser.ParseAsync(tempPath);
            System.IO.File.Delete(tempPath);

            return Ok(result);
        }

        /// <summary>
        /// Confirm a parsed statement and save transactions to the database
        /// </summary>
        [HttpPost("{statementId}/confirm")]
        public async Task<IActionResult> Confirm(string statementId, [FromQuery] Guid cardId)
        {
            var pending = _parser.GetPending().FirstOrDefault(s => s.Id == statementId);
            if (pending == null)
                return NotFound("Statement not found or already confirmed.");

            var card = await _context.CreditCards.FindAsync(cardId);
            if (card == null)
                return NotFound("Card not found.");

            var now = DateTime.UtcNow;

            foreach (var tx in pending.Transactions)
            {
                _context.Transactions.Add(new Transaction
                {
                    Id = Guid.NewGuid(),
                    CreditCardId = cardId,
                    Amount = tx.Amount,
                    Merchant = tx.Merchant,
                    Category = tx.Category ?? "Other",
                    TransactionDate = tx.Date,
                    PointsEarned = tx.RewardPoints,
                    RewardValueInRupees = tx.RewardPoints * (card.BaseRewardPointValue ?? 0.5m),
                    Description = $"Imported from statement: {pending.FileName}"
                });
            }

            // Update accumulated reward points on the card
            card.AccumulatedRewardPoints += pending.RewardPointsEarned;
            card.LastUpdatedAt = now;

            await _context.SaveChangesAsync();
            _parser.MarkConfirmed(statementId);

            _logger.LogInformation(
                "Confirmed statement {Id}: {TxCount} transactions imported for card {CardId}",
                statementId, pending.Transactions.Count, cardId);

            return Ok(new
            {
                Imported = pending.Transactions.Count,
                RewardPointsAdded = pending.RewardPointsEarned,
                TotalSpend = pending.TotalSpend
            });
        }

        /// <summary>
        /// Dismiss / discard a parsed statement without importing
        /// </summary>
        [HttpDelete("{statementId}")]
        public IActionResult Dismiss(string statementId)
        {
            _parser.Remove(statementId);
            return NoContent();
        }
    }
}
