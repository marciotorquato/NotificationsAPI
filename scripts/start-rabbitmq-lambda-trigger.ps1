$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$scriptPath = Join-Path $repoRoot "scripts\rabbitmq-lambda-trigger.js"

docker compose -f (Join-Path $repoRoot "docker-compose.yml") up -d localstack rabbitmq

docker run --rm `
    --name notifications-rabbitmq-lambda-trigger `
    --network fiap-notifications-local `
    -v "${scriptPath}:/app/rabbitmq-lambda-trigger.js:ro" `
    -e RABBITMQ_URL="amqp://admin:admin@rabbitmq:5672/%2F" `
    -e LAMBDA_ENDPOINT="http://localstack:4566" `
    -e AWS_REGION="us-east-1" `
    -e AWS_ACCESS_KEY_ID="test" `
    -e AWS_SECRET_ACCESS_KEY="test" `
    -e LAMBDA_FUNCTION_NAME="notifications-api-function" `
    node:22-alpine `
    sh -lc "mkdir -p /tmp/trigger && cp /app/rabbitmq-lambda-trigger.js /tmp/trigger/ && cd /tmp/trigger && npm init -y >/dev/null && npm install --silent amqplib @aws-sdk/client-lambda >/dev/null && node rabbitmq-lambda-trigger.js"
