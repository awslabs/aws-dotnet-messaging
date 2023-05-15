// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.Lambda.Core;
using AWS.Messaging.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Messaging.Lambda.Services.Internal;

/// <summary>
/// LambdaContextServiceHolder is used as a singleton service in the DI that holds onto the current ILambdaContext for the request. The
/// ILambdaContext is registered in the DI with a factory function that uses the LambdaContextServiceHolder and returns back the Context
/// property.
///
/// The <see cref="DefaultLambdaMessaging"/> takes care of taking the ILambdaContext that it was given for the invocation and setting
/// it on the LambdaContextServiceHolder registered in the DI.
/// </summary>
public class LambdaContextServiceHolder
{
    /// <summary>
    /// The current ILambdaContext for the function invocation.
    /// </summary>
    public ILambdaContext? Context { get; set; }

    internal static void Register(IMessageBusBuilder messageBusBuilder)
    {
        LambdaContextServiceHolder holder = new LambdaContextServiceHolder();
        messageBusBuilder.AddAdditionalService(new ServiceDescriptor(typeof(LambdaContextServiceHolder), holder));
        messageBusBuilder.AddAdditionalService(new ServiceDescriptor(typeof(ILambdaContext), provider =>
        {
            var holder = provider.GetRequiredService<LambdaContextServiceHolder>();
            return holder.Context!;
        }, ServiceLifetime.Transient));
    }
}
