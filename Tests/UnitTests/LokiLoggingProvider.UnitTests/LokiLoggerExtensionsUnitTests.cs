namespace LokiLoggingProvider.UnitTests
{
    using System;
    using LokiLoggingProvider.Options;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Configuration;
    using Microsoft.Extensions.Options;
    using Xunit;

    public class LokiLoggerExtensionsUnitTests
    {
        [Fact]
        public void When_AddingLokiLogger_Expect_LokiLoggerAdded()
        {
            // Arrange
            ILoggingBuilder builder = new MockLoggingBuilder();

            // Act
            builder.AddLoki();

            // Assert
            IServiceProvider serviceProvider = builder.Services.BuildServiceProvider();

            Assert.NotNull(serviceProvider.GetService<IOptions<LokiLoggerOptions>>());
            Assert.NotNull(serviceProvider.GetService<IOptionsSnapshot<LokiLoggerOptions>>());
            Assert.NotNull(serviceProvider.GetService<IOptionsMonitor<LokiLoggerOptions>>());

            Assert.IsType<LokiLoggerProvider>(serviceProvider.GetService<ILoggerProvider>());

            Assert.Collection(
                builder.Services,
                serviceDescriptor =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
                    Assert.Equal(typeof(ILoggerProviderConfigurationFactory), serviceDescriptor.ServiceType);
                    Assert.NotNull(serviceDescriptor.ImplementationType);
                },
                serviceDescriptor =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
                    Assert.Equal(typeof(ILoggerProviderConfiguration<>), serviceDescriptor.ServiceType);
                    Assert.NotNull(serviceDescriptor.ImplementationType);
                },
                serviceDescriptor =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
                    Assert.Equal(typeof(IOptions<>), serviceDescriptor.ServiceType);
                    Assert.Equal(typeof(OptionsManager<>), serviceDescriptor.ImplementationType);
                },
                serviceDescriptor =>
                {
                    Assert.Equal(ServiceLifetime.Scoped, serviceDescriptor.Lifetime);
                    Assert.Equal(typeof(IOptionsSnapshot<>), serviceDescriptor.ServiceType);
                    Assert.Equal(typeof(OptionsManager<>), serviceDescriptor.ImplementationType);
                },
                serviceDescriptor =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
                    Assert.Equal(typeof(IOptionsMonitor<>), serviceDescriptor.ServiceType);
                    Assert.Equal(typeof(OptionsMonitor<>), serviceDescriptor.ImplementationType);
                },
                serviceDescriptor =>
                {
                    Assert.Equal(ServiceLifetime.Transient, serviceDescriptor.Lifetime);
                    Assert.Equal(typeof(IOptionsFactory<>), serviceDescriptor.ServiceType);
                    Assert.Equal(typeof(OptionsFactory<>), serviceDescriptor.ImplementationType);
                },
                serviceDescriptor =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
                    Assert.Equal(typeof(IOptionsMonitorCache<>), serviceDescriptor.ServiceType);
                    Assert.Equal(typeof(OptionsCache<>), serviceDescriptor.ImplementationType);
                },
                serviceDescriptor =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
                    Assert.Equal(typeof(ILoggerProvider), serviceDescriptor.ServiceType);
                    Assert.Equal(typeof(LokiLoggerProvider), serviceDescriptor.ImplementationType);
                },
                serviceDescriptor =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
                    Assert.Equal(typeof(IConfigureOptions<LokiLoggerOptions>), serviceDescriptor.ServiceType);
                    Assert.NotNull(serviceDescriptor.ImplementationType);
                },
                serviceDescriptor =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
                    Assert.Equal(typeof(IOptionsChangeTokenSource<LokiLoggerOptions>), serviceDescriptor.ServiceType);
                    Assert.NotNull(serviceDescriptor.ImplementationType);
                });
        }

        private class MockLoggingBuilder : ILoggingBuilder
        {
            public IServiceCollection Services { get; } = new ServiceCollection();
        }
    }
}
