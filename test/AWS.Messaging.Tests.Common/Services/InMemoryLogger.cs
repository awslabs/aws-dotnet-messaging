// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AWS.Messaging.Tests.Common.Services
{
    public record LogItem(LogLevel LogLevel, string Message, Exception? Exception);

    public class InMemoryLoggerProvider : ILoggerProvider
    {
        private readonly InMemoryLogger logger;

        public InMemoryLoggerProvider(InMemoryLogger logger) => this.logger = logger;

        public ILogger CreateLogger(string categoryName) => logger;

        public void Dispose() { }
    }

    public class InMemoryLogger : ILogger
    {
        private readonly LoggerExternalScopeProvider _scopeProvider;

        public InMemoryLogger(LoggerExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }

        private readonly ConcurrentBag<LogItem> concurrentLogs = new ConcurrentBag<LogItem>();

        public IEnumerable<LogItem> Logs => concurrentLogs.ToList();

        public IDisposable BeginScope<TState>(TState state)  where TState : notnull => _scopeProvider.Push(state);

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            concurrentLogs.Add(new LogItem(logLevel, formatter(state, exception), exception));
        }
    }

    public static class InMemoryLoggerExtensions
    {
        public static ILoggingBuilder AddInMemoryLogger(this ILoggingBuilder builder)
        {
            var logger = new InMemoryLogger(new LoggerExternalScopeProvider());
            builder.Services.AddSingleton(logger);
            return builder.AddProvider(new InMemoryLoggerProvider(logger));
        }
    }
}
