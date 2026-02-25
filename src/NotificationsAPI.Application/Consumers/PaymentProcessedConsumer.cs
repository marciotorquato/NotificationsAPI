using Microsoft.Extensions.Logging;
using NotificationsAPI.Domain.Events;

namespace NotificationsAPI.Application.Consumers;

public class PaymentProcessedConsumer
{
    private readonly ILogger<PaymentProcessedConsumer> _logger;

    public PaymentProcessedConsumer(ILogger<PaymentProcessedConsumer> logger)
    {
        _logger = logger;
    }

    public async Task ProcessAsync(PaymentProcessedEvent paymentEvent)
    {
        _logger.LogInformation(
            "📧 NOTIFICAÇÃO DISPARADA | UsuarioId: {UsuarioId} | GameId: {GameId} | Status: {Status}",
            paymentEvent.UsuarioId,
            paymentEvent.GameId,
            paymentEvent.Status);

        try
        {
            await Task.Delay(100);

            _logger.LogInformation("Notificação enviada com sucesso | UsuarioId: {UsuarioId}", paymentEvent.UsuarioId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar notificação | UsuarioId: {UsuarioId}", paymentEvent.UsuarioId);
            throw;
        }
    }
}
