# 🔔 NotificationsAPI — FCG FIAP Cloud Games

Microsserviço responsável pelo **envio de notificações** da plataforma FIAP Cloud Games. Não expõe endpoints HTTP — opera exclusivamente via eventos RabbitMQ, logando as notificações no console.

---

## 🧱 Tecnologias

- .NET 9
- SQL Server (dados relacionais)
- MongoDB (logs via Serilog)
- RabbitMQ (consumo de eventos)

---

## ⚡ Como Funciona

A NotificationsAPI é um serviço orientado a eventos. Ela não possui endpoints HTTP — todo o seu fluxo é acionado por mensagens do RabbitMQ. Ao receber um evento, loga a notificação no console (simulando o envio de e-mail).

```
UsersAPI
  └── publica → user-created-exchange
        └── NotificationsAPI consome → loga e-mail de boas-vindas

PaymentsAPI
  └── publica → payment-processed-exchange
        └── NotificationsAPI consome → loga resultado do pagamento
```

---

## 📨 Eventos Consumidos

| Exchange | Fila | Notificação Enviada |
|---|---|---|
| `user-created-exchange` | `user-created-queue-notifications` | E-mail de boas-vindas ao novo usuário |
| `payment-processed-exchange` | `payment-processed-queue-notifications` | E-mail com resultado do pagamento (aprovado ou rejeitado) |

---

## 🔄 Fluxo de Notificações

**Boas-vindas:**
1. Usuário se cadastra na UsersAPI
2. UsersAPI publica `UserCreatedEvent`
3. NotificationsAPI consome e loga e-mail de boas-vindas

**Resultado de pagamento:**
1. PaymentsAPI processa pagamento e publica `PaymentProcessedEvent`
2. NotificationsAPI consome e loga e-mail com resultado (aprovado ou rejeitado)

---

## 🗃️ Banco de Dados

| Configuração | Valor |
|---|---|
| Connection String | `MS_NotificationsAPI` |
| Database | `MS_NotificationsAPI` |

---

## 🐳 Rodando Localmente (Docker Compose)

Este serviço faz parte da orquestração central. Para rodar o ambiente completo:

```bash
# Clone todos os repositórios na mesma pasta pai
git clone https://github.com/pablosdlima/OrchestrationApi
git clone https://github.com/marciotorquato/NotificationsAPI

# Suba o ambiente
cd OrchestrationAPI
docker compose up --build
```

> ℹ️ Este serviço não possui Swagger pois não expõe endpoints HTTP. Para ver as notificações logadas, acompanhe os logs do container:

```bash
docker logs -f notifications-api
```

---

## ☸️ Rodando com Kubernetes

### Pré-requisitos

- Docker Desktop com **Kubernetes habilitado**
- `kubectl` disponível no terminal
- Infraestrutura já aplicada via OrchestrationAPI

### Estrutura dos manifestos

```
NotificationsAPI/
└── k8s/
    ├── configmap.yaml   ← variáveis não sensíveis
    ├── secret.yaml      ← variáveis sensíveis (Base64)
    ├── deployment.yaml  ← gerencia os Pods
    └── service.yaml     ← expõe o serviço na rede
```

### 1. Aplicar os manifestos

```bash
# Na raiz do repositório NotificationsAPI
kubectl apply -f k8s/
```

### 2. Verificar se está rodando

```bash
kubectl get pods
```

### 3. Monitorar as notificações

```bash
# Ver logs do serviço em tempo real
kubectl logs -f notifications-api-xxx

# Ou acesse o painel do RabbitMQ para ver os eventos
http://localhost:30072
```

> ℹ️ Substitua `notifications-api-xxx` pelo nome real do Pod obtido via `kubectl get pods`.

### Parar o serviço

```bash
kubectl delete -f k8s/
```

---

## 🎓 Contexto Acadêmico

Desenvolvido para o **Tech Challenge Fase 2 — PosTech FIAP**
Arquitetura de Software em .NET com Azure.