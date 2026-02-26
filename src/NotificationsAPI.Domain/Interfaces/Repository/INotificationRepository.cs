using NotificationsAPI.Domain.Entities;

namespace NotificationsAPI.Domain.Interfaces.Repository;

public interface INotificationRepository
{
    Task<Notification> CreateAsync(Notification notification);
    Task UpdateAsync(Notification notification);
    Task<Notification?> GetByIdAsync(Guid id);
    Task<IEnumerable<Notification>> GetByUsuarioIdAsync(Guid usuarioId);
}