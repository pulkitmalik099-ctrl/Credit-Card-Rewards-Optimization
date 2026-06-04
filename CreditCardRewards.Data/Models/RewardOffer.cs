using System;

namespace CreditCardRewards.Data.Models
{
    /// <summary>
    /// Represents a special offer (merchant-specific, time-limited promotion)
    /// </summary>
    public class RewardOffer
    {
        public Guid Id { get; set; }
        public Guid CreditCardId { get; set; }
        public string Title { get; set; } = null!; // "Get 5% extra cashback at Amazon"
        public string? Merchant { get; set; } // "Amazon", "Swiggy", null = all merchants
        public string? Category { get; set; } // "Dining", "Shopping", null = all categories
        public decimal RewardMultiplier { get; set; } // 1.05 for 5% bonus, 5.0 for 5x bonus
        public string RewardBasis { get; set; } = "Multiplier"; // "Multiplier", "FlatBonus"
        public decimal? FlatBonusAmount { get; set; } // Used if RewardBasis = "FlatBonus"
        public string RewardUnit { get; set; } = "Cashback"; // "Cashback", "Points", "Miles"
        public decimal? MaxRewardPerTransaction { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidUntil { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Description { get; set; }
        
        // Navigation
        public CreditCard CreditCard { get; set; } = null!;
    }
}
