// <copyright file="MongoConfig.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Storage.Mongo.Configuration;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Configuration settings for connecting to MongoDB.
/// </summary>
internal sealed class MongoConfig
{
    /// <summary>
    /// The configuration section name to bind these settings from.
    /// </summary>
    public const string SectionName = "Mongo";

    /// <summary>
    /// Gets the MongoDB connection string URI.
    /// </summary>
    [Required]
    public required string DatabaseUri { get; init; }

    /// <summary>
    /// Gets the name of the MongoDB database to use.
    /// </summary>
    [Required]
    public required string DatabaseName { get; init; }
}
