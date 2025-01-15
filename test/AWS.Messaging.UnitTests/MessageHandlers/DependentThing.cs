// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.UnitTests.MessageHandlers;

public class DependentThing : IDependentThing
{
    private readonly IThingDoer _ThingDoer;

    public DependentThing(IThingDoer thingDoer)
    {
        _ThingDoer = thingDoer;
    }


    public void DoThingWithThing()
    {
        _ThingDoer.DoThing();
    }
}
