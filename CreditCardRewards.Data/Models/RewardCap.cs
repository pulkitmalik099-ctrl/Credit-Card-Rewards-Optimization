using System;

namespace CreditCardRewards.Data.Models
{
    /// <summary>
    /// Represents reward earning caps (e.g., max annual points, max monthly cashback)
    /// </summary>
    public class RewardCap
    {
        public Guid Id { get; set; }
        public Guid CreditCardId { get; set; }
        public string CapType { get; set; } = null!; // "Annual", "Monthly", "Quarterly", "Transaction"
        public string Category { get; set; } = null!; // Category this cap applies to, "All" for all categories
        public decimal MaxRewardValue { get; set; } // Max points or cashback in rupees
        public string Unit { get; set; } = "Points"; // "Points", "Cashback", etc.
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveUntil { get; set; }
        
        // Navigation
        public CreditCard CreditCard { get; set; } = null!;
    }
}
