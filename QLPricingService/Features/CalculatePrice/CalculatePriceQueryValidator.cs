using FluentValidation;

namespace QLPricingService.Features.CalculatePrice;

public class CalculatePriceQueryValidator : AbstractValidator<CalculatePriceQuery>
{
    public CalculatePriceQueryValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("CustomerId must be positive.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("StartDate is required.")
            // Add reasonable bounds if applicable (e.g., not too far in the past/future)
            .LessThan(DateTime.UtcNow.AddYears(10)).WithMessage("StartDate seems too far in the future.")
            .GreaterThan(DateTime.UtcNow.AddYears(-20)).WithMessage("StartDate seems too far in the past.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("EndDate is required.")
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("EndDate must be on or after StartDate.");
        // Add reasonable bounds
        // .LessThan(DateTime.UtcNow.AddYears(10)).WithMessage("EndDate seems too far in the future.");
    }
}