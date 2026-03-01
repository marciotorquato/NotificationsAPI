using Microsoft.Extensions.Logging;
using NotificationsAPI.Domain.Events;

namespace NotificationsAPI.Application.Consumers;

public class PaymentProcessedConsumer
{
    private readonly ILogger<PaymentProcessedConsumer> _logger;
    private readonly NotificationService _notificationService;

    public PaymentProcessedConsumer(ILogger<PaymentProcessedConsumer> logger, NotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task ProcessAsync(PaymentProcessedEvent paymentEvent)
    {
        _logger.LogInformation(
            "CONSUMER EXECUTADO | UsuarioId: {UsuarioId} | GameId: {GameId} | Status: {Status}",
            paymentEvent.UsuarioId,
            paymentEvent.GameId,
            paymentEvent.Status);

        try
        {
            if (paymentEvent.Status.Equals("Aprovado", StringComparison.OrdinalIgnoreCase))
            {
                await _notificationService.EnviarNotificacaoConfirmacaoCompraAsync(paymentEvent.UsuarioId, paymentEvent.GameId);
            }
            else
            {
                _logger.LogWarning("Pagamento não aprovado. Notificação não enviada | Status: {Status}", paymentEvent.Status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar PaymentProcessed | UsuarioId: {UsuarioId}", paymentEvent.UsuarioId);
            throw;
        }
    }
}
