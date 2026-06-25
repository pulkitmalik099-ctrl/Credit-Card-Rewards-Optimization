namespace CreditCardRewards.DataRefresh.Models
{
    public class CardLookupResult
    {
        public string CardName { get; set; } = null!;
        public string Issuer { get; set; } = null!;
        public decimal BaseRewardRate { get; set; }
        public decimal BaseRewardPointValue { get; set; }
        public string BaseRewardUnit { get; set; } = "Points";
        public decimal AnnualFee { get; set; }
        public decimal JoiningFee { get; set; }
        public decimal AnnualFeeWaiverSpendThreshold { get; set; }
        public List<AcceleratedCategoryInfo> AcceleratedCategories { get; set; } = new();
        public string? WelcomeOffer { get; set; }
        public string? AirportLoungeBenefits { get; set; }
        public string? TransferPartners { get; set; }
        public string DataSource { get; set; } = "OpenAI";
        public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
        public string Disclaimer { get; set; } = "Reward rates are approximate and subject to change. Verify with your bank before making decisions.";
        public bool IsConfident { get; set; } = true;
    }

    public class AcceleratedCategoryInfo
    {
        public string Category { get; set; } = null!;
        public decimal RewardRate { get; set; }
        public string? MonthlyCap { get; set; }
    }
}