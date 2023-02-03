// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Configuration;

/// <summary>
/// SQS implementation of <see cref="IMessagePublisherConfiguration"/>
/// </summary>
public class SQSPublisherConfiguration : IMessagePublisherConfiguration
{
    private readonly string _queueUrl;

    /// <summary>
    /// Creates an instance of <see cref="SQSPublisherConfiguration"/>.
    /// </summary>
    /// <param name="queueUrl">The SQS Queue URL</param>
    public SQSPublisherConfiguration(string queueUrl)
    {
        if (string.IsNullOrEmpty(queueUrl))
            throw new InvalidPublisherEndpointException("The SQS Queue URL cannot be empty.");

        _queueUrl = queueUrl;
    }

    /// <summary>
    /// Retrieves the SQS Queue URL which the publisher will use to route the message.
    /// </summary>
    /// <returns>SQS Queue URL</returns>
    public string GetPublisherEndpoint()
    {
        return _queueUrl;
    }
}
