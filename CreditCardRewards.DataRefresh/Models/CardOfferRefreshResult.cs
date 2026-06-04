namespace CreditCardRewards.DataRefresh.Models
{
    public class CardOfferRefreshResult
    {
        public Guid CardId { get; set; }
        public string CardName { get; set; } = null!;
        public string Issuer { get; set; } = null!;
        public int ExistingActiveOffers { get; set; }
        public List<OfferSourceCandidate> SourceCandidates { get; set; } = new();
    }
}
