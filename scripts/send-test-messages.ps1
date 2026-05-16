$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$messageDir = Join-Path $repoRoot "artifacts\messages"
$userCreatedPath = Join-Path $messageDir "user-created.json"
$paymentProcessedPath = Join-Path $messageDir "payment-processed.json"

if (Test-Path $messageDir) {
    Remove-Item -LiteralPath $messageDir -Recurse -Force
}

New-Item -ItemType Directory -Force $messageDir | Out-Null

$userId = [guid]::NewGuid()
$gameId = [guid]::NewGuid()

$userCreatedBody = @{
    usuarioId = $userId
} | ConvertTo-Json -Compress

$paymentProcessedBody = @{
    usuarioId = $userId
    gameId = $gameId
    status = "Aprovado"
    dataProcessamento = [DateTimeOffset]::UtcNow.ToString("O")
} | ConvertTo-Json -Compress

[System.IO.File]::WriteAllText($userCreatedPath, $userCreatedBody, [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText($paymentProcessedPath, $paymentProcessedBody, [System.Text.UTF8Encoding]::new($false))

docker cp $userCreatedPath localstack:/tmp/user-created.json
docker cp $paymentProcessedPath localstack:/tmp/payment-processed.json

docker exec localstack awslocal sqs send-message `
    --queue-url http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/user-created-queue-notifications `
    --message-body file:///tmp/user-created.json | Out-Null

docker exec localstack awslocal sqs send-message `
    --queue-url http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/payment-processed-queue-notifications `
    --message-body file:///tmp/payment-processed.json | Out-Null

Write-Host "Mensagens enviadas."
Write-Host "UsuarioId: $userId"
Write-Host "GameId: $gameId"
