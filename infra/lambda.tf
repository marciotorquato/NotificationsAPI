# -----------------------------------------------------------------------------
# Lambda Function — runtime .NET 9 custom (provided.al2023)
# -----------------------------------------------------------------------------

resource "aws_lambda_function" "notifications" {
  function_name = var.function_name
  description   = "Processa UserCreatedEvent e PaymentProcessedEvent do RabbitMQ e persiste notificacoes no SQL Server."

  role    = aws_iam_role.lambda_execution.arn
  handler = "bootstrap"
  runtime = "provided.al2023"

  memory_size = var.lambda_memory_mb
  timeout     = var.lambda_timeout_sec

  # Codigo carregado a partir do S3
  s3_bucket        = aws_s3_bucket.lambda_artifacts.bucket
  s3_key           = aws_s3_object.lambda_package.key
  source_code_hash = filebase64sha256(var.lambda_package_path)

  environment {
    variables = {
      ConnectionStrings__MS_NotificationsAPI       = var.sql_connection_string
      DOTNET_SYSTEM_GLOBALIZATION_APPLOCALICU      = "72.1.0.3"
      ASPNETCORE_ENVIRONMENT                        = var.environment
    }
  }

  depends_on = [
    aws_iam_role_policy_attachment.basic_execution,
    aws_iam_role_policy_attachment.mq_execution,
    aws_s3_object.lambda_package,
  ]
}

# -----------------------------------------------------------------------------
# CloudWatch Log Group — retencao de 7 dias para logs da Lambda
# -----------------------------------------------------------------------------

resource "aws_cloudwatch_log_group" "notifications_logs" {
  name              = "/aws/lambda/${var.function_name}"
  retention_in_days = 7
}
