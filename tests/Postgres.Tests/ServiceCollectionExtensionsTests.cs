// <copyright file="ServiceCollectionExtensionsTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>
namespace Defra.Livestock.Sdk.Storage.Postgres.Tests;

using Amazon.Runtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>. None of these connect to a database.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    private const string ReadWrite = "Host=rw-host;Database=test;Username=app;Password=secret";
    private const string ReadOnly = "Host=ro-host;Database=test;Username=app;Password=secret";

    /// <summary>
    /// Verifies that each pool is registered under its key with its own connection string.
    /// </summary>
    [Fact]
    public void AddPostgres_Should_Register_Keyed_Pools()
    {
        // Arrange
        using var provider = Build(new()
        {
            ["Postgres:ReadWriteConnectionString"] = ReadWrite,
            ["Postgres:ReadOnlyConnectionString"] = ReadOnly,
        });

        // Act
        var readWrite = provider.GetRequiredKeyedService<NpgsqlDataSource>(PostgresConnection.ReadWrite);
        var readOnly = provider.GetRequiredKeyedService<NpgsqlDataSource>(PostgresConnection.ReadOnly);

        // Assert
        readWrite.ConnectionString.ShouldContain("Host=rw-host");
        readOnly.ConnectionString.ShouldContain("Host=ro-host");
        readWrite.ShouldBeSameAs(provider.GetRequiredKeyedService<NpgsqlDataSource>(PostgresConnection.ReadWrite));
    }

    /// <summary>
    /// Verifies that the read-only pool uses the read-write connection string when none is set.
    /// </summary>
    [Fact]
    public void AddPostgres_Should_Fall_Back_To_ReadWrite_For_ReadOnly()
    {
        // Arrange
        using var provider = Build(new() { ["Postgres:ReadWriteConnectionString"] = ReadWrite });

        // Act
        var readOnly = provider.GetRequiredKeyedService<NpgsqlDataSource>(PostgresConnection.ReadOnly);

        // Assert
        readOnly.ConnectionString.ShouldContain("Host=rw-host");
    }

    /// <summary>
    /// Verifies that a missing read-write connection string fails options validation.
    /// </summary>
    [Fact]
    public void AddPostgres_Should_Fail_Validation_Without_ReadWrite_ConnectionString()
    {
        // Arrange
        using var provider = Build(new());

        // Act / Assert
        Should.Throw<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<PostgresOptions>>().Value);
    }

    /// <summary>
    /// Verifies that IAM authentication forces SSL.
    /// </summary>
    [Fact]
    public void AddPostgres_With_Iam_Should_Require_Ssl()
    {
        // Arrange
        using var provider = Build(
            new()
            {
                ["Postgres:ReadWriteConnectionString"] = "Host=rw-host;Database=test;Username=app",
                ["Postgres:UseIamAuthentication"] = "true",
                ["Postgres:AwsRegion"] = "eu-west-2",
            },
            services => services.AddSingleton<AWSCredentials>(new BasicAWSCredentials("key", "secret")));

        // Act
        var readWrite = provider.GetRequiredKeyedService<NpgsqlDataSource>(PostgresConnection.ReadWrite);

        // Assert
        new NpgsqlConnectionStringBuilder(readWrite.ConnectionString).SslMode.ShouldBe(SslMode.Require);
    }

    /// <summary>
    /// Verifies that IAM authentication needs a username in the connection string.
    /// </summary>
    [Fact]
    public void AddPostgres_With_Iam_Should_Throw_Without_Username()
    {
        // Arrange
        using var provider = Build(
            new()
            {
                ["Postgres:ReadWriteConnectionString"] = "Host=rw-host;Database=test",
                ["Postgres:UseIamAuthentication"] = "true",
                ["Postgres:AwsRegion"] = "eu-west-2",
            },
            services => services.AddSingleton<AWSCredentials>(new BasicAWSCredentials("key", "secret")));

        // Act / Assert
        Should.Throw<InvalidOperationException>(
            () => provider.GetRequiredKeyedService<NpgsqlDataSource>(PostgresConnection.ReadWrite));
    }

    /// <summary>
    /// Verifies that each context is put on the chosen pool, and that a read-only context does not track.
    /// </summary>
    [Fact]
    public void AddPostgresDbContext_Should_Use_Chosen_Pool()
    {
        // Arrange
        using var provider = Build(
            new()
            {
                ["Postgres:ReadWriteConnectionString"] = ReadWrite,
                ["Postgres:ReadOnlyConnectionString"] = ReadOnly,
            },
            services => services
                .AddPostgresDbContext<WriteContext>(PostgresConnection.ReadWrite)
                .AddPostgresDbContext<ReadContext>(PostgresConnection.ReadOnly));
        using var scope = provider.CreateScope();

        // Act
        var write = scope.ServiceProvider.GetRequiredService<WriteContext>();
        var read = scope.ServiceProvider.GetRequiredService<ReadContext>();

        // Assert
        write.Database.GetConnectionString().ShouldNotBeNull().ShouldContain("Host=rw-host");
        read.Database.GetConnectionString().ShouldNotBeNull().ShouldContain("Host=ro-host");
        write.ChangeTracker.QueryTrackingBehavior.ShouldBe(QueryTrackingBehavior.TrackAll);
        read.ChangeTracker.QueryTrackingBehavior.ShouldBe(QueryTrackingBehavior.NoTracking);
    }

    private static ServiceProvider Build(
        Dictionary<string, string?> values,
        Action<IServiceCollection>? configure = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddPostgres(configuration);
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// A context registered on the read-write pool.
    /// </summary>
    /// <param name="options">The context options.</param>
    public sealed class WriteContext(DbContextOptions<WriteContext> options) : DbContext(options);

    /// <summary>
    /// A context registered on the read-only pool.
    /// </summary>
    /// <param name="options">The context options.</param>
    public sealed class ReadContext(DbContextOptions<ReadContext> options) : DbContext(options);
}
