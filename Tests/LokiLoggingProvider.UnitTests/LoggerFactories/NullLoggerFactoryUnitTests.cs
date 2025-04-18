#pragma warning disable IDISP016 // Don't use disposed instance
#pragma warning disable IDISP017 // Prefer using
#pragma warning disable S3966 // Don't call dispose more than once

namespace LokiLoggingProvider.UnitTests.LoggerFactories;

using System;
using LoggingProvider.Loki.Logger;
using LoggingProvider.Loki.LoggerFactories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public class NullLoggerFactoryUnitTests
{
    public class CreateLogger
    {
        [Fact]
        public void When_CreatingLogger_Expect_LoggerCreated()
        {
            // Arrange
            using ILokiLoggerFactory loggerFactory = new LoggingProvider.Loki.LoggerFactories.NullLoggerFactory();
            string categoryName = nameof(categoryName);

            // Act
            ILogger logger = loggerFactory.CreateLogger(categoryName);

            // Assert
            Assert.IsType<NullLogger>(logger);
        }

        [Fact]
        public void When_CreatingLoggerWithDisposedLoggerFactory_NoExceptions()
        {
            // Arrange
            ILokiLoggerFactory loggerFactory = new LoggingProvider.Loki.LoggerFactories.NullLoggerFactory();
            loggerFactory.Dispose();

            string categoryName = nameof(categoryName);

            // Act
            Exception result = Record.Exception(() => loggerFactory.CreateLogger(categoryName));

            // Assert
            Assert.Null(result);
        }
    }

    public class Dispose
    {
        [Fact]
        public void When_DisposingMoreThanOnce_Expect_NoExceptions()
        {
            // Arrange
            ILokiLoggerFactory loggerFactory = new LoggingProvider.Loki.LoggerFactories.NullLoggerFactory();

            // Act
            Exception result = Record.Exception(() =>
            {
                loggerFactory.Dispose();
                loggerFactory.Dispose();
            });

            // Assert
            Assert.Null(result);
        }
    }

    public class SetScopeProvider
    {
        [Fact]
        public void When_SettingScopeProvider_Expect_NoExceptions()
        {
            // Arrange
            using ILokiLoggerFactory loggerFactory = new LoggingProvider.Loki.LoggerFactories.NullLoggerFactory();

            // Act
            Exception result = Record.Exception(() => loggerFactory.SetScopeProvider(NullExternalScopeProvider.Instance));

            // Arrange
            Assert.Null(result);
        }
    }
}
