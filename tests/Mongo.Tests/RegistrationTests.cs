// <copyright file="RegistrationTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>
namespace Defra.Livestock.Sdk.Storage.Mongo.Tests;

using MongoDB.Bson;

/// <summary>
/// Unit tests for <see cref="MongoConventions"/> and <see cref="MongoExtensions"/>.
/// </summary>
public class RegistrationTests
{
    /// <summary>
    /// Verifies that <see cref="MongoConventions.Register"/> can be called repeatedly and applies camel-case
    /// element names.
    /// </summary>
    [Fact]
    public void MongoConventions_Register_Should_Be_Idempotent_And_Apply_CamelCase()
    {
        // Act
        MongoConventions.Register();
        MongoConventions.Register();
        var document = new SampleDocument { AnimalName = "Daisy" }.ToBsonDocument();

        // Assert
        document.Contains("animalName").ShouldBeTrue();
        document.Contains("AnimalName").ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="MongoExtensions.Register"/> can be called repeatedly.
    /// </summary>
    [Fact]
    public void MongoExtensions_Register_Should_Be_Idempotent()
    {
        Should.NotThrow(() =>
        {
            MongoExtensions.Register();
            MongoExtensions.Register();
        });
    }

    /// <summary>
    /// A document type used to check the element name convention.
    /// </summary>
    public sealed class SampleDocument
    {
        /// <summary>
        /// Gets or sets the animal name.
        /// </summary>
        public string AnimalName { get; set; } = string.Empty;
    }
}
