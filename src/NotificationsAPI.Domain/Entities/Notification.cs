using NotificationsAPI.Domain.Enums;

namespace NotificationsAPI.Domain.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public TipoNotificacao Tipo { get; set; }
    public string Destinatario { get; set; } = string.Empty;
    public string Assunto { get; set; } = string.Empty;
    public string Corpo { get; set; } = string.Empty;
    public StatusNotificacao Status { get; set; }
    public DateTimeOffset DataCriacao { get; set; }
    public DateTimeOffset? DataEnvio { get; set; }
    public int TentativasEnvio { get; set; }
    public string? ErroMensagem { get; set; }
}