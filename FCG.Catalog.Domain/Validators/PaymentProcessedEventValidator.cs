using FCG.Catalog.Domain.Dto;
using FluentValidation;

namespace FCG.Catalog.Domain.Validators;

public sealed class PaymentProcessedEventValidator : AbstractValidator<PaymentProcessedEvent>
{
    public PaymentProcessedEventValidator()
    {
        RuleFor(evt => evt.OrderId)
            .NotEqual(Guid.Empty);

        RuleFor(evt => evt.UserId)
            .NotEqual(Guid.Empty);

        RuleFor(evt => evt.GameId)
            .NotEqual(Guid.Empty);

        RuleFor(evt => evt.Status)
            .IsInEnum();
    }
}
