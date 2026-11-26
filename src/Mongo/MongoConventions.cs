// <copyright file="MongoConventions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Storage.Mongo;

using MongoDB.Bson.Serialization.Conventions;

/// <summary>
/// Registers the BSON serialization conventions used by this library.
/// </summary>
internal static class MongoConventions
{
    private static readonly Lazy<bool> Registration = new(() =>
    {
        var conversions = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
        };

        ConventionRegistry.Register("CamelCase", conversions, _ => true);
        return true;
    });

    /// <summary>
    /// Registers a camel-case element name convention for all types.
    /// Safe to call multiple times; registration only happens once per process.
    /// </summary>
    public static void Register()
    {
        _ = Registration.Value;
    }
}
