using Microsoft.Extensions.Logging;
using NotificationsAPI.Data;
using NotificationsAPI.Domain.Entities;
using NotificationsAPI.Domain.Enums;

namespace NotificationsAPI.Application;

public class NotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly NotificationDbContext _context;

    public NotificationService(ILogger<NotificationService> logger, NotificationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task EnviarNotificacaoBoasVindasAsync(Guid usuarioId)
    {
        var email = $"usuario-{usuarioId}@example.com";

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Tipo = TipoNotificacao.BoasVindas,
            Destinatario = email,
            Assunto = "Bem-vindo à FIAP Cloud Games!",
            Corpo = "Olá! Seja bem-vindo à nossa plataforma de jogos.",
            Status = StatusNotificacao.Pendente,
            DataCriacao = DateTimeOffset.UtcNow,
            TentativasEnvio = 0
        };

        await _context.Notifications.AddAsync(notification);
        await _context.SaveChangesAsync();

        try
        {
            _logger.LogInformation("ENVIANDO E-MAIL DE BOAS-VINDAS | UsuarioId: {UsuarioId} | Para: {Email}", usuarioId, email);

            await Task.Delay(100); // Simular envio

            notification.Status = StatusNotificacao.Enviado;
            notification.DataEnvio = DateTimeOffset.UtcNow;
            notification.TentativasEnvio = 1;

            await _context.SaveChangesAsync();

            _logger.LogInformation("E-mail de boas-vindas enviado com sucesso");
        }
        catch (Exception ex)
        {
            notification.Status = StatusNotificacao.Falhou;
            notification.ErroMensagem = ex.Message;
            notification.TentativasEnvio++;

            await _context.SaveChangesAsync();

            _logger.LogError(ex, "Erro ao enviar e-mail de boas-vindas");
            throw;
        }
    }

    public async Task EnviarNotificacaoConfirmacaoCompraAsync(Guid usuarioId, Guid gameId)
    {
        var email = $"usuario-{usuarioId}@example.com";

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Tipo = TipoNotificacao.ConfirmacaoCompra,
            Destinatario = email,
            Assunto = "Compra confirmada - FIAP Cloud Games",
            Corpo = $"Sua compra do jogo (ID: {gameId}) foi confirmada!",
            Status = StatusNotificacao.Pendente,
            DataCriacao = DateTimeOffset.UtcNow,
            TentativasEnvio = 0
        };

        await _context.Notifications.AddAsync(notification);
        await _context.SaveChangesAsync();

        try
        {
            _logger.LogInformation("ENVIANDO E-MAIL DE CONFIRMAÇÃO | UsuarioId: {UsuarioId}", usuarioId);

            await Task.Delay(100); // simular envio

            notification.Status = StatusNotificacao.Enviado;
            notification.DataEnvio = DateTimeOffset.UtcNow;
            notification.TentativasEnvio = 1;

            await _context.SaveChangesAsync();

            _logger.LogInformation("E-mail de confirmação enviado");
        }
        catch (Exception ex)
        {
            notification.Status = StatusNotificacao.Falhou;
            notification.ErroMensagem = ex.Message;
            notification.TentativasEnvio++;

            await _context.SaveChangesAsync();

            _logger.LogError(ex, "Erro ao enviar e-mail de confirmação");
            throw;
        }
    }
}