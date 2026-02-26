using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationsAPI.Application.Consumers;
using NotificationsAPI.Messaging;

namespace NotificationsAPI.IoC;

public static class RabbitMQExtensions
{
    public static IServiceCollection AddRabbitMQMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        // Registrar o inicializador
        services.AddSingleton<RabbitMQInitializer>();

        // Registrar o Consumer como Scoped
        services.AddScoped<PaymentProcessedConsumer>();

        // Registrar Background Service
        services.AddHostedService<PaymentProcessedRabbitMQConsumer>();

        return services;
    }
}