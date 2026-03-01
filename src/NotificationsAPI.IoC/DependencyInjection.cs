using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationsAPI.Application;
using NotificationsAPI.Application.Consumers;
using NotificationsAPI.Data;
using NotificationsAPI.Data.Repositories;
using NotificationsAPI.Domain.Interfaces.Repository;
using NotificationsAPI.Messaging;

namespace NotificationsAPI.IoC;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NotificationDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("MS_NotificationsAPI"),
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                    sqlOptions.MigrationsAssembly(typeof(NotificationDbContext).Assembly.FullName);
                });
        });


        // Repositories
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // Services
        services.AddScoped<NotificationService>();

        // Consumers
        services.AddScoped<PaymentProcessedConsumer>();
        services.AddScoped<UserCreatedConsumer>();

        // RabbitMQ
        services.AddSingleton<RabbitMQInitializer>();
        services.AddHostedService<PaymentProcessedRabbitMQConsumer>();
        services.AddHostedService<UserCreatedRabbitMQConsumer>();

        return services;
    }
}