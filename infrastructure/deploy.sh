#!/bin/bash
set -e

# Ensure we're in the infrastructure directory
cd "$(dirname "$0")"

# Get account ID and region
ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
REGION=$(aws configure get region)

echo "Using AWS Account: $ACCOUNT_ID in region: $REGION"

# Build and deploy CDK stack to create resources
echo "Building and deploying CDK stack..."
npm run build
cdk deploy --require-approval never

# Get ECR repository URIs from stack outputs
echo "Getting ECR repository URIs..."
PUBLISHER_REPO=$(aws cloudformation describe-stacks --stack-name MessagingStack --query 'Stacks[0].Outputs[?OutputKey==`PublisherRepoUri`].OutputValue' --output text)
SUBSCRIBER_REPO=$(aws cloudformation describe-stacks --stack-name MessagingStack --query 'Stacks[0].Outputs[?OutputKey==`SubscriberRepoUri`].OutputValue' --output text)

echo "Publisher Repository: $PUBLISHER_REPO"
echo "Subscriber Repository: $SUBSCRIBER_REPO"

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

echo "Deployment complete!"
echo "Publisher API will be available at:"
aws cloudformation describe-stacks --stack-name MessagingStack --query 'Stacks[0].Outputs[?OutputKey==`PublisherUrl`].OutputValue' --output text

echo "Waiting for App Runner services to update..."
echo "You can check the status in the AWS Console:"
echo "https://$REGION.console.aws.amazon.com/apprunner/home?region=$REGION#/services"
