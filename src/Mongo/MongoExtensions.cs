// <copyright file="MongoExtensions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Storage.Mongo;

using MongoDB.Driver;
using MongoDB.Driver.Authentication.AWS;

/// <summary>
/// Registers MongoDB driver extensions used by this library.
/// </summary>
internal static class MongoExtensions
{
    private static readonly Lazy<bool> Registration = new(() =>
    {
        MongoClientSettings.Extensions.AddAWSAuthentication();
        return true;
    });

    /// <summary>
    /// Registers the AWS authentication extension with the MongoDB driver.
    /// Safe to call multiple times; registration only happens once per process.
    /// </summary>
    public static void Register()
    {
        _ = Registration.Value;
    }
}
