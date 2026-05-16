using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotificationsAPI.Application.Consumers;
using NotificationsAPI.Data;
using NotificationsAPI.Domain.Events;
using NotificationsAPI.IoC;
using Serilog;
using System.Text.Json;

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

    public async Task<SQSBatchResponse> FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        var batchFailures = new List<SQSBatchResponse.BatchItemFailure>();

        foreach (var record in sqsEvent.Records)
        {
            try
            {
                await ProcessRecordAsync(record, context);
            }
            catch (Exception ex)
            {
                context.Logger.LogLine(
                    $"Erro ao processar mensagem SQS | MessageId: {record.MessageId} | Queue: {GetQueueName(record)} | Erro: {ex}");

                batchFailures.Add(new SQSBatchResponse.BatchItemFailure
                {
                    ItemIdentifier = record.MessageId
                });
            }
        }

        return new SQSBatchResponse(batchFailures);
    }

    private static async Task ProcessRecordAsync(SQSEvent.SQSMessage record, ILambdaContext context)
    {
        var queueName = GetQueueName(record);

        context.Logger.LogLine(
            $"Mensagem recebida | MessageId: {record.MessageId} | Queue: {queueName} | Body: {record.Body}");

        using var scope = ServiceProvider.Value.CreateScope();

        switch (queueName)
        {
            case UserCreatedQueueName:
                var userCreatedEvent = Deserialize<UserCreatedEvent>(record.Body, record.MessageId);
                var userCreatedConsumer = scope.ServiceProvider.GetRequiredService<UserCreatedConsumer>();
                await userCreatedConsumer.ProcessAsync(userCreatedEvent);
                break;

            case PaymentProcessedQueueName:
                var paymentProcessedEvent = Deserialize<PaymentProcessedEvent>(record.Body, record.MessageId);
                var paymentProcessedConsumer = scope.ServiceProvider.GetRequiredService<PaymentProcessedConsumer>();
                await paymentProcessedConsumer.ProcessAsync(paymentProcessedEvent);
                break;

            default:
                throw new InvalidOperationException(
                    $"Fila SQS nao mapeada para NotificationsAPI.Function: {queueName}");
        }

        context.Logger.LogLine(
            $"Mensagem processada com sucesso | MessageId: {record.MessageId} | Queue: {queueName}");
    }

    private static T Deserialize<T>(string body, string messageId)
    {
        var message = JsonSerializer.Deserialize<T>(body, JsonOptions);

        return message is null
            ? throw new JsonException($"Mensagem SQS invalida ou vazia | MessageId: {messageId}")
            : message;
    }

    private static string GetQueueName(SQSEvent.SQSMessage record)
    {
        var eventSourceArn = record.EventSourceArn;

        if (string.IsNullOrWhiteSpace(eventSourceArn))
        {
            return string.Empty;
        }

        return eventSourceArn.Split(':').Last();
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
