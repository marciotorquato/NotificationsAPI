variable "aws_region" {
  type        = string
  default     = "us-east-1"
  description = "Regiao AWS onde os recursos serao provisionados."
}

variable "localstack_endpoint" {
  type        = string
  default     = "http://localhost:4566"
  description = "Endpoint do LocalStack para desenvolvimento local. Deixe vazio para usar AWS real."
}

variable "environment" {
  type        = string
  default     = "local"
  description = "Ambiente de execucao: local | dev | staging | prod."

  validation {
    condition     = contains(["local", "dev", "staging", "prod"], var.environment)
    error_message = "Valor permitido: local, dev, staging ou prod."
  }
}

variable "function_name" {
  type        = string
  default     = "notifications-api-function"
  description = "Nome da funcao Lambda."
}

variable "lambda_role_name" {
  type        = string
  default     = "notifications-api-lambda-role"
  description = "Nome da IAM Role de execucao da Lambda."
}

variable "s3_bucket_name" {
  type        = string
  default     = "notifications-api-lambda-artifacts"
  description = "Nome do bucket S3 usado para armazenar o pacote da Lambda."
}

variable "lambda_package_path" {
  type        = string
  default     = "../artifacts/lambda/notifications-api-function.zip"
  description = "Caminho local do arquivo .zip da Lambda (relativo ao diretorio infra/)."
}

variable "lambda_memory_mb" {
  type        = number
  default     = 512
  description = "Memoria alocada para a Lambda em MB."
}

variable "lambda_timeout_sec" {
  type        = number
  default     = 30
  description = "Timeout maximo da Lambda em segundos."
}

variable "sql_connection_string" {
  type        = string
  sensitive   = true
  default     = "Server=sqlserver,1433;Database=MS_NotificationsAPI;User Id=sa;Password=Senha@123;TrustServerCertificate=True"
  description = "Connection string do SQL Server para o banco MS_NotificationsAPI."
}
