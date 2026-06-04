using Microsoft.EntityFrameworkCore;
using CreditCardRewards.Data.Context;
using CreditCardRewards.DataRefresh.Interfaces;
using CreditCardRewards.DataRefresh.Models;

namespace CreditCardRewards.DataRefresh.Services
{
    public class CardOfferRefreshService : ICardOfferRefreshService
    {
        private readonly AppDbContext _context;

        public CardOfferRefreshService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<CardOfferRefreshResult>> BuildRefreshPlanAsync(List<Guid>? cardIds = null)
        {
            var query = _context.CreditCards
                .Include(c => c.Offers.Where(o => o.IsActive))
                .AsQueryable();

            if (cardIds is { Count: > 0 })
            {
                query = query.Where(c => cardIds.Contains(c.Id));
            }

            var cards = await query
                .OrderBy(c => c.Issuer)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return cards.Select(card => new CardOfferRefreshResult
            {
                CardId = card.Id,
                CardName = card.Name,
                Issuer = card.Issuer,
                ExistingActiveOffers = card.Offers.Count,
                SourceCandidates = BuildSourceCandidates(card.Issuer, card.Name)
            }).ToList();
        }

        private static List<OfferSourceCandidate> BuildSourceCandidates(string issuer, string cardName)
        {
            var normalizedIssuer = issuer.Equals("Unknown", StringComparison.OrdinalIgnoreCase)
                ? cardName
                : issuer;

            return new List<OfferSourceCandidate>
            {
                new()
                {
                    Label = "Issuer offers page",
                    Query = $"{normalizedIssuer} {cardName} credit card offers campaigns"
                },
                new()
                {
                    Label = "Merchant campaigns",
                    Query = $"{cardName} credit card Amazon Swiggy Zomato Flipkart offers"
                },
                new()
                {
                    Label = "Reward program updates",
                    Query = $"{normalizedIssuer} {cardName} reward points cashback milestone offers"
                }
            };
        }
    }
}
