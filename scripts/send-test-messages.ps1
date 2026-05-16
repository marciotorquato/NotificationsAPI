$ErrorActionPreference = "Stop"

$userId = [guid]::NewGuid()
$gameId = [guid]::NewGuid()

function Publish-RabbitMqMessage {
    param(
        [string]$Exchange,
        [string]$Payload
    )

    $auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:admin"))
    $headers = @{ Authorization = "Basic $auth" }
    $body = @{
        properties = @{
            content_type = "application/json"
            delivery_mode = 2
            message_id = [guid]::NewGuid().ToString()
        }
        routing_key = ""
        payload = $Payload
        payload_encoding = "string"
    } | ConvertTo-Json -Depth 10

    Invoke-RestMethod `
        -Method "POST" `
        -Uri "http://localhost:15672/api/exchanges/%2F/$Exchange/publish" `
        -Headers $headers `
        -ContentType "application/json" `
        -Body $body | Out-Null
}

$userCreatedBody = @{
    usuarioId = $userId
} | ConvertTo-Json -Compress

$paymentProcessedBody = @{
    usuarioId = $userId
    gameId = $gameId
    status = "Aprovado"
    dataProcessamento = [DateTimeOffset]::UtcNow.ToString("O")
} | ConvertTo-Json -Compress

Publish-RabbitMqMessage -Exchange "user-created-exchange" -Payload $userCreatedBody
Publish-RabbitMqMessage -Exchange "payment-processed-exchange" -Payload $paymentProcessedBody

Write-Host "Mensagens RabbitMQ enviadas."
Write-Host "UsuarioId: $userId"
Write-Host "GameId: $gameId"
