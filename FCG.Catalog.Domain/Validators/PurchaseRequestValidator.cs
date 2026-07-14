using FCG.Catalog.Domain.Dto;
using FluentValidation;

namespace FCG.Catalog.Domain.Validators;

public sealed class PurchaseRequestValidator : AbstractValidator<PurchaseRequest>
{
    public PurchaseRequestValidator()
    {
        RuleFor(request => request.UserId)
            .NotEqual(Guid.Empty);

        RuleFor(request => request.GameId)
            .NotEqual(Guid.Empty);
    }
}
