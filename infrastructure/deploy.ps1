# Stop on any error
$ErrorActionPreference = "Stop"

# Ensure we're in the infrastructure directory
Set-Location -Path $PSScriptRoot

Write-Host "Getting AWS account ID and region..."
$ACCOUNT_ID = (aws sts get-caller-identity --query Account --output text)
$REGION = (aws configure get region)

Write-Host "Using AWS Account: $ACCOUNT_ID in region: $REGION"

# Set repository names
$PUBLISHER_REPO_NAME = "messaging-demo-publisher"
$SUBSCRIBER_REPO_NAME = "messaging-demo-subscriber"
$PUBLISHER_REPO = "$ACCOUNT_ID.dkr.ecr.$REGION.amazonaws.com/$PUBLISHER_REPO_NAME"
$SUBSCRIBER_REPO = "$ACCOUNT_ID.dkr.ecr.$REGION.amazonaws.com/$SUBSCRIBER_REPO_NAME"

# Create ECR repositories if they don't exist
Write-Host "Creating ECR repositories if they don't exist..."
try { aws ecr create-repository --repository-name $PUBLISHER_REPO_NAME --region $REGION } catch { }
try { aws ecr create-repository --repository-name $SUBSCRIBER_REPO_NAME --region $REGION } catch { }

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

# Build and deploy CDK stack to create resources
Write-Host "Building and deploying CDK stack..."
npm run build
cdk deploy --require-approval never

Write-Host "Deployment complete!"
Write-Host "Publisher API will be available at:"
aws cloudformation describe-stacks --stack-name MessagingStack --query 'Stacks[0].Outputs[?OutputKey==`PublisherUrl`].OutputValue' --output text

Write-Host "You can check the App Runner service status in the AWS Console:"
Write-Host "https://$REGION.console.aws.amazon.com/apprunner/home?region=$REGION#/services"
