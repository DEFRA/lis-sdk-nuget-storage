// <copyright file="ServiceCollectionExtensions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Storage.Postgres;

using Amazon;
using Amazon.RDS.Util;
using Amazon.Runtime;
using Amazon.Runtime.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

/// <summary>
/// Extension methods that register the PostgreSQL connection pools.
/// </summary>
public static class ServiceCollectionExtensions
{
    private const int MaxRetryCount = 5;
    private const int CommandTimeoutSeconds = 60;
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan TokenRefreshInterval = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan TokenRetryInterval = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Binds and validates <see cref="PostgresOptions"/> from the <see cref="PostgresOptions.SectionName"/>
    /// configuration section and registers a read-write and a read-only <see cref="NpgsqlDataSource"/> as keyed
    /// singletons, keyed by <see cref="PostgresConnection"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to read the settings from.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddPostgres(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<PostgresOptions>()
            .Bind(configuration.GetSection(PostgresOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddKeyedSingleton(
            PostgresConnection.ReadWrite,
            (sp, _) => CreateDataSource(sp, PostgresConnection.ReadWrite));
        services.AddKeyedSingleton(
            PostgresConnection.ReadOnly,
            (sp, _) => CreateDataSource(sp, PostgresConnection.ReadOnly));

        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="TContext"/> on the chosen connection pool, with retry on transient errors and
    /// a 60 second command timeout. A <see cref="PostgresConnection.ReadOnly"/> context does not track entities.
    /// Call <see cref="AddPostgres"/> first.
    /// </summary>
    /// <typeparam name="TContext">The <see cref="DbContext"/> to register.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="connection">The connection pool the context uses.</param>
    /// <param name="configure">Optional extra configuration for the context options.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddPostgresDbContext<TContext>(
        this IServiceCollection services,
        PostgresConnection connection,
        Action<DbContextOptionsBuilder>? configure = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDbContext<TContext>((sp, options) =>
        {
            options.UseNpgsql(
                sp.GetRequiredKeyedService<NpgsqlDataSource>(connection),
                npgsql =>
                {
                    npgsql.EnableRetryOnFailure(MaxRetryCount, MaxRetryDelay, errorCodesToAdd: null);
                    npgsql.CommandTimeout(CommandTimeoutSeconds);
                });

            if (connection == PostgresConnection.ReadOnly)
            {
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            }

            configure?.Invoke(options);
        });

        return services;
    }

    private static NpgsqlDataSource CreateDataSource(IServiceProvider sp, PostgresConnection connection)
    {
        var options = sp.GetRequiredService<IOptions<PostgresOptions>>().Value;

        var useReadOnly = connection == PostgresConnection.ReadOnly
                          && !string.IsNullOrWhiteSpace(options.ReadOnlyConnectionString);
        var connectionString = useReadOnly ? options.ReadOnlyConnectionString : options.ReadWriteConnectionString;

        var builder = new NpgsqlDataSourceBuilder(connectionString)
        {
            Name = connection.ToString(),
        };

        if (options.UseIamAuthentication)
        {
            UseIamAuthentication(sp, builder, options);
        }

        return builder.Build();
    }

    private static void UseIamAuthentication(
        IServiceProvider sp,
        NpgsqlDataSourceBuilder builder,
        PostgresOptions options)
    {
        var connectionStringBuilder = builder.ConnectionStringBuilder;
        connectionStringBuilder.SslMode = SslMode.Require;

        var host = connectionStringBuilder.Host
                   ?? throw new InvalidOperationException("The connection string must set Host for IAM authentication.");
        var username = connectionStringBuilder.Username
                       ?? throw new InvalidOperationException("The connection string must set Username for IAM authentication.");
        var port = connectionStringBuilder.Port;

        var credentials = sp.GetService<AWSCredentials>() ?? DefaultAWSCredentialsIdentityResolver.GetCredentials();
        var region = string.IsNullOrWhiteSpace(options.AwsRegion)
            ? FallbackRegionFactory.GetRegionEndpoint()
            : RegionEndpoint.GetBySystemName(options.AwsRegion);

        builder.UsePeriodicPasswordProvider(
            (_, _) => ValueTask.FromResult(
                RDSAuthTokenGenerator.GenerateAuthToken(credentials, region, host, port, username)),
            TokenRefreshInterval,
            TokenRetryInterval);
    }
}
