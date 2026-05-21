output "lambda_function_name" {
  description = "Nome da funcao Lambda provisionada."
  value       = aws_lambda_function.notifications.function_name
}

output "lambda_function_arn" {
  description = "ARN da funcao Lambda provisionada."
  value       = aws_lambda_function.notifications.arn
}

output "lambda_role_arn" {
  description = "ARN da IAM Role de execucao."
  value       = aws_iam_role.lambda_execution.arn
}

output "s3_bucket_name" {
  description = "Nome do bucket S3 com os artefatos da Lambda."
  value       = aws_s3_bucket.lambda_artifacts.bucket
}

output "cloudwatch_log_group" {
  description = "Nome do Log Group no CloudWatch."
  value       = aws_cloudwatch_log_group.notifications_logs.name
}
