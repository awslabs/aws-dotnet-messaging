// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Concurrent;

namespace AWS.Messaging.UnitTests.Models;

public class ChatMessage
{
    public string MessageDescription { get; set; } = string.Empty;
}

public class AddressInfo
{
    public int Unit { get; set; }
    public string? Street { get; set; }
    public string? ZipCode { get; set; }
}

public class PersonInfo
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public int Age { get; set; }
    public Gender Gender { get; set; }
    public AddressInfo? Address { get; set; }
}

public enum Gender
{
    Male,
    Female
}

public interface IGreeter
{
    string Greet();
}

public class Greeter : IGreeter
{
    public readonly string _message;

    public Greeter()
    {
        _message = Guid.NewGuid().ToString();
    }

    public string Greet()
    {
        return _message;
    }
}

public class TempStorage<T>
{
    public ConcurrentBag<MessageEnvelope<T>> Messages { get; set; } = new ConcurrentBag<MessageEnvelope<T>>();

    public ConcurrentQueue<MessageEnvelope<T>> FifoMessages { get; set; } = new ConcurrentQueue<MessageEnvelope<T>>();
}
