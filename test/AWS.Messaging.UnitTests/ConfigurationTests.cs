// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using System.Threading.Tasks;
using AWS.Messaging.Configuration;
using Xunit;

namespace AWS.Messaging.UnitTests;

public class ConfigurationTests
{
    [Fact]
    public void SubscriberMappingNoMessageIdentifier()
    {
        var mapping = SubscriberMapping.Create<PurchaseOrderHandler, PurchaseOrder>();
        Assert.Equal("AWS.Messaging.UnitTests.ConfigurationTests+PurchaseOrder", mapping.MessageTypeIdentifier);
    }

    [Fact]
    public void SubscriberMappingWithMessageIdentifier()
    {
        var mapping = SubscriberMapping.Create<PurchaseOrderHandler, PurchaseOrder>("PO");
        Assert.Equal("PO", mapping.MessageTypeIdentifier);
    }

    public class PurchaseOrderHandler : IMessageHandler<PurchaseOrder>
    {
        public Task<MessageProcessStatus> HandleAsync(MessageEnvelope<PurchaseOrder> messageEnvelope, CancellationToken token = default)
        {
            return Task.FromResult(MessageProcessStatus.Success());
        }
    }

    public class PurchaseOrder
    {
        public string? Id { get; set; }
    }
}
