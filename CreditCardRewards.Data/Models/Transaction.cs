using System;

namespace CreditCardRewards.Data.Models
{
    /// <summary>
    /// Represents a transaction logged by the user
    /// </summary>
    public class Transaction
    {
        public Guid Id { get; set; }
        public Guid CreditCardId { get; set; }
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; } // Transaction amount in ₹
        public string Merchant { get; set; } = null!; // "Amazon", "Swiggy", etc.
        public string Category { get; set; } = null!; // "Online Shopping", "Dining", etc.
        public string? Description { get; set; }
        
        // Reward calculation results (cached after calculation)
        public decimal PointsEarned { get; set; }
        public decimal CashbackEarned { get; set; }
        public decimal RewardValueInRupees { get; set; } // Total reward value in ₹
        public decimal EffectiveReturnPercentage { get; set; } // Reward as % of transaction amount
        
        // Milestone tracking
        public bool ContributedToMilestone { get; set; } = false;
        public Guid? MilestoneId { get; set; } // Which milestone this transaction contributed to
        
        // Fee waiver tracking
        public bool ContributedToFeeWaiver { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation
        public CreditCard CreditCard { get; set; } = null!;
    }
}
