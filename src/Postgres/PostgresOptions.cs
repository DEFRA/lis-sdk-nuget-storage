// <copyright file="PostgresOptions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Storage.Postgres;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Settings for connecting to the PostgreSQL cluster.
/// </summary>
internal sealed class PostgresOptions
{
    /// <summary>
    /// The configuration section name to bind these settings from.
    /// </summary>
    public const string SectionName = "Postgres";

    /// <summary>
    /// Gets the connection string for the read-write pool.
    /// </summary>
    [Required]
    public string ReadWriteConnectionString { get; init; } = string.Empty;

    /// <summary>
    /// Gets the connection string for the read-only pool. When empty, the read-only pool uses
    /// <see cref="ReadWriteConnectionString"/>.
    /// </summary>
    public string? ReadOnlyConnectionString { get; init; }

    /// <summary>
    /// Gets a value indicating whether to sign in with short-lived AWS IAM tokens instead of the password in the
    /// connection string. When <see langword="true"/>, SSL is required.
    /// </summary>
    public bool UseIamAuthentication { get; init; }

    /// <summary>
    /// Gets the AWS region used to sign IAM tokens, for example <c>eu-west-2</c>. When empty, the region comes from
    /// the default AWS region chain.
    /// </summary>
    public string? AwsRegion { get; init; }
}
