# Stop on any error
$ErrorActionPreference = "Stop"

# Ensure we're in the infrastructure directory
Set-Location -Path $PSScriptRoot

Write-Host "Getting AWS account ID and region..."
$ACCOUNT_ID = (aws sts get-caller-identity --query Account --output text)
$REGION = (aws configure get region)

# Build and deploy CDK stack to create resources
Write-Host "Deploying CDK stack..."
npm run build
cdk deploy --require-approval never

# Get ECR repository URIs from stack outputs
Write-Host "Getting ECR repository URIs..."
$PUBLISHER_REPO = $(aws cloudformation describe-stacks --stack-name MessagingStack --query 'Stacks[0].Outputs[?OutputKey==`PublisherRepoUri`].OutputValue' --output text)
$SUBSCRIBER_REPO = $(aws cloudformation describe-stacks --stack-name MessagingStack --query 'Stacks[0].Outputs[?OutputKey==`SubscriberRepoUri`].OutputValue' --output text)

# Login to ECR
Write-Host "Logging into ECR..."
$(aws ecr get-login-password --region $REGION) | docker login --username AWS --password-stdin "$ACCOUNT_ID.dkr.ecr.$REGION.amazonaws.com"

# Build and push Publisher API
Write-Host "Building and pushing Publisher API..."
$publisherPath = Join-Path -Path $PSScriptRoot -ChildPath "..\sampleapps\PublisherAPI"
Set-Location -Path $publisherPath
docker build -t "${PUBLISHER_REPO}:latest" .
docker push "${PUBLISHER_REPO}:latest"

# Build and push Subscriber Service
Write-Host "Building and pushing Subscriber Service..."
$subscriberPath = Join-Path -Path $PSScriptRoot -ChildPath "..\sampleapps\SubscriberService"
Set-Location -Path $subscriberPath
docker build -t "${SUBSCRIBER_REPO}:latest" .
docker push "${SUBSCRIBER_REPO}:latest"

# Return to infrastructure directory
Set-Location -Path $PSScriptRoot

Write-Host "Deployment complete!"
Write-Host "Publisher API will be available at:"
aws cloudformation describe-stacks --stack-name MessagingStack --query 'Stacks[0].Outputs[?OutputKey==`PublisherUrl`].OutputValue' --output text
