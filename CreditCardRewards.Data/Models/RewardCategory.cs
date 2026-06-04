using System;

namespace CreditCardRewards.Data.Models
{
    /// <summary>
    /// Represents an accelerated reward category for a credit card
    /// (e.g., "Dining" gets 5x points instead of base 1x)
    /// </summary>
    public class RewardCategory
    {
        public Guid Id { get; set; }
        public Guid CreditCardId { get; set; }
        public string Category { get; set; } = null!; // "Dining", "Travel", "Online Shopping", etc.
        public decimal RewardMultiplier { get; set; } // e.g., 5.0 for 5x base rate
        public decimal? Cap { get; set; } // Max reward points/cashback per month, null = unlimited
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveUntil { get; set; } // Null if ongoing
        
        // Navigation
        public CreditCard CreditCard { get; set; } = null!;
    }
}
