using FluentValidation;
using CreditCardRewards.Api.Models;

namespace CreditCardRewards.Api.Validators
{
    public class CreateCreditCardRequestValidator : AbstractValidator<CreateCreditCardRequest>
    {
        public CreateCreditCardRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Card name is required.")
                .MaximumLength(100);

            RuleFor(x => x.Issuer)
                .MaximumLength(50);

            RuleFor(x => x.TotalLimit)
                .GreaterThan(0).WithMessage("Credit limit must be greater than zero.");

            RuleFor(x => x.BaseRewardRate)
                .GreaterThanOrEqualTo(0).When(x => x.BaseRewardRate.HasValue)
                .WithMessage("Reward rate cannot be negative.");

            RuleFor(x => x.BaseRewardPointValue)
                .GreaterThanOrEqualTo(0).When(x => x.BaseRewardPointValue.HasValue)
                .WithMessage("Point value cannot be negative.");

            RuleFor(x => x.AccumulatedSpend)
                .GreaterThanOrEqualTo(0).WithMessage("Accumulated spend cannot be negative.");

            RuleFor(x => x.AccumulatedRewardPoints)
                .GreaterThanOrEqualTo(0).WithMessage("Accumulated points cannot be negative.");

            RuleFor(x => x.UserProfileId)
                .NotEmpty().WithMessage("User profile ID is required.");
        }
    }
}
