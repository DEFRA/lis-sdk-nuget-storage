// <copyright file="ServiceCollectionExtensions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Storage.Mongo;

using Defra.Livestock.Sdk.Storage.Mongo.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Extension methods that register the MongoDB services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the BSON conventions and AWS authentication extension, binds and validates
    /// <see cref="MongoConfig"/> from the <see cref="MongoConfig.SectionName"/> configuration section,
    /// and registers <see cref="IMongoDbClientFactory"/> as a singleton.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to read the settings from.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddMongoDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        MongoConventions.Register();
        MongoExtensions.Register();

        services
            .AddOptions<MongoConfig>()
            .Bind(configuration.GetSection(MongoConfig.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddSingleton<IMongoDbClientFactory, MongoDbClientFactory>();

        return services;
    }
}
