terraform {
  required_version = ">= 1.6"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

# -----------------------------------------------------------------------------
# Provider AWS
#
# Ambiente local  : localstack_endpoint = "http://localhost:4566"
#                   access_key / secret_key = "test"
#                   skip_* = true
#
# Ambiente real   : localstack_endpoint = ""
#                   Credenciais via variavel de ambiente AWS_ACCESS_KEY_ID /
#                   AWS_SECRET_ACCESS_KEY ou perfil ~/.aws/credentials
# -----------------------------------------------------------------------------
provider "aws" {
  region     = var.aws_region
  access_key = var.localstack_endpoint != "" ? "test" : null
  secret_key = var.localstack_endpoint != "" ? "test" : null

  skip_credentials_validation = var.localstack_endpoint != ""
  skip_metadata_api_check     = var.localstack_endpoint != ""
  skip_requesting_account_id  = var.localstack_endpoint != ""

  dynamic "endpoints" {
    for_each = var.localstack_endpoint != "" ? [var.localstack_endpoint] : []
    content {
      lambda = endpoints.value
      iam    = endpoints.value
      s3     = endpoints.value
      logs   = endpoints.value
    }
  }
}
