using FCG.Catalog.Domain.Dto;
using FCG.Catalog.Domain.Enums;
using FCG.Catalog.Domain.Interfaces.IService;
using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace FCG.Catalog.Api.Consumers;

public sealed class PaymentProcessedEventConsumer : IConsumer<PaymentProcessedEvent>
{
    private readonly IValidator<PaymentProcessedEvent> _validator;
    private readonly ILibraryService _libraryService;
    private readonly ILogger<PaymentProcessedEventConsumer> _logger;

    public PaymentProcessedEventConsumer(
        IValidator<PaymentProcessedEvent> validator,
        ILibraryService libraryService,
        ILogger<PaymentProcessedEventConsumer> logger)
    {
        _validator = validator;
        _libraryService = libraryService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        // delay proposital para demo — dá tempo de ver a mensagem na fila no RabbitMQ Management UI
        await Task.Delay(TimeSpan.FromSeconds(5));

        var result = await _validator.ValidateAsync(context.Message);

        if (!result.IsValid)
        {
            _logger.LogWarning("Falha na validação de PaymentProcessedEvent: {Errors}", result.ToString());
            return;
        }

        if (context.Message.Status != PaymentStatus.Approved)
            return;

        try
        {
            await _libraryService.AddToLibraryAsync(context.Message.UserId, context.Message.GameId);

            _logger.LogInformation("Jogo {GameId} adicionado à biblioteca do usuário {UserId}", context.Message.GameId, context.Message.UserId);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Jogo {GameId} já está na biblioteca do usuário {UserId}, entrega concorrente ignorada.", context.Message.GameId, context.Message.UserId);
        }
    }
}
