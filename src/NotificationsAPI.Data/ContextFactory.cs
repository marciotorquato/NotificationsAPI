using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NotificationsAPI.Data;

internal class ContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
{
    public NotificationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<NotificationDbContext>();
        optionsBuilder.UseSqlServer("Data source=(localdb)\\mssqllocaldb;Initial Catalog=MS_NotificationsAPI;Integrated security=true");

        return new NotificationDbContext(optionsBuilder.Options);
    }
}
