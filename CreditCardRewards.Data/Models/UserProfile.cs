using System;
using System.Collections.Generic;

namespace CreditCardRewards.Data.Models
{
    /// <summary>
    /// Represents a user profile in the system
    /// </summary>
    public class UserProfile
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        // Relationships
        public ICollection<CreditCard> CreditCards { get; set; } = new List<CreditCard>();
    }
}
