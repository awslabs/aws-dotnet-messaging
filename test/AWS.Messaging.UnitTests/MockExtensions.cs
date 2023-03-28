// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using Microsoft.Extensions.Logging;
using Moq;

namespace AWS.Messaging.UnitTests;

/// <summary>
/// Extensions for <see cref="Mock"/>
/// </summary>
public static class MockExtensions
{
    /// <summary>
    /// Verifies that Log was called on a mocked <see cref="ILogger"/> with given parameters
    /// </summary>
    /// <typeparam name="T">Generic type for the logger (which type you expect to log the message)</typeparam>
    /// <param name="mock">Mocked Logger</param>
    /// <param name="logLevel">Expected log level</param>
    /// <param name="exceptionType">Expected type of the exception that's being logged</param>
    /// <param name="message">Expected log message</param>
    public static void VerifyLog<T>(this Mock<ILogger<T>> mock, LogLevel logLevel, Type exceptionType, string message)
    {
        mock.Verify(x => x.Log(
            It.Is<LogLevel>(level => level == logLevel),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((@object, @type) => @object.ToString() == message && @type.Name == "FormattedLogValues"),
            It.Is<Exception>(exception => exception.GetType() == exceptionType),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that LogError was called on a mocked <see cref="ILogger"/> with given parameters
    /// </summary>
    /// <typeparam name="T">Generic type for the logger (which type you expect to log the message)</typeparam>
    /// <param name="mock">Mocked Logger</param>
    /// <param name="exceptionType">Expected type of the exception that's being logged</param>
    /// <param name="message">Expected log message</param>
    public static void VerifyLogError<T>(this Mock<ILogger<T>> mock, Type exceptionType, string message)
    {
        // We can't verify LogError directly because it's an extension method
        // see https://stackoverflow.com/a/66307704/557448
        mock.VerifyLog(LogLevel.Error, exceptionType, message);
    }
}
