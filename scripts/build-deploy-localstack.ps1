param(
    [string]$Region = "us-east-1"
)

$ErrorActionPreference = "Stop"

$repoRoot    = Split-Path -Parent $PSScriptRoot
$packageDir  = Join-Path $repoRoot "artifacts\lambda"
$packagePath = Join-Path $packageDir "notifications-api-function.zip"
$infraDir    = Join-Path $repoRoot "infra"

# ------------------------------------------------------------------------------
# Detecta ferramenta de deploy disponivel
# Preferencia: tflocal > terraform > awslocal (fallback)
#
# tflocal   : wrapper do Terraform que injeta automaticamente os endpoints
#             do LocalStack. Instalacao: pip install terraform-local
#
# terraform : Terraform padrao. Os endpoints do LocalStack ja estao
#             configurados em infra/main.tf via variavel localstack_endpoint.
#
# awslocal  : fallback via AWS CLI direto no container LocalStack.
# ------------------------------------------------------------------------------
function Test-CommandWorks {
    param([string]$Cmd, [string]$Args = "version")
    try {
        $out = & $Cmd $Args 2>&1
        return $LASTEXITCODE -eq 0
    } catch { return $false }
}

$tflocalExists   = $null -ne (Get-Command tflocal   -ErrorAction SilentlyContinue)
$terraformExists = $null -ne (Get-Command terraform -ErrorAction SilentlyContinue)

# tflocal e um wrapper Python que exige o binario terraform instalado.
# Valida se realmente funciona antes de usar.
$tflocalWorks   = $tflocalExists   -and (Test-CommandWorks "tflocal"   "version")
$terraformWorks = $terraformExists -and (Test-CommandWorks "terraform" "version")

if ($tflocalWorks) {
    $tfCmd = "tflocal"
    Write-Host "[IaC] tflocal operante - deploy via Terraform + LocalStack."
} elseif ($tflocalExists -and -not $tflocalWorks) {
    Write-Host "[AVISO] tflocal encontrado mas nao funcionou."
    Write-Host "        O tflocal exige o Terraform instalado separadamente."
    Write-Host "        Instale: winget install HashiCorp.Terraform"
    if ($terraformWorks) {
        $tfCmd = "terraform"
        Write-Host "[IaC] Usando terraform diretamente (endpoints LocalStack em main.tf)."
    } else {
        $tfCmd = $null
        Write-Host "[AVISO] Terraform tambem nao encontrado. Usando awslocal como fallback."
    }
} elseif ($terraformWorks) {
    $tfCmd = "terraform"
    Write-Host "[IaC] terraform detectado - deploy via Terraform (endpoints LocalStack em main.tf)."
} else {
    $tfCmd = $null
    Write-Host "[AVISO] Terraform nao encontrado. Usando awslocal como fallback."
    Write-Host "        Para ativar o fluxo Terraform:"
    Write-Host "          winget install HashiCorp.Terraform"
    Write-Host "          pip install terraform-local"
}

# ------------------------------------------------------------------------------
# Funcoes auxiliares RabbitMQ
# ------------------------------------------------------------------------------

function Invoke-RabbitMqApi {
    param([string]$Method, [string]$Path, [object]$Body = $null)
    $auth    = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:admin"))
    $headers = @{ Authorization = "Basic $auth" }
    $uri     = "http://localhost:15672/api/$Path"
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
    for ($i = 1; $i -le 30; $i++) {
        try { Invoke-RabbitMqApi -Method "GET" -Path "overview"; $ready = $true; break }
        catch { Start-Sleep -Seconds 2 }
    }
    if (-not $ready) { throw "RabbitMQ nao ficou pronto em tempo." }

    Invoke-RabbitMqApi -Method "PUT" -Path "exchanges/%2F/user-created-exchange" -Body @{
        type = "fanout"; durable = $true; auto_delete = $false; arguments = @{}
    }
    Invoke-RabbitMqApi -Method "PUT" -Path "queues/%2F/user-created-queue-notifications" -Body @{
        durable = $true; auto_delete = $false; arguments = @{}
    }
    Invoke-RabbitMqApi -Method "POST" -Path "bindings/%2F/e/user-created-exchange/q/user-created-queue-notifications" -Body @{
        routing_key = ""; arguments = @{}
    }
    Invoke-RabbitMqApi -Method "PUT" -Path "exchanges/%2F/payment-processed-exchange" -Body @{
        type = "fanout"; durable = $true; auto_delete = $false; arguments = @{}
    }
    Invoke-RabbitMqApi -Method "PUT" -Path "queues/%2F/payment-processed-queue-notifications" -Body @{
        durable = $true; auto_delete = $false; arguments = @{}
    }
    Invoke-RabbitMqApi -Method "POST" -Path "bindings/%2F/e/payment-processed-exchange/q/payment-processed-queue-notifications" -Body @{
        routing_key = ""; arguments = @{}
    }
    Write-Host "RabbitMQ configurado."
}

# ==============================================================================
# ETAPA 1 - Subir infraestrutura compartilhada
# ==============================================================================
$orchestrationCompose = Join-Path $repoRoot "..\OrchestrationApi\docker-compose.yml"
docker compose -f $orchestrationCompose up -d localstack sqlserver rabbitmq

Initialize-RabbitMq

# ==============================================================================
# ETAPA 2 - Build do pacote Lambda via Docker
# Compila o projeto .NET 9 dentro de um container mcr.microsoft.com/dotnet/sdk:9.0
# garantindo compatibilidade com o runtime Lambda provided.al2023 (linux-x64).
# ==============================================================================
if (Test-Path $packageDir) {
    Remove-Item -LiteralPath $packageDir -Recurse -Force
}
New-Item -ItemType Directory -Force $packageDir | Out-Null

Write-Host "Compilando NotificationsAPI.Function..."
docker run --rm `
    -v "${repoRoot}:/src" `
    -w /src `
    mcr.microsoft.com/dotnet/sdk:9.0 `
    bash -lc "dotnet publish src/NotificationsAPI.Function/NotificationsAPI.Function.csproj -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true -o artifacts/lambda/publish && cd artifacts/lambda/publish && printf '#!/bin/sh\n./NotificationsAPI.Function\n' > bootstrap && chmod +x bootstrap NotificationsAPI.Function && apt-get update >/dev/null && apt-get install -y zip >/dev/null && zip -r ../notifications-api-function.zip . >/dev/null"

if ($LASTEXITCODE -ne 0) { throw "Falha ao compilar NotificationsAPI.Function." }
if (-not (Test-Path $packagePath)) { throw "Pacote Lambda nao encontrado: $packagePath" }
Write-Host "Build concluido: $packagePath"

# ==============================================================================
# ETAPA 3A - Deploy via Terraform (fluxo principal com IaC declarativo)
# O terraform.tfvars ja contem os valores para o ambiente local.
# O provider em main.tf aponta para LocalStack via variavel localstack_endpoint.
# ==============================================================================
if ($null -ne $tfCmd) {
    Write-Host "[Terraform] Inicializando providers em $infraDir..."
    Set-Location $infraDir

    & $tfCmd init -upgrade
    if ($LASTEXITCODE -ne 0) { throw "Falha no terraform init." }

    Write-Host "[Terraform] Aplicando infraestrutura (IAM role, S3 bucket, Lambda)..."
    & $tfCmd apply -auto-approve -var-file="terraform.tfvars"
    if ($LASTEXITCODE -ne 0) { throw "Falha no terraform apply." }

    Write-Host ""
    Write-Host "[Terraform] Outputs:"
    & $tfCmd output
}
# ==============================================================================
# ETAPA 3B - Deploy direto via awslocal (fallback sem Terraform instalado)
# Reproduz manualmente o que o Terraform declara em infra/:
# IAM role, bucket S3, upload do zip e criacao/atualizacao da Lambda.
# ==============================================================================
else {
    $functionName         = "notifications-api-function"
    $roleName             = "notifications-api-lambda-role"
    $deploymentBucketName = "notifications-api-lambda-artifacts"
    $deploymentKey        = "notifications-api-function.zip"
    $connectionString     = "Server=sqlserver,1433;Database=MS_NotificationsAPI;User Id=sa;Password=Senha@123;TrustServerCertificate=True"
    $trustPolicyPath      = Join-Path $repoRoot "localstack\lambda-trust-policy.json"
    $lambdaEnvPath        = Join-Path $packageDir "lambda-environment.json"

    $envJson = @{
        Variables = @{
            "ConnectionStrings__MS_NotificationsAPI"  = $connectionString
            "DOTNET_SYSTEM_GLOBALIZATION_APPLOCALICU" = "72.1.0.3"
            "ASPNETCORE_ENVIRONMENT"                   = "local"
        }
    } | ConvertTo-Json -Depth 3
    [System.IO.File]::WriteAllText($lambdaEnvPath, $envJson, [System.Text.UTF8Encoding]::new($false))

    docker cp $packagePath localstack:/tmp/notifications-api-function.zip
    docker exec localstack awslocal s3 mb "s3://$deploymentBucketName" 2>$null | Out-Null
    docker exec localstack awslocal s3 cp /tmp/notifications-api-function.zip "s3://$deploymentBucketName/$deploymentKey" | Out-Null

    docker cp $trustPolicyPath localstack:/tmp/lambda-trust-policy.json
    docker cp $lambdaEnvPath   localstack:/tmp/lambda-environment.json

    docker exec localstack sh -c "awslocal iam get-role --role-name $roleName >/dev/null 2>&1 || awslocal iam create-role --role-name $roleName --assume-role-policy-document file:///tmp/lambda-trust-policy.json >/dev/null"

    $roleArn = docker exec localstack awslocal iam get-role --role-name $roleName --query "Role.Arn" --output text
    if (-not $roleArn) { throw "Role IAM nao encontrada no LocalStack." }

    $exists = docker exec localstack sh -c "awslocal lambda get-function --function-name $functionName >/dev/null 2>&1 && echo exists || true"
    if ($exists -eq "exists") {
        docker exec localstack awslocal lambda wait function-updated --function-name $functionName
        docker exec localstack awslocal lambda update-function-code --function-name $functionName --s3-bucket $deploymentBucketName --s3-key $deploymentKey | Out-Null
        docker exec localstack awslocal lambda wait function-updated --function-name $functionName
        docker exec localstack awslocal lambda update-function-configuration --function-name $functionName --timeout 30 --memory-size 512 --environment file:///tmp/lambda-environment.json | Out-Null
    } else {
        docker exec localstack awslocal lambda create-function --function-name $functionName --runtime provided.al2023 --handler bootstrap --role $roleArn --code "S3Bucket=$deploymentBucketName,S3Key=$deploymentKey" --timeout 30 --memory-size 512 --environment file:///tmp/lambda-environment.json | Out-Null
    }
    Write-Host "[awslocal] Deploy concluido."
}

# ==============================================================================
# Resumo
# ==============================================================================
Write-Host ""
Write-Host "Deploy LocalStack concluido."
Write-Host "Funcao  : notifications-api-function"
Write-Host "IaC     : Terraform (infra/)"
Write-Host "Arquivos: infra/main.tf | variables.tf | iam.tf | s3.tf | lambda.tf | outputs.tf"
Write-Host ""
Write-Host "Proximo passo: .\scripts\start-rabbitmq-lambda-trigger.ps1"
