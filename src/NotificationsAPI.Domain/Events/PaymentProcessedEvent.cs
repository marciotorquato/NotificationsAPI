namespace NotificationsAPI.Domain.Events;

public class PaymentProcessedEvent
{
    public Guid UsuarioId { get; set; }
    public Guid GameId { get; set; }
    public string Status { get; set; }
    public DateTimeOffset DataProcessamento { get; set; }
}