# -----------------------------------------------------------------------------
# S3 — bucket para armazenar o pacote .zip da Lambda
# -----------------------------------------------------------------------------

resource "aws_s3_bucket" "lambda_artifacts" {
  bucket        = var.s3_bucket_name
  force_destroy = true
}

# Upload do pacote compilado para o bucket
resource "aws_s3_object" "lambda_package" {
  bucket = aws_s3_bucket.lambda_artifacts.bucket
  key    = "notifications-api-function.zip"
  source = var.lambda_package_path
  etag   = filemd5(var.lambda_package_path)
}
