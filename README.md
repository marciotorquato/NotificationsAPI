# NotificationsAPI - Serverless com RabbitMQ e LocalStack

Microsservico responsavel pelo envio de notificacoes da plataforma FIAP Cloud Games.

Este repositorio contem **duas formas de execucao, de proposito**:

- **Producao/Cloud:** processamento serverless via **AWS Lambda** (`NotificationsAPI.Function`), acionado por mensagens do RabbitMQ. Este e o caminho oficial da arquitetura.
- **Dev/Teste local em Kubernetes:** o projeto `NotificationsAPI.Api` (worker tradicional em container) e os manifestos em `k8s/` permitem rodar o servico localmente sem depender do LocalStack, util para desenvolvimento e testes rapidos. Esse caminho **nao substitui** o serverless — e um modo alternativo, mais leve, apenas para ambiente local.

## Tecnologias

- .NET 9
- AWS Lambda custom runtime (producao) / LocalStack (simulacao local do Lambda)
- Kubernetes — execucao alternativa em container para dev/teste local
- RabbitMQ como broker de mensageria
- SQL Server para persistencia
- MongoDB (logs via Serilog)
- Terraform — provisionamento da infraestrutura Lambda/IAM/S3 na AWS
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

Existem duas formas de rodar o servico localmente — escolha conforme o objetivo.

### Opcao A — Serverless (LocalStack), simula o ambiente de producao

**Pre-requisitos**

- Docker Desktop em execucao
- PowerShell
- Acesso para baixar imagens Docker na primeira execucao

**1. Deploy da Lambda no LocalStack**

```powershell
.\scripts\build-deploy-localstack.ps1
```

Esse script:

- sobe LocalStack, SQL Server e RabbitMQ;
- cria exchanges, filas e bindings no RabbitMQ;
- publica o projeto `src/NotificationsAPI.Function`;
- cria ou atualiza a Lambda `notifications-api-function` no LocalStack.
- configura a connection string da Lambda para usar SQL Server no Docker.

**2. Iniciar o trigger RabbitMQ -> Lambda**

```powershell
.\scripts\start-rabbitmq-lambda-trigger.ps1
```

Deixe esse processo aberto durante os testes locais. Ele representa o poller gerenciado que a AWS executa por tras do Event Source Mapping de Amazon MQ/RabbitMQ.

**3. Enviar mensagens de teste**

Em outro terminal:

```powershell
.\scripts\send-test-messages.ps1
```

O script publica mensagens nos exchanges RabbitMQ. O trigger local consome, invoca a Lambda e envia ACK somente quando a funcao processa com sucesso.

### Opcao B — Container tradicional (Kubernetes), para dev/teste rapido

Alternativa mais leve para quem nao precisa simular o Lambda: sobe a `NotificationsAPI.Api` como um Deployment comum.

**Pre-requisitos**

- Docker Desktop com Kubernetes habilitado
- `kubectl` disponivel no terminal
- Infraestrutura base (RabbitMQ, SQL Server) ja aplicada via OrchestrationAPI

**Estrutura dos manifestos**

```
NotificationsAPI/
└── k8s/
    ├── configmap.yaml  ← variaveis nao sensiveis
    ├── secret.yaml     ← variaveis sensiveis (Base64) — repositorio deve conter apenas placeholders
    ├── deployment.yaml ← gerencia os Pods
    └── service.yaml    ← expoe o servico (NodePort, uso local apenas)
```

> 🔒 Assim como no UsersAPI, o `secret.yaml` deste repositorio nunca deve conter valores reais commitados. As credenciais reais sao aplicadas diretamente no cluster (`kubectl create secret`) ou geridas via GitHub Secrets/Azure Key Vault, nunca versionadas.

**Aplicar os manifestos**

```bash
kubectl apply -f k8s/
```

**Verificar se esta rodando**

```bash
kubectl get pods
kubectl get services
```

**Parar o servico**

```bash
kubectl delete -f k8s/
```

## SQL Server

A NotificationsAPI persiste as notificacoes no SQL Server usando Entity Framework Core.

- Container: `notifications-sqlserver`
- Porta local: `14333`
- Porta interna Docker: `1433`
- Banco: `MS_NotificationsAPI`
- Usuario: `sa`
- Senha: definida na variavel de ambiente `MSSQL_SA_PASSWORD` (ver `.env` local — nunca commitar o valor real)

Connection string usada pela Lambda no LocalStack:

```text
Server=sqlserver,1433;Database=MS_NotificationsAPI;User Id=sa;Password=<SUA_SENHA_AQUI>;TrustServerCertificate=True
```

> 🔒 O valor real da senha deve vir de uma variavel de ambiente/arquivo `.env` **ignorado pelo Git**, nunca escrito diretamente em `docker-compose.yml`, `appsettings.json` ou neste README.

O nome `sqlserver` funciona porque a Lambda executa na rede Docker `fiap-notifications-local`.

## RabbitMQ

- Management UI: `http://localhost:15672`
- Usuario: definido na variavel de ambiente `RABBITMQ_DEFAULT_USER`
- Senha: definida na variavel de ambiente `RABBITMQ_DEFAULT_PASS` (nunca commitar o valor real)

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

## 🎓 Contexto Academico

Desenvolvido para o **Tech Challenge — PosTech FIAP**, Arquitetura de Software em .NET com Azure.
