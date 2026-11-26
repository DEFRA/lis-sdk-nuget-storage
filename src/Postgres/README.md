# Defra.Livestock.Sdk.Storage.Postgres

Connection set-up for the PostgreSQL cluster used by Livestock Information Service (LIS) .NET
services. It registers two connection pools, read-write and read-only, and each service chooses
which one to use.

Part of [lis-sdk-nuget-storage](https://github.com/DEFRA/lis-sdk-nuget-storage).

## Installation

```bash
dotnet add package Defra.Livestock.Sdk.Storage.Postgres
```

Targets `net10.0`.

## Usage

### Register the pools

```csharp
using Defra.Livestock.Sdk.Storage.Postgres;

builder.Services.AddPostgres(builder.Configuration);
```

`AddPostgres` reads the `Postgres` section described under [Configuration](#configuration), checks
it at start-up, and registers two `NpgsqlDataSource` singletons keyed by `PostgresConnection`:

| Key | Connects with |
| --- | ------------- |
| `PostgresConnection.ReadWrite` | `ReadWriteConnectionString` |
| `PostgresConnection.ReadOnly` | `ReadOnlyConnectionString`, or `ReadWriteConnectionString` if that is not set |

Each `NpgsqlDataSource` is a connection pool. The container disposes both when the app shuts down.
Neither connects until it is first used.

### Use a pool directly (Npgsql or Dapper)

```csharp
public class HoldingQueries(
    [FromKeyedServices(PostgresConnection.ReadOnly)] NpgsqlDataSource db)
{
    public async Task<int> CountAsync(CancellationToken ct)
    {
        await using var command = db.CreateCommand("SELECT count(*) FROM holdings");
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct));
    }
}
```

### Use a pool with Entity Framework Core

Register each of your own contexts on the pool it should use:

```csharp
builder.Services
    .AddPostgres(builder.Configuration)
    .AddPostgresDbContext<CattleDbContext>(PostgresConnection.ReadWrite)
    .AddPostgresDbContext<CattleReadDbContext>(PostgresConnection.ReadOnly);
```

Each context:

- retries up to 5 times, waiting up to 10 seconds between tries, on errors Npgsql treats as transient
- times out commands after 60 seconds
- does not track entities if it is on the `ReadOnly` pool

Pass a third argument to configure anything else, for example
`options => options.AddInterceptors(auditInterceptor)`.

The read-only pool does not block writes itself. Point `ReadOnlyConnectionString` at the cluster's
reader endpoint and the database rejects them.

## Configuration

`AddPostgres` binds the `Postgres` section of `IConfiguration`, so you can use `appsettings.json`,
environment variables (`Postgres__ReadWriteConnectionString`) or any other provider.

### Password authentication

```json
{
  "Postgres": {
    "ReadWriteConnectionString": "Host=localhost;Port=5432;Database=my-db;Username=my-user;Password=my-password",
    "ReadOnlyConnectionString": "Host=localhost;Port=5432;Database=my-db;Username=my-user;Password=my-password"
  }
}
```

### AWS IAM authentication

Set `UseIamAuthentication` to `true` to connect to Amazon RDS or Aurora with short-lived IAM tokens.
Leave the password out of the connection strings. `Host`, `Port`, `Database` and `Username` still
come from them.

```json
{
  "Postgres": {
    "ReadWriteConnectionString": "Host=my-db.cluster-abc.eu-west-2.rds.amazonaws.com;Database=my-db;Username=my-iam-user",
    "ReadOnlyConnectionString": "Host=my-db.cluster-ro-abc.eu-west-2.rds.amazonaws.com;Database=my-db;Username=my-iam-user",
    "UseIamAuthentication": true,
    "AwsRegion": "eu-west-2"
  }
}
```

In this mode SSL is always required. Tokens are renewed every 10 minutes, and retried after 30
seconds if renewal fails. AWS credentials come from an `AWSCredentials` instance registered in
dependency injection, or otherwise from the default AWS credentials chain.

### Settings

| Key | Default | Meaning |
| --- | ------- | ------- |
| `Postgres:ReadWriteConnectionString` | none (required) | Connection string for the read-write pool |
| `Postgres:ReadOnlyConnectionString` | read-write connection string | Connection string for the read-only pool |
| `Postgres:UseIamAuthentication` | `false` | Use IAM tokens instead of a password |
| `Postgres:AwsRegion` | default AWS region chain | Region used to sign IAM tokens |

If `ReadWriteConnectionString` is missing, start-up fails with `OptionsValidationException`.
