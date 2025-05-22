# AWS Messaging Infrastructure

This directory contains the AWS CDK infrastructure code for deploying the messaging demo services.

## Architecture

The infrastructure consists of:
- SQS Queue for message passing
- DynamoDB Table for message storage
- ECR Repositories for container images
- App Runner Services for running the applications
- IAM Roles and permissions

## Prerequisites

1. AWS CLI configured with appropriate credentials
2. Node.js and npm installed
3. AWS CDK CLI installed (`npm install -g aws-cdk`)
4. Docker installed and running
5. OpenTelemetry Collector running and accessible

## Configuration

Before deploying, update the following:

1. In `lib/messaging-stack.ts`, replace the OpenTelemetry collector endpoint:
```typescript
OTLP_ENDPOINT: 'http://your-collector-endpoint:4317'
```

## Deployment

1. Install dependencies:
```bash
npm install
```

2. Bootstrap CDK (first time only):
```bash
cdk bootstrap
```

3. Deploy the infrastructure and services:
```bash
./deploy.sh
```

The deploy script will:
1. Deploy the CDK stack creating all AWS resources
2. Build and push Docker images to ECR
3. Deploy the services to App Runner

## Testing

After deployment, you can test the services:

1. Get the Publisher API URL:
```bash
aws cloudformation describe-stacks --stack-name MessagingStack \
  --query 'Stacks[0].Outputs[?OutputKey==`PublisherUrl`].OutputValue' \
  --output text
```

2. Send a test message:
```bash
curl -X POST "https://<publisher-url>/Publisher/chat" \
  -H "Content-Type: application/json" \
  -d '{"messageDescription":"Test message"}'
```

3. The message will be:
   - Published to SQS
   - Picked up by the Subscriber Service
   - Stored in DynamoDB

4. Check OpenTelemetry traces to observe the message flow through the system.

## Cleanup

To remove all resources:
```bash
cdk destroy
```

Note: This will delete all resources including the DynamoDB table and its data.
