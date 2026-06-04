using System.ComponentModel.DataAnnotations;

namespace CreditCardRewards.Api.Models
{
    public class OnboardCreditCardsRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "Enter at least one credit card.")]
        public List<CreateCreditCardRequest> Cards { get; set; } = new();
    }
}
