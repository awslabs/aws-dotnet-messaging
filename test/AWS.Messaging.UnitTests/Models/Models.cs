// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

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
