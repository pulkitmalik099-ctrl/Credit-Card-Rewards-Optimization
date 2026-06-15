using System.ComponentModel.DataAnnotations;

namespace CreditCardRewards.Api.Models
{
    public class CreateCreditCardRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [Range(1, double.MaxValue, ErrorMessage = "Total limit must be greater than zero.")]
        public decimal TotalLimit { get; set; }

        [MaxLength(50)]
        public string? Issuer { get; set; }

        public decimal? BaseRewardRate { get; set; }

        public decimal? BaseRewardPointValue { get; set; }

        public decimal AccumulatedSpend { get; set; } = 0;

        public decimal AccumulatedRewardPoints { get; set; } = 0;
    }
}
