// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon;

#pragma warning disable CA2252 // This API requires opting into preview features

var builder = DistributedApplication.CreateBuilder(args);

var awsConfig = builder.AddAWSSDKConfig()
                        .WithProfile("default")
                        .WithRegion(RegionEndpoint.USWest2);


builder.AddAWSLambdaFunction<Projects.LambdaMessaging>(
    "LambdaMessaging",
    lambdaHandler: "LambdaMessaging::LambdaMessaging.Function_FunctionHandler_Generated::FunctionHandler")
    .WithReference(awsConfig);

var awsResources = builder.AddAWSCloudFormationTemplate("AspireSampleDevResources", "app-resources.template")
                          .WithReference(awsConfig);


builder.AddProject<Projects.PublisherAPI>("PublisherAPI")
       .WithReference(awsResources);

// Note: PollyIntegration and SubscriberService demonstrate different ways to process messages
// and should not run simultaneously. Comment/uncomment the appropriate line below to switch between them.
builder.AddProject<Projects.SubscriberService>("SubscriberService")
      .WithReference(awsResources);

// builder.AddProject<Projects.PollyIntegration>("PollyIntegration")
//        .WithReference(awsResources);


builder.Build().Run();
