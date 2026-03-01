using Microsoft.Extensions.Logging;
using NotificationsAPI.Domain.Events;

namespace NotificationsAPI.Application.Consumers;

public class UserCreatedConsumer
{
    private readonly ILogger<UserCreatedConsumer> _logger;
    private readonly NotificationService _notificationService;

    public UserCreatedConsumer(ILogger<UserCreatedConsumer> logger, NotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task ProcessAsync(UserCreatedEvent userEvent)
    {
        _logger.LogInformation("CONSUMER EXECUTADO | UsuarioId: {UsuarioId}", userEvent.UsuarioId);

        try
        {
            await _notificationService.EnviarNotificacaoBoasVindasAsync(userEvent.UsuarioId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar UserCreated | UsuarioId: {UsuarioId}", userEvent.UsuarioId);
            throw;
        }
    }
}