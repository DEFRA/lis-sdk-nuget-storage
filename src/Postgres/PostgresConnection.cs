// <copyright file="PostgresConnection.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Storage.Postgres;

/// <summary>
/// Identifies one of the two connection pools. Also used as the service key for the
/// keyed <see cref="Npgsql.NpgsqlDataSource"/> registrations.
/// </summary>
public enum PostgresConnection
{
    /// <summary>
    /// The read-write pool, connected to the cluster's writer endpoint.
    /// </summary>
    ReadWrite,

    /// <summary>
    /// The read-only pool, connected to the cluster's reader endpoint.
    /// </summary>
    ReadOnly,
}
