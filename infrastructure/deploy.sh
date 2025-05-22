#!/bin/bash
set -e

# Ensure we're in the infrastructure directory
cd "$(dirname "$0")"

# Get account ID and region
ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
REGION=$(aws configure get region)

echo "Using AWS Account: $ACCOUNT_ID in region: $REGION"

# Set repository names
PUBLISHER_REPO_NAME="messaging-demo-publisher"
SUBSCRIBER_REPO_NAME="messaging-demo-subscriber"
PUBLISHER_REPO="$ACCOUNT_ID.dkr.ecr.$REGION.amazonaws.com/$PUBLISHER_REPO_NAME"
SUBSCRIBER_REPO="$ACCOUNT_ID.dkr.ecr.$REGION.amazonaws.com/$SUBSCRIBER_REPO_NAME"

# Create ECR repositories if they don't exist
echo "Creating ECR repositories if they don't exist..."
aws ecr create-repository --repository-name $PUBLISHER_REPO_NAME --region $REGION || true
aws ecr create-repository --repository-name $SUBSCRIBER_REPO_NAME --region $REGION || true

# Login to ECR
echo "Logging into ECR..."
aws ecr get-login-password --region $REGION | docker login --username AWS --password-stdin $ACCOUNT_ID.dkr.ecr.$REGION.amazonaws.com

# Build and push Publisher API
echo "Building and pushing Publisher API..."
cd ../sampleapps/PublisherAPI
docker build -t $PUBLISHER_REPO:latest .
docker push $PUBLISHER_REPO:latest

# Build and push Subscriber Service
echo "Building and pushing Subscriber Service..."
cd ../SubscriberService
docker build -t $SUBSCRIBER_REPO:latest .
docker push $SUBSCRIBER_REPO:latest

# Return to infrastructure directory
cd ../../infrastructure

# Build and deploy CDK stack to create resources
echo "Building and deploying CDK stack..."
npm run build
cdk deploy --require-approval never

echo "Deployment complete!"
echo "Publisher API will be available at:"
aws cloudformation describe-stacks --stack-name MessagingStack --query 'Stacks[0].Outputs[?OutputKey==`PublisherUrl`].OutputValue' --output text

echo "You can check the App Runner service status in the AWS Console:"
echo "https://$REGION.console.aws.amazon.com/apprunner/home?region=$REGION#/services"
