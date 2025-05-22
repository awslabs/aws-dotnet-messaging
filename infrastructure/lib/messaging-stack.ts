import * as cdk from 'aws-cdk-lib';
import * as sqs from 'aws-cdk-lib/aws-sqs';
import * as dynamodb from 'aws-cdk-lib/aws-dynamodb';
import * as apprunner from 'aws-cdk-lib/aws-apprunner';
import * as iam from 'aws-cdk-lib/aws-iam';
import * as ecr from 'aws-cdk-lib/aws-ecr';
import { Construct } from 'constructs';

export class MessagingStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);

    // Create ECR repositories
    const publisherRepo = new ecr.Repository(this, 'PublisherRepo', {
      repositoryName: 'messaging-demo-publisher',
      removalPolicy: cdk.RemovalPolicy.DESTROY
    });

    const subscriberRepo = new ecr.Repository(this, 'SubscriberRepo', {
      repositoryName: 'messaging-demo-subscriber',
      removalPolicy: cdk.RemovalPolicy.DESTROY
    });

    // Create SQS Queue
    const queue = new sqs.Queue(this, 'MessagingQueue', {
      queueName: 'messaging-demo-queue',
      visibilityTimeout: cdk.Duration.seconds(30),
      removalPolicy: cdk.RemovalPolicy.DESTROY
    });

    // Create DynamoDB Table
    const table = new dynamodb.Table(this, 'MessagesTable', {
      tableName: 'messaging-demo-messages',
      partitionKey: { name: 'id', type: dynamodb.AttributeType.STRING },
      billingMode: dynamodb.BillingMode.PAY_PER_REQUEST,
      removalPolicy: cdk.RemovalPolicy.DESTROY
    });

    // Create IAM role for Publisher API
    const publisherRole = new iam.Role(this, 'PublisherRole', {
      assumedBy: new iam.ServicePrincipal('build.apprunner.amazonaws.com'),
    });

    // Grant SQS permissions to Publisher
    queue.grantSendMessages(publisherRole);

    // Create IAM role for Subscriber Service
    const subscriberRole = new iam.Role(this, 'SubscriberRole', {
      assumedBy: new iam.ServicePrincipal('build.apprunner.amazonaws.com'),
    });

    // Grant SQS and DynamoDB permissions to Subscriber
    queue.grantConsumeMessages(subscriberRole);
    table.grantWriteData(subscriberRole);

    // Create App Runner service for Publisher API
    const publisherService = new apprunner.CfnService(this, 'PublisherService', {
      sourceConfiguration: {
        autoDeploymentsEnabled: true,
        imageRepository: {
          imageIdentifier: `${publisherRepo.repositoryUri}:latest`,
          imageRepositoryType: 'ECR',
          imageConfiguration: {
            port: '80',
            runtimeEnvironmentVariables: [
              {
                name: 'AWS_SQS_QUEUE_URL',
                value: queue.queueUrl
              },
              {
                name: 'ASPNETCORE_ENVIRONMENT',
                value: 'Production'
              },
              {
                name: 'OTLP_ENDPOINT',
                value: 'http://52.12.96.156:4317'
              },
              {
                name: 'OTEL_RESOURCE_ATTRIBUTES',
                value: 'service.name=PublisherAPI'
              }
            ]
          }
        }
      },
      instanceConfiguration: {
        instanceRoleArn: publisherRole.roleArn
      }
    });

    // Create App Runner service for Subscriber Service
    const subscriberService = new apprunner.CfnService(this, 'SubscriberService', {
      sourceConfiguration: {
        autoDeploymentsEnabled: true,
        imageRepository: {
          imageIdentifier: `${subscriberRepo.repositoryUri}:latest`,
          imageRepositoryType: 'ECR',
          imageConfiguration: {
            port: '80',
            runtimeEnvironmentVariables: [
              {
                name: 'AWS_SQS_QUEUE_URL',
                value: queue.queueUrl
              },
              {
                name: 'DYNAMODB_TABLE_NAME',
                value: table.tableName
              },
              {
                name: 'ASPNETCORE_ENVIRONMENT',
                value: 'Production'
              },
              {
                name: 'OTLP_ENDPOINT',
                value: 'http://52.12.96.156:4317'
              },
              {
                name: 'OTEL_RESOURCE_ATTRIBUTES',
                value: 'service.name=SubscriberService'
              }
            ]
          }
        }
      },
      instanceConfiguration: {
        instanceRoleArn: subscriberRole.roleArn
      }
    });

    // Output the queue URL, table name, and ECR repository URIs
    new cdk.CfnOutput(this, 'QueueUrl', { value: queue.queueUrl });
    new cdk.CfnOutput(this, 'TableName', { value: table.tableName });
    new cdk.CfnOutput(this, 'PublisherUrl', { value: publisherService.attrServiceUrl });
    new cdk.CfnOutput(this, 'PublisherRepoUri', { value: publisherRepo.repositoryUri });
    new cdk.CfnOutput(this, 'SubscriberRepoUri', { value: subscriberRepo.repositoryUri });
  }
}
