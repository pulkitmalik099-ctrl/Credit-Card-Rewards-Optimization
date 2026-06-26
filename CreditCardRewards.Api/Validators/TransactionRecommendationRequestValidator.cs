using FluentValidation;
using CreditCardRewards.Core.Dtos;

namespace CreditCardRewards.Api.Validators
{
    public class TransactionRecommendationRequestValidator : AbstractValidator<TransactionRecommendationRequest>
    {
        public TransactionRecommendationRequestValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Transaction amount must be greater than zero.");

            RuleFor(x => x.Merchant)
                .NotEmpty().WithMessage("Merchant name is required.")
                .MaximumLength(100);

            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("Category is required.");
        }
    }
}
