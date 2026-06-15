using System;
using System.Collections.Generic;

namespace CreditCardRewards.Data.Models
{
    /// <summary>
    /// Represents a credit card in the user's portfolio
    /// </summary>
    public class CreditCard
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!; // Card name (e.g., "HDFC Infinia")
        public string Issuer { get; set; } = null!; // Bank name (e.g., "HDFC")
        public decimal TotalLimit { get; set; } // Total credit limit available on the card
        public decimal JoiningFee { get; set; } // One-time joining fee
        public decimal AnnualFee { get; set; } // Recurring annual fee
        public decimal AnnualFeeWaiverSpendThreshold { get; set; } // Spend amount to waive annual fee
        public decimal AccumulatedSpend { get; set; } // Spent amount done on the card prior to onboarding
        public decimal AccumulatedRewardPoints { get; set; } // Reward points earned on the card prior to onboarding
        
        // Reward structure
        public decimal BaseRewardRate { get; set; } // Base reward rate (e.g., 1% = 1.0)
        public string BaseRewardUnit { get; set; } = "Points"; // "Points", "Cashback", etc.
        
        // Milestone tracking
        public decimal? BaseRewardPointValue { get; set; } // ₹ value per reward point
        
        // Transfer partners
        public string? TransferPartners { get; set; } // Comma-separated partner names
        
        // Card benefits
        public string? AirportLoungeBenefits { get; set; }
        public string? HotelBenefits { get; set; }
        public string? TravelBenefits { get; set; }
        public string? OtherBenefits { get; set; }
        
        // Welcome offer
        public string? WelcomeOffer { get; set; }
        public decimal? WelcomeOfferValue { get; set; }
        
        // Data refresh tracking
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public string DataSource { get; set; } = "Web Scrape"; // "Web Scrape", "Manual", etc.
        
        // Relationships
        public ICollection<RewardCategory> AcceleratedCategories { get; set; } = new List<RewardCategory>();
        public ICollection<RewardCap> RewardCaps { get; set; } = new List<RewardCap>();
        public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
        public ICollection<RewardOffer> Offers { get; set; } = new List<RewardOffer>();
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
