// <copyright file="MongoDbClientFactory.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Storage.Mongo;

using System.Diagnostics.CodeAnalysis;
using Defra.Livestock.Sdk.Storage.Mongo.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

/// <summary>
/// Default <see cref="IMongoDbClientFactory"/> implementation that creates a single
/// <see cref="MongoClient"/> from <see cref="MongoConfig"/> and resolves the configured database.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class MongoDbClientFactory : IMongoDbClientFactory, IDisposable
{
    private readonly IMongoDatabase mongoDatabase;
    private readonly MongoClient client;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDbClientFactory"/> class.
    /// </summary>
    /// <param name="config">The MongoDB configuration options.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="MongoConfig.DatabaseUri"/> or <see cref="MongoConfig.DatabaseName"/> is null, empty or whitespace.
    /// </exception>
    public MongoDbClientFactory(IOptions<MongoConfig> config)
    {
        var uri = config.Value.DatabaseUri;
        var databaseName = config.Value.DatabaseName;

        if (string.IsNullOrWhiteSpace(uri))
        {
            throw new ArgumentException("MongoDB uri string cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new ArgumentException("MongoDB database name cannot be empty");
        }

        var settings = MongoClientSettings.FromConnectionString(uri);
        client = new MongoClient(settings);
        mongoDatabase = client.GetDatabase(databaseName);
    }

    /// <inheritdoc/>
    public IMongoCollection<T> GetCollection<T>(string collection)
    {
        return mongoDatabase.GetCollection<T>(collection);
    }

    /// <inheritdoc/>
    public IMongoClient GetClient()
    {
        return client;
    }

    /// <inheritdoc/>
    public IMongoDatabase GetDatabase()
    {
        return mongoDatabase;
    }

    /// <summary>
    /// Disposes the underlying <see cref="MongoClient"/>.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        client.Dispose();
        disposed = true;
    }
}
