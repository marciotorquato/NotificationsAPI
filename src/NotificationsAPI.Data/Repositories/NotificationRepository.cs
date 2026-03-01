using Microsoft.EntityFrameworkCore;
using NotificationsAPI.Domain.Entities;
using NotificationsAPI.Domain.Interfaces.Repository;

namespace NotificationsAPI.Data.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _context;

    public NotificationRepository(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task<Notification> CreateAsync(Notification notification)
    {
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        return notification;
    }

    public async Task UpdateAsync(Notification notification)
    {
        _context.Notifications.Update(notification);
        await _context.SaveChangesAsync();
    }

    public async Task<Notification?> GetByIdAsync(Guid id)
    {
        return await _context.Notifications.FindAsync(id);
    }

    public async Task<IEnumerable<Notification>> GetByUsuarioIdAsync(Guid usuarioId)
    {
        return await _context.Notifications.Where(n => n.UsuarioId == usuarioId).OrderByDescending(n => n.DataCriacao).ToListAsync();
    }
}