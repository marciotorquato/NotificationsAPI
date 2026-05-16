const amqp = require("amqplib");
const { LambdaClient, InvokeCommand } = require("@aws-sdk/client-lambda");

const rabbitUrl = process.env.RABBITMQ_URL || "amqp://admin:admin@rabbitmq:5672/%2F";
const lambdaEndpoint = process.env.LAMBDA_ENDPOINT || "http://localstack:4566";
const lambdaRegion = process.env.AWS_REGION || "us-east-1";
const functionName = process.env.LAMBDA_FUNCTION_NAME || "notifications-api-function";
const virtualHost = process.env.RABBITMQ_VIRTUAL_HOST || "/";
const queues = (process.env.RABBITMQ_QUEUES || [
  "user-created-queue-notifications",
  "payment-processed-queue-notifications",
].join(","))
  .split(",")
  .map((queue) => queue.trim())
  .filter(Boolean);

const lambda = new LambdaClient({
  endpoint: lambdaEndpoint,
  region: lambdaRegion,
  credentials: {
    accessKeyId: "test",
    secretAccessKey: "test",
  },
});

function timestamp() {
  return new Date().toISOString();
}

function log(message) {
  console.log(`[${timestamp()}] ${message}`);
}

function logError(message, error) {
  console.error(`[${timestamp()}] ${message}`, error);
}

function buildRabbitMqLambdaPayload(queue, message) {
  const body = message.content.toString("base64");
  const properties = message.properties || {};

  return {
    eventSource: "aws:rmq",
    eventSourceArn: `arn:aws:mq:${lambdaRegion}:000000000000:broker:local-rabbitmq:b-local`,
    rmqMessagesByQueue: {
      [`${queue}::${virtualHost}`]: [
        {
          basicProperties: {
            contentType: properties.contentType || "application/json",
            contentEncoding: properties.contentEncoding || null,
            deliveryMode: properties.deliveryMode || 2,
            priority: properties.priority || 0,
            correlationId: properties.correlationId || null,
            replyTo: properties.replyTo || null,
            expiration: properties.expiration || null,
            messageId: properties.messageId || null,
            timestamp: properties.timestamp ? new Date(properties.timestamp * 1000).toISOString() : null,
            type: properties.type || null,
            userId: properties.userId || null,
            appId: properties.appId || "localstack-rabbitmq-trigger",
            clusterId: null,
            bodySize: message.content.length,
          },
          redelivered: message.fields.redelivered,
          data: body,
        },
      ],
    },
  };
}

async function invokeLambda(queue, message) {
  const payload = buildRabbitMqLambdaPayload(queue, message);
  const command = new InvokeCommand({
    FunctionName: functionName,
    InvocationType: "RequestResponse",
    Payload: Buffer.from(JSON.stringify(payload)),
  });

  const response = await lambda.send(command);
  const functionError = response.FunctionError;
  const responsePayload = response.Payload ? Buffer.from(response.Payload).toString("utf8") : "";

  if (functionError) {
    throw new Error(`Lambda retornou erro (${functionError}): ${responsePayload}`);
  }

  if (response.StatusCode < 200 || response.StatusCode >= 300) {
    throw new Error(`Lambda retornou status ${response.StatusCode}: ${responsePayload}`);
  }
}

async function main() {
  const connection = await amqp.connect(rabbitUrl);
  const channel = await connection.createChannel();

  await channel.prefetch(1);

  for (const queue of queues) {
    await channel.assertQueue(queue, { durable: true });
    await channel.consume(
      queue,
      async (message) => {
        if (!message) {
          return;
        }

        try {
          log(`Mensagem recebida do RabbitMQ | Queue: ${queue}`);
          await invokeLambda(queue, message);
          channel.ack(message);
          log(`Mensagem processada pela Lambda e ACK enviada | Queue: ${queue}`);
        } catch (error) {
          logError(`Erro ao invocar Lambda para ${queue}:`, error);
          channel.nack(message, false, true);
        }
      },
      { noAck: false },
    );

    log(`Trigger RabbitMQ -> Lambda ativo | Queue: ${queue}`);
  }

  process.on("SIGTERM", async () => {
    await channel.close();
    await connection.close();
    process.exit(0);
  });
}

main().catch((error) => {
  logError("Erro fatal no trigger RabbitMQ -> Lambda:", error);
  process.exit(1);
});
