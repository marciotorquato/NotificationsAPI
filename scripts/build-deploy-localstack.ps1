param(
    [string]$Region = "us-east-1"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$packageDir = Join-Path $repoRoot "artifacts\lambda"
$packagePath = Join-Path $packageDir "notifications-api-function.zip"
$lambdaEnvironmentPath = Join-Path $packageDir "lambda-environment.json"
$trustPolicyPath = Join-Path $repoRoot "localstack\lambda-trust-policy.json"
$functionName = "notifications-api-function"
$roleName = "notifications-api-lambda-role"
$deploymentBucketName = "notifications-api-lambda-artifacts"
$deploymentKey = "notifications-api-function.zip"
$connectionString = "Server=sqlserver,1433;Database=MS_NotificationsAPI;User Id=sa;Password=Fiap@12345;TrustServerCertificate=True"

function Invoke-RabbitMqApi {
    param(
        [string]$Method,
        [string]$Path,
        [object]$Body = $null
    )

    $auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:admin"))
    $headers = @{ Authorization = "Basic $auth" }
    $uri = "http://localhost:15672/api/$Path"

    if ($null -eq $Body) {
        Invoke-RestMethod -Method $Method -Uri $uri -Headers $headers | Out-Null
        return
    }

    $jsonBody = $Body | ConvertTo-Json -Depth 10
    Invoke-RestMethod -Method $Method -Uri $uri -Headers $headers -ContentType "application/json" -Body $jsonBody | Out-Null
}

function Initialize-RabbitMq {
    Write-Host "Configurando RabbitMQ..."

    $ready = $false
    for ($attempt = 1; $attempt -le 30; $attempt++) {
        try {
            Invoke-RabbitMqApi -Method "GET" -Path "overview"
            $ready = $true
            break
        }
        catch {
            Start-Sleep -Seconds 2
        }
    }

    if (-not $ready) {
        throw "RabbitMQ Management API nao ficou pronta em tempo."
    }

    Invoke-RabbitMqApi -Method "PUT" -Path "exchanges/%2F/user-created-exchange" -Body @{
        type = "fanout"
        durable = $true
        auto_delete = $false
        arguments = @{}
    }

    Invoke-RabbitMqApi -Method "PUT" -Path "queues/%2F/user-created-queue-notifications" -Body @{
        durable = $true
        auto_delete = $false
        arguments = @{}
    }

    Invoke-RabbitMqApi -Method "POST" -Path "bindings/%2F/e/user-created-exchange/q/user-created-queue-notifications" -Body @{
        routing_key = ""
        arguments = @{}
    }

    Invoke-RabbitMqApi -Method "PUT" -Path "exchanges/%2F/payment-processed-exchange" -Body @{
        type = "fanout"
        durable = $true
        auto_delete = $false
        arguments = @{}
    }

    Invoke-RabbitMqApi -Method "PUT" -Path "queues/%2F/payment-processed-queue-notifications" -Body @{
        durable = $true
        auto_delete = $false
        arguments = @{}
    }

    Invoke-RabbitMqApi -Method "POST" -Path "bindings/%2F/e/payment-processed-exchange/q/payment-processed-queue-notifications" -Body @{
        routing_key = ""
        arguments = @{}
    }
}

docker compose -f (Join-Path $repoRoot "docker-compose.yml") up -d localstack sqlserver rabbitmq

Initialize-RabbitMq

if (Test-Path $packageDir) {
    Remove-Item -LiteralPath $packageDir -Recurse -Force
}

New-Item -ItemType Directory -Force $packageDir | Out-Null

$lambdaEnvironmentJson = @{
    Variables = @{
        "ConnectionStrings__MS_NotificationsAPI" = $connectionString
        "DOTNET_SYSTEM_GLOBALIZATION_APPLOCALICU" = "72.1.0.3"
    }
} | ConvertTo-Json -Depth 3
[System.IO.File]::WriteAllText($lambdaEnvironmentPath, $lambdaEnvironmentJson, [System.Text.UTF8Encoding]::new($false))

docker run --rm `
    -v "${repoRoot}:/src" `
    -w /src `
    mcr.microsoft.com/dotnet/sdk:9.0 `
    bash -lc "dotnet publish src/NotificationsAPI.Function/NotificationsAPI.Function.csproj -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true -o artifacts/lambda/publish && cd artifacts/lambda/publish && printf '#!/bin/sh\n./NotificationsAPI.Function\n' > bootstrap && chmod +x bootstrap NotificationsAPI.Function && apt-get update >/dev/null && apt-get install -y zip >/dev/null && zip -r ../notifications-api-function.zip . >/dev/null"

if ($LASTEXITCODE -ne 0) {
    throw "Falha ao publicar o projeto NotificationsAPI.Function."
}

if (-not (Test-Path $packagePath)) {
    throw "Pacote Lambda nao encontrado: $packagePath"
}

docker cp $packagePath localstack:/tmp/notifications-api-function.zip
if ($LASTEXITCODE -ne 0) {
    throw "Falha ao copiar o pacote Lambda para o container LocalStack."
}

docker exec localstack awslocal s3 mb "s3://$deploymentBucketName" 2>$null | Out-Null
docker exec localstack awslocal s3 cp /tmp/notifications-api-function.zip "s3://$deploymentBucketName/$deploymentKey" | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "Falha ao enviar o pacote Lambda para o S3 do LocalStack."
}

docker cp $trustPolicyPath localstack:/tmp/lambda-trust-policy.json
if ($LASTEXITCODE -ne 0) {
    throw "Falha ao copiar a trust policy para o container LocalStack."
}

docker cp $lambdaEnvironmentPath localstack:/tmp/lambda-environment.json
if ($LASTEXITCODE -ne 0) {
    throw "Falha ao copiar as variaveis de ambiente da Lambda para o container LocalStack."
}

docker exec localstack sh -c "awslocal iam get-role --role-name $roleName >/dev/null 2>&1 || awslocal iam create-role --role-name $roleName --assume-role-policy-document file:///tmp/lambda-trust-policy.json >/dev/null"
if ($LASTEXITCODE -ne 0) {
    throw "Falha ao criar/consultar a role IAM da Lambda no LocalStack."
}

$roleArn = docker exec localstack awslocal iam get-role --role-name $roleName --query "Role.Arn" --output text
if ($LASTEXITCODE -ne 0 -or -not $roleArn) {
    throw "Role IAM nao encontrada no LocalStack."
}

$functionExists = docker exec localstack sh -c "awslocal lambda get-function --function-name $functionName >/dev/null 2>&1 && echo exists || true"
if ($functionExists -eq "exists") {
    docker exec localstack awslocal lambda wait function-updated --function-name $functionName

    docker exec localstack awslocal lambda update-function-code `
        --function-name $functionName `
        --s3-bucket $deploymentBucketName `
        --s3-key $deploymentKey | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Falha ao atualizar o codigo da Lambda."
    }

    docker exec localstack awslocal lambda wait function-updated --function-name $functionName

    docker exec localstack awslocal lambda update-function-configuration `
        --function-name $functionName `
        --timeout 30 `
        --memory-size 512 `
        --environment file:///tmp/lambda-environment.json | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Falha ao atualizar a configuracao da Lambda."
    }
}
else {
    docker exec localstack awslocal lambda create-function `
        --function-name $functionName `
        --runtime provided.al2023 `
        --handler bootstrap `
        --role $roleArn `
        --code "S3Bucket=$deploymentBucketName,S3Key=$deploymentKey" `
        --timeout 30 `
        --memory-size 512 `
        --environment file:///tmp/lambda-environment.json | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Falha ao criar a Lambda no LocalStack."
    }
}

Write-Host "Deploy LocalStack concluido."
Write-Host "Funcao: $functionName"
Write-Host "RabbitMQ: user-created-exchange -> user-created-queue-notifications"
Write-Host "RabbitMQ: payment-processed-exchange -> payment-processed-queue-notifications"
Write-Host "Para emular o trigger RabbitMQ no LocalStack, execute: .\scripts\start-rabbitmq-lambda-trigger.ps1"
