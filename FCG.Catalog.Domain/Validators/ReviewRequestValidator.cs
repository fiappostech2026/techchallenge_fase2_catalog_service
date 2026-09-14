using FCG.Catalog.Domain.Dto;
using FluentValidation;

namespace FCG.Catalog.Domain.Validators;

public sealed class ReviewRequestValidator : AbstractValidator<ReviewRequest>
{
    public ReviewRequestValidator()
    {
        RuleFor(request => request.Rating)
            .InclusiveBetween(1, 5);

        RuleFor(request => request.Comment)
            .MaximumLength(2000);
    }
}
