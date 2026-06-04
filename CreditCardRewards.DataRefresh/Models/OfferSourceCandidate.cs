namespace CreditCardRewards.DataRefresh.Models
{
    public class OfferSourceCandidate
    {
        public string Label { get; set; } = null!;
        public string Query { get; set; } = null!;
        public string? Url { get; set; }
    }
}
