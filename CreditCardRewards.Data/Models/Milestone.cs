using System;

namespace CreditCardRewards.Data.Models
{
    /// <summary>
    /// Represents a spending milestone that unlocks bonus rewards
    /// (e.g., Spend ₹3 lakh to get ₹5,000 bonus)
    /// </summary>
    public class Milestone
    {
        public Guid Id { get; set; }
        public Guid CreditCardId { get; set; }
        public string Title { get; set; } = null!; // "First ₹3 Lakh Spend", etc.
        public decimal SpendThreshold { get; set; } // ₹ amount to reach milestone
        public decimal RewardValue { get; set; } // ₹ or points value of reward
        public string RewardUnit { get; set; } = "Rupees"; // "Rupees", "Points", "AirMiles", etc.
        public bool IsAutomatic { get; set; } = true; // Whether reward posts automatically
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveUntil { get; set; }
        
        // Navigation
        public CreditCard CreditCard { get; set; } = null!;
    }
}
