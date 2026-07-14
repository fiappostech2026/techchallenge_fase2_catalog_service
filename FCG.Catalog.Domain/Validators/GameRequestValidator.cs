using FCG.Catalog.Domain.Dto;
using FluentValidation;

namespace FCG.Catalog.Domain.Validators;

public sealed class GameRequestValidator : AbstractValidator<GameRequest>
{
    public GameRequestValidator()
    {
        RuleFor(request => request.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.Description)
            .MaximumLength(2000);

        RuleFor(request => request.Genre)
            .MaximumLength(100);

        RuleFor(request => request.Price)
            .GreaterThan(0)
            .LessThanOrEqualTo(99999999.99m);
    }
}
