# -----------------------------------------------------------------------------
# IAM Role — permissoes minimas para a Lambda executar e consumir Amazon MQ
# -----------------------------------------------------------------------------

resource "aws_iam_role" "lambda_execution" {
  name        = var.lambda_role_name
  description = "Role de execucao da Lambda NotificationsAPI."

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect    = "Allow"
        Principal = { Service = "lambda.amazonaws.com" }
        Action    = "sts:AssumeRole"
      }
    ]
  })
}

# Permite escrita de logs no CloudWatch
resource "aws_iam_role_policy_attachment" "basic_execution" {
  role       = aws_iam_role.lambda_execution.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

# Permite leitura de filas Amazon MQ (necessaria para o Event Source Mapping)
resource "aws_iam_role_policy_attachment" "mq_execution" {
  role       = aws_iam_role.lambda_execution.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaMQExecutionRole"
}
