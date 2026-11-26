// <copyright file="MongoDbClientFactoryTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>
namespace Defra.Livestock.Sdk.Storage.Mongo.Tests;

using Defra.Livestock.Sdk.Storage.Mongo.Configuration;
using Microsoft.Extensions.Options;

/// <summary>
/// Unit tests for <see cref="MongoDbClientFactory"/>. None of these connect to a server.
/// </summary>
public class MongoDbClientFactoryTests
{
    private const string Uri = "mongodb://localhost:27017";
    private const string DatabaseName = "test-db";

    /// <summary>
    /// Verifies that the constructor rejects an empty connection URI.
    /// </summary>
    /// <param name="uri">The invalid URI.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Should_Throw_When_Uri_Empty(string uri)
    {
        Should.Throw<ArgumentException>(() => CreateFactory(uri, DatabaseName));
    }

    /// <summary>
    /// Verifies that the constructor rejects an empty database name.
    /// </summary>
    /// <param name="databaseName">The invalid database name.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Should_Throw_When_DatabaseName_Empty(string databaseName)
    {
        Should.Throw<ArgumentException>(() => CreateFactory(Uri, databaseName));
    }

    /// <summary>
    /// Verifies that the factory exposes the client, the configured database and its collections.
    /// </summary>
    [Fact]
    public void Factory_Should_Return_Client_Database_And_Collection()
    {
        // Arrange
        using var factory = CreateFactory(Uri, DatabaseName);

        // Act
        var client = factory.GetClient();
        var database = factory.GetDatabase();
        var collection = factory.GetCollection<object>("animals");

        // Assert
        client.ShouldNotBeNull();
        database.DatabaseNamespace.DatabaseName.ShouldBe(DatabaseName);
        collection.CollectionNamespace.FullName.ShouldBe($"{DatabaseName}.animals");
    }

    /// <summary>
    /// Verifies that disposing the factory more than once does not throw.
    /// </summary>
    [Fact]
    public void Dispose_Should_Be_Safe_To_Call_Twice()
    {
        // Arrange
        var factory = CreateFactory(Uri, DatabaseName);

        // Act / Assert
        Should.NotThrow(() =>
        {
            factory.Dispose();
            factory.Dispose();
        });
    }

    private static MongoDbClientFactory CreateFactory(string uri, string databaseName) =>
        new(Options.Create(new MongoConfig { DatabaseUri = uri, DatabaseName = databaseName }));
}
