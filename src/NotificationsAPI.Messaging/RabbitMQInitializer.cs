using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace NotificationsAPI.Messaging;

public class RabbitMQInitializer
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMQInitializer> _logger;

    public RabbitMQInitializer(IConfiguration configuration, ILogger<RabbitMQInitializer> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
            UserName = _configuration["RabbitMQ:Username"] ?? "admin",
            Password = _configuration["RabbitMQ:Password"] ?? "admin",
            VirtualHost = "/",
            Port = 5672
        };

        try
        {
            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            var paymentProcessedExchange = _configuration["RabbitMQ:Exchanges:PaymentProcessed"] ?? "payment-processed-exchange";

            await channel.ExchangeDeclareAsync(
                exchange: paymentProcessedExchange,
                type: ExchangeType.Fanout,
                durable: true,
                autoDelete: false,
                arguments: null
            );

            var notificationsQueue = "payment-processed-queue-notifications";
            await channel.QueueDeclareAsync(
                queue: notificationsQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            await channel.QueueBindAsync(
                queue: notificationsQueue,
                exchange: paymentProcessedExchange,
                routingKey: "",
                arguments: null
            );

            _logger.LogInformation(
                "RabbitMQ inicializado | Exchange: {Exchange} | Queue: {Queue}",
                paymentProcessedExchange,
                notificationsQueue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao inicializar RabbitMQ");
            throw;
        }
    }
}