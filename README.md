# NotificationsAPI - Serverless com RabbitMQ e LocalStack

Microsservico responsavel pelo envio de notificacoes da plataforma FIAP Cloud Games.

Nesta versao, a NotificationsAPI nao roda mais como um container de worker continuamente ativo. O processamento foi migrado para uma funcao serverless (`NotificationsAPI.Function`) publicada no LocalStack e acionada por mensagens do RabbitMQ.

## Tecnologias

- .NET 9
- AWS Lambda custom runtime no LocalStack
- RabbitMQ como broker de mensageria
- SQL Server para persistencia
- Serilog para logs

Observacao: referencias nao utilizadas de AutoMapper foram removidas para eliminar o alerta NuGet `NU1903` de vulnerabilidade no pacote `AutoMapper 12.0.1`.

## Arquitetura

```text
UsersAPI
  publica -> user-created-exchange
             -> user-created-queue-notifications
                -> trigger RabbitMQ
                   -> Lambda NotificationsAPI.Function

PaymentsAPI
  publica -> payment-processed-exchange
             -> payment-processed-queue-notifications
                -> trigger RabbitMQ
                   -> Lambda NotificationsAPI.Function
```

No AWS real, o desenho equivalente usa Lambda Event Source Mapping para Amazon MQ/RabbitMQ. No ambiente local, o LocalStack Community nao possui event source mapping nativo para RabbitMQ; por isso o repositorio inclui um emulador de trigger em `scripts/rabbitmq-lambda-trigger.js`. Ele consome as filas RabbitMQ, monta o payload padrao `aws:rmq` da Lambda e invoca a funcao no LocalStack. Assim, o container antigo da NotificationsAPI continua substituido por uma funcao serverless.

## Eventos Consumidos

| Exchange | Fila | Evento | Acao |
|---|---|---|---|
| `user-created-exchange` | `user-created-queue-notifications` | `UserCreatedEvent` | Notificacao de boas-vindas |
| `payment-processed-exchange` | `payment-processed-queue-notifications` | `PaymentProcessedEvent` | Notificacao de confirmacao de compra quando aprovado |

## Rodando Localmente

### Pre-requisitos

- Docker Desktop em execucao
- PowerShell
- Acesso para baixar imagens Docker na primeira execucao

### 1. Deploy da Lambda no LocalStack

```powershell
.\scripts\build-deploy-localstack.ps1
```

Esse script:

- sobe LocalStack, SQL Server e RabbitMQ;
- cria exchanges, filas e bindings no RabbitMQ;
- publica o projeto `src/NotificationsAPI.Function`;
- cria ou atualiza a Lambda `notifications-api-function` no LocalStack.
- configura a connection string da Lambda para usar SQL Server no Docker.

### 2. Iniciar o trigger RabbitMQ -> Lambda

```powershell
.\scripts\start-rabbitmq-lambda-trigger.ps1
```

Deixe esse processo aberto durante os testes locais. Ele representa o poller gerenciado que a AWS executa por tras do Event Source Mapping de Amazon MQ/RabbitMQ.

### 3. Enviar mensagens de teste

Em outro terminal:

```powershell
.\scripts\send-test-messages.ps1
```

O script publica mensagens nos exchanges RabbitMQ. O trigger local consome, invoca a Lambda e envia ACK somente quando a funcao processa com sucesso.

## SQL Server

A NotificationsAPI persiste as notificacoes no SQL Server usando Entity Framework Core.

- Container: `notifications-sqlserver`
- Porta local: `14333`
- Porta interna Docker: `1433`
- Banco: `MS_NotificationsAPI`
- Usuario: `sa`
- Senha: `Fiap@12345`

Connection string usada pela Lambda no LocalStack:

```text
Server=sqlserver,1433;Database=MS_NotificationsAPI;User Id=sa;Password=Fiap@12345;TrustServerCertificate=True
```

O nome `sqlserver` funciona porque a Lambda executa na rede Docker `fiap-notifications-local`.

## RabbitMQ

- Management UI: `http://localhost:15672`
- Usuario: `admin`
- Senha: `admin`

## Validacao do LocalStack

Verificar se o container esta ativo:

```powershell
docker ps --filter "name=localstack"
```

Verificar health dos servicos:

```powershell
Invoke-RestMethod -Uri http://localhost:4566/_localstack/health | ConvertTo-Json -Depth 5
```

Listar a Lambda publicada:

```powershell
docker exec localstack awslocal lambda list-functions
```

Resultado esperado:

```text
notifications-api-function
```

## Validacao do Build

Caso o .NET SDK nao esteja instalado localmente, o build pode ser validado via Docker:

```powershell
docker run --rm -v "${PWD}:/src" -w /src mcr.microsoft.com/dotnet/sdk:9.0 dotnet build NotificationsAPI.sln
```

Resultado esperado:

```text
Build succeeded.
```

Se o VS Code/C# Dev Kit mostrar erro de restore, confirme se o .NET 9 SDK esta instalado no Windows ou use o comando Docker acima.

## Observacao sobre LocalStack

O handler da funcao usa o payload oficial de RabbitMQ para Lambda:

```json
{
  "eventSource": "aws:rmq",
  "rmqMessagesByQueue": {
    "user-created-queue-notifications::/": [
      {
        "basicProperties": {},
        "redelivered": false,
        "data": "base64-do-json"
      }
    ]
  }
}
```

Isso mantem a funcao alinhada ao formato esperado por Amazon MQ/RabbitMQ em Lambda, enquanto o ambiente local usa LocalStack para execucao da funcao e RabbitMQ local como broker.
