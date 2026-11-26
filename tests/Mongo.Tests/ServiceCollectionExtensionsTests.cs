// <copyright file="ServiceCollectionExtensionsTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>
namespace Defra.Livestock.Sdk.Storage.Mongo.Tests;

using Defra.Livestock.Sdk.Storage.Mongo.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="ServiceCollectionExtensions.AddMongoDatabase"/> binds the settings and registers
    /// the client factory as a singleton.
    /// </summary>
    [Fact]
    public void AddMongoDatabase_Should_Register_Factory_And_Bind_Config()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new()
        {
            ["Mongo:DatabaseUri"] = "mongodb://localhost:27017",
            ["Mongo:DatabaseName"] = "test-db",
        });

        // Act
        services.AddMongoDatabase(configuration);
        using var serviceProvider = services.BuildServiceProvider();

        // Assert
        var config = serviceProvider.GetRequiredService<IOptions<MongoConfig>>().Value;
        config.DatabaseUri.ShouldBe("mongodb://localhost:27017");
        config.DatabaseName.ShouldBe("test-db");

        var factory = serviceProvider.GetRequiredService<IMongoDbClientFactory>();
        factory.ShouldBeOfType<MongoDbClientFactory>();
        factory.GetDatabase().DatabaseNamespace.DatabaseName.ShouldBe("test-db");
        serviceProvider.GetRequiredService<IMongoDbClientFactory>().ShouldBeSameAs(factory);
    }

    /// <summary>
    /// Verifies that missing required settings fail options validation.
    /// </summary>
    /// <param name="key">The setting to leave out.</param>
    [Theory]
    [InlineData("Mongo:DatabaseUri")]
    [InlineData("Mongo:DatabaseName")]
    public void AddMongoDatabase_Should_Fail_Validation_When_Setting_Missing(string key)
    {
        // Arrange
        var values = new Dictionary<string, string?>
        {
            ["Mongo:DatabaseUri"] = "mongodb://localhost:27017",
            ["Mongo:DatabaseName"] = "test-db",
        };
        values.Remove(key);
        var services = new ServiceCollection();
        services.AddMongoDatabase(CreateConfiguration(values));
        using var serviceProvider = services.BuildServiceProvider();

        // Act / Assert
        Should.Throw<OptionsValidationException>(
            () => serviceProvider.GetRequiredService<IOptions<MongoConfig>>().Value);
    }

    /// <summary>
    /// Verifies that calling <see cref="ServiceCollectionExtensions.AddMongoDatabase"/> twice registers the factory once.
    /// </summary>
    [Fact]
    public void AddMongoDatabase_Should_Not_Duplicate_Factory_Registration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new());

        // Act
        services.AddMongoDatabase(configuration);
        services.AddMongoDatabase(configuration);

        // Assert
        services.Count(d => d.ServiceType == typeof(IMongoDbClientFactory)).ShouldBe(1);
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
