using Amazon.Lambda.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotificationsAPI.Application.Consumers;
using NotificationsAPI.Data;
using NotificationsAPI.Domain.Events;
using NotificationsAPI.IoC;
using Serilog;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace NotificationsAPI.Function;

public class Function
{
    private const string UserCreatedQueueName = "user-created-queue-notifications";
    private const string PaymentProcessedQueueName = "payment-processed-queue-notifications";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Lazy<ServiceProvider> ServiceProvider = new(BuildServiceProvider);

    public async Task FunctionHandler(RabbitMqLambdaEvent rabbitMqEvent, ILambdaContext context)
    {
        if (rabbitMqEvent.RmqMessagesByQueue.Count == 0)
        {
            context.Logger.LogLine("Evento RabbitMQ recebido sem mensagens.");
            return;
        }

        foreach (var queueBatch in rabbitMqEvent.RmqMessagesByQueue)
        {
            var queueName = GetQueueName(queueBatch.Key);

            foreach (var message in queueBatch.Value)
            {
                await ProcessMessageAsync(queueName, message, context);
            }
        }
    }

    private static async Task ProcessMessageAsync(
        string queueName,
        RabbitMqMessage message,
        ILambdaContext context)
    {
        var body = DecodeMessageBody(message.Data);
        var messageId = message.BasicProperties?.MessageId ?? context.AwsRequestId;

        context.Logger.LogLine(
            $"Mensagem RabbitMQ recebida | MessageId: {messageId} | Queue: {queueName} | Redelivered: {message.Redelivered} | Body: {body}");

        using var scope = ServiceProvider.Value.CreateScope();

        switch (queueName)
        {
            case UserCreatedQueueName:
                var userCreatedEvent = Deserialize<UserCreatedEvent>(body, messageId);
                var userCreatedConsumer = scope.ServiceProvider.GetRequiredService<UserCreatedConsumer>();
                await userCreatedConsumer.ProcessAsync(userCreatedEvent);
                break;

            case PaymentProcessedQueueName:
                var paymentProcessedEvent = Deserialize<PaymentProcessedEvent>(body, messageId);
                var paymentProcessedConsumer = scope.ServiceProvider.GetRequiredService<PaymentProcessedConsumer>();
                await paymentProcessedConsumer.ProcessAsync(paymentProcessedEvent);
                break;

            default:
                throw new InvalidOperationException(
                    $"Fila RabbitMQ nao mapeada para NotificationsAPI.Function: {queueName}");
        }

        context.Logger.LogLine(
            $"Mensagem RabbitMQ processada com sucesso | MessageId: {messageId} | Queue: {queueName}");
    }

    private static T Deserialize<T>(string body, string messageId)
    {
        var message = JsonSerializer.Deserialize<T>(body, JsonOptions);

        return message is null
            ? throw new JsonException($"Mensagem RabbitMQ invalida ou vazia | MessageId: {messageId}")
            : message;
    }

    private static string DecodeMessageBody(string data)
    {
        var bytes = Convert.FromBase64String(data);
        return Encoding.UTF8.GetString(bytes);
    }

    private static string GetQueueName(string queueKey)
    {
        var separatorIndex = queueKey.IndexOf("::", StringComparison.Ordinal);
        return separatorIndex >= 0 ? queueKey[..separatorIndex] : queueKey;
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddSerilog(Log.Logger, dispose: true);
        });
        services.AddInfrastructure(configuration);

        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        dbContext.Database.Migrate();

        return serviceProvider;
    }
}

public sealed class RabbitMqLambdaEvent
{
    [JsonPropertyName("eventSource")]
    public string? EventSource { get; init; }

    [JsonPropertyName("eventSourceArn")]
    public string? EventSourceArn { get; init; }

    [JsonPropertyName("rmqMessagesByQueue")]
    public Dictionary<string, List<RabbitMqMessage>> RmqMessagesByQueue { get; init; } = [];
}

public sealed class RabbitMqMessage
{
    [JsonPropertyName("basicProperties")]
    public RabbitMqBasicProperties? BasicProperties { get; init; }

    [JsonPropertyName("redelivered")]
    public bool Redelivered { get; init; }

    [JsonPropertyName("data")]
    public string Data { get; init; } = string.Empty;
}

public sealed class RabbitMqBasicProperties
{
    [JsonPropertyName("contentType")]
    public string? ContentType { get; init; }

    [JsonPropertyName("contentEncoding")]
    public string? ContentEncoding { get; init; }

    [JsonPropertyName("deliveryMode")]
    public int? DeliveryMode { get; init; }

    [JsonPropertyName("priority")]
    public int? Priority { get; init; }

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; init; }

    [JsonPropertyName("replyTo")]
    public string? ReplyTo { get; init; }

    [JsonPropertyName("expiration")]
    public string? Expiration { get; init; }

    [JsonPropertyName("messageId")]
    public string? MessageId { get; init; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("userId")]
    public string? UserId { get; init; }

    [JsonPropertyName("appId")]
    public string? AppId { get; init; }

    [JsonPropertyName("clusterId")]
    public string? ClusterId { get; init; }

    [JsonPropertyName("bodySize")]
    public int? BodySize { get; init; }
}
