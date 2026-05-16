# NotificationsAPI - FIAP Cloud Games

Microsservico responsavel pelo envio de notificacoes da plataforma FIAP Cloud Games.

A aplicacao foi migrada para uma arquitetura serverless: em vez de manter um container executando continuamente consumidores RabbitMQ, a `NotificationsAPI` agora processa eventos por meio de uma funcao Lambda local no LocalStack, acionada diretamente por mensagens em filas SQS.

## Arquitetura

```text
UsersAPI
  publica evento UserCreatedEvent
    -> SQS user-created-queue-notifications
      -> Lambda notifications-api-function
        -> UserCreatedConsumer
          -> registra/loga email de boas-vindas

PaymentsAPI
  publica evento PaymentProcessedEvent
    -> SQS payment-processed-queue-notifications
      -> Lambda notifications-api-function
        -> PaymentProcessedConsumer
          -> registra/loga email de confirmacao de compra
```

## Tecnologias

- .NET 9
- AWS Lambda local via LocalStack
- Amazon SQS local via LocalStack
- Amazon S3 local via LocalStack para armazenar o pacote da Lambda
- SQL Server em Docker
- Serilog para logs
- Docker Compose

## Projetos

| Projeto | Responsabilidade |
|---|---|
| `NotificationsAPI.Function` | Handler serverless acionado por eventos SQS |
| `NotificationsAPI.Application` | Consumers e regras de processamento |
| `NotificationsAPI.Domain` | Eventos, entidades e enums |
| `NotificationsAPI.Data` | DbContext, repositorios e migrations |
| `NotificationsAPI.IoC` | Registro de dependencias compartilhadas |
| `NotificationsAPI.Messaging` | Implementacao legada RabbitMQ, mantida para referencia/compatibilidade |
| `NotificationsAPI.Api` | Host HTTP legado, nao necessario para o fluxo serverless |

## Eventos Consumidos

| Fila SQS | Evento | Processamento |
|---|---|---|
| `user-created-queue-notifications` | `UserCreatedEvent` | Envia/loga email de boas-vindas |
| `payment-processed-queue-notifications` | `PaymentProcessedEvent` | Envia/loga confirmacao de compra quando o pagamento esta aprovado |

Exemplo `UserCreatedEvent`:

```json
{
  "usuarioId": "00000000-0000-0000-0000-000000000000"
}
```

Exemplo `PaymentProcessedEvent`:

```json
{
  "usuarioId": "00000000-0000-0000-0000-000000000000",
  "gameId": "11111111-1111-1111-1111-111111111111",
  "status": "Aprovado",
  "dataProcessamento": "2026-05-16T12:00:00Z"
}
```

## Pre-requisitos

- Docker Desktop rodando
- PowerShell
- Acesso a internet para baixar imagens Docker e pacotes NuGet no primeiro build

Nao e obrigatorio instalar .NET localmente para testar, pois o script de deploy usa a imagem Docker `mcr.microsoft.com/dotnet/sdk:9.0`.

## Rodando Localmente com LocalStack

Suba os servicos principais:

```powershell
docker compose up -d localstack sqlserver
```

Valide a saude do LocalStack:

```powershell
curl.exe http://localhost:4566/_localstack/health
```

O retorno deve indicar, entre outros, os servicos `lambda`, `sqs`, `s3`, `iam` e `logs` como disponiveis.

## Deploy da Funcao Serverless

Execute:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-deploy-localstack.ps1
```

Esse script:

- sobe `localstack` e `sqlserver`;
- publica o projeto `NotificationsAPI.Function`;
- empacota a funcao Lambda;
- envia o ZIP para o S3 local do LocalStack;
- cria/atualiza a Lambda `notifications-api-function`;
- cria as filas SQS;
- conecta as filas a Lambda via event source mapping.

Ao final, o esperado e:

```text
Deploy LocalStack concluido.
Funcao: notifications-api-function
Filas: user-created-queue-notifications, payment-processed-queue-notifications
```

## Enviando Mensagens de Teste

Execute:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\send-test-messages.ps1
```

O script envia uma mensagem para cada fila:

- `user-created-queue-notifications`
- `payment-processed-queue-notifications`

## Validando o Processamento

Veja os logs da funcao:

```powershell
docker logs localstack --tail 250 | Select-String "CONSUMER EXECUTADO|Mensagem processada|ENVIANDO"
```

Saida esperada:

```text
CONSUMER EXECUTADO | UsuarioId: ...
ENVIANDO E-MAIL DE CONFIRMACAO
Mensagem processada com sucesso | Queue: payment-processed-queue-notifications

CONSUMER EXECUTADO | UsuarioId: ...
ENVIANDO E-MAIL DE BOAS-VINDAS
Mensagem processada com sucesso | Queue: user-created-queue-notifications
```

Confirme se as filas ficaram vazias:

```powershell
docker exec localstack awslocal sqs get-queue-attributes --queue-url http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/user-created-queue-notifications --attribute-names ApproximateNumberOfMessages ApproximateNumberOfMessagesNotVisible

docker exec localstack awslocal sqs get-queue-attributes --queue-url http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/payment-processed-queue-notifications --attribute-names ApproximateNumberOfMessages ApproximateNumberOfMessagesNotVisible
```

O esperado para cada fila:

```json
{
  "Attributes": {
    "ApproximateNumberOfMessages": "0",
    "ApproximateNumberOfMessagesNotVisible": "0"
  }
}
```

## Teste do Zero

Para limpar volumes, filas, banco e recriar tudo:

```powershell
docker compose down -v
docker compose up -d localstack sqlserver
powershell -ExecutionPolicy Bypass -File scripts\build-deploy-localstack.ps1
powershell -ExecutionPolicy Bypass -File scripts\send-test-messages.ps1
docker logs localstack --tail 250 | Select-String "CONSUMER EXECUTADO|Mensagem processada|ENVIANDO"
```

## Comandos Uteis

Listar filas:

```powershell
docker exec localstack awslocal sqs list-queues
```

Listar funcoes Lambda:

```powershell
docker exec localstack awslocal lambda list-functions
```

Ver triggers da Lambda:

```powershell
docker exec localstack awslocal lambda list-event-source-mappings --function-name notifications-api-function
```

Ver objetos do bucket de deploy:

```powershell
docker exec localstack awslocal s3 ls s3://notifications-api-lambda-artifacts
```

## Observacoes

- O aviso `debconf: delaying package configuration, since apt-utils is not installed` durante o build e normal e pode ser ignorado.
- O pacote da Lambda e enviado por S3 local porque o ZIP self-contained do .NET pode ultrapassar o limite de upload direto de 50 MB.
- O SQL Server usado pela Lambda roda no Docker Compose como `sqlserver`, acessivel internamente pela connection string configurada no script de deploy.
- O fluxo RabbitMQ antigo foi substituido pelo trigger direto SQS -> Lambda para evitar container consumidor rodando continuamente.

## Contexto Academico

Projeto desenvolvido para o Tech Challenge da PosTech FIAP, demonstrando a migracao de um microsservico orientado a eventos para uma arquitetura de Funcoes como Servico com LocalStack.
