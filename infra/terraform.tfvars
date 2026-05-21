# =============================================================================
# terraform.tfvars — valores para o ambiente LOCAL (LocalStack)
#
# Para dev/prod: crie terraform.tfvars.dev / terraform.tfvars.prod
# e aplique com: terraform apply -var-file="terraform.tfvars.dev"
#
# ATENCAO: nunca commitar arquivos .tfvars com senhas de producao.
# =============================================================================

aws_region          = "us-east-1"
localstack_endpoint = "http://localhost:4566"
environment         = "local"

function_name    = "notifications-api-function"
lambda_role_name = "notifications-api-lambda-role"
s3_bucket_name   = "notifications-api-lambda-artifacts"

lambda_memory_mb   = 512
lambda_timeout_sec = 30

lambda_package_path = "../artifacts/lambda/notifications-api-function.zip"

sql_connection_string = "Server=sqlserver,1433;Database=MS_NotificationsAPI;User Id=sa;Password=Senha@123;TrustServerCertificate=True"
