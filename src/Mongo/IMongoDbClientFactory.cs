// <copyright file="IMongoDbClientFactory.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Storage.Mongo;

using MongoDB.Driver;

/// <summary>
/// Provides access to a configured MongoDB client, database and collections.
/// </summary>
public interface IMongoDbClientFactory
{
    /// <summary>
    /// Gets the underlying MongoDB client.
    /// </summary>
    /// <returns>The <see cref="IMongoClient"/> used to connect to the MongoDB server.</returns>
    IMongoClient GetClient();

    /// <summary>
    /// Gets the configured MongoDB database.
    /// </summary>
    /// <returns>The <see cref="IMongoDatabase"/> for the configured database name.</returns>
    IMongoDatabase GetDatabase();

    /// <summary>
    /// Gets a collection from the configured MongoDB database.
    /// </summary>
    /// <typeparam name="T">The document type stored in the collection.</typeparam>
    /// <param name="collection">The name of the collection.</param>
    /// <returns>The <see cref="IMongoCollection{TDocument}"/> for the named collection.</returns>
    IMongoCollection<T> GetCollection<T>(string collection);
}
