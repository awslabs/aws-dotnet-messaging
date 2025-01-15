// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.UnitTests.MessageHandlers;

public class ThingDoer : IThingDoer
{
    public string DoThing()
    {
        return "I did a thing!";
    }
}
