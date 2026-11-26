# Defra.Livestock.Sdk.Storage.Mongo

MongoDB set-up for Livestock Information Service (LIS) .NET services: a client factory for the
configured database, camel-case BSON conventions, and AWS IAM authentication support.

Part of [lis-sdk-nuget-storage](https://github.com/DEFRA/lis-sdk-nuget-storage).

## Installation

```bash
dotnet add package Defra.Livestock.Sdk.Storage.Mongo
```

This package does not depend on the other storage packages. It targets `net10.0`.

## Usage

```csharp
using Defra.Livestock.Sdk.Storage.Mongo;

builder.Services.AddMongoDatabase(builder.Configuration);
```

`AddMongoDatabase` reads the `Mongo` section described under [Configuration](#configuration)
and:

- registers camel-case element names for all BSON-serialised types
- registers the MongoDB driver's AWS authentication extension
- binds and validates the settings, checked at start-up
- registers `IMongoDbClientFactory` as a singleton, which owns one `MongoClient` and disposes it
  when the container is disposed

```csharp
public class AnimalRepository(IMongoDbClientFactory factory)
{
    private readonly IMongoCollection<Animal> animals = factory.GetCollection<Animal>("animals");

    public Task<Animal?> FindAsync(Guid id) =>
        animals.Find(a => a.Id == id).FirstOrDefaultAsync();
}
```

Creating the factory does not connect to the server. The driver connects on first use.

## Configuration

```json
{
  "Mongo": {
    "DatabaseUri": "mongodb://localhost:27017",
    "DatabaseName": "my-db"
  }
}
```

| Key | Default | Meaning |
| --- | ------- | ------- |
| `Mongo:DatabaseUri` | none (required) | MongoDB connection string |
| `Mongo:DatabaseName` | none (required) | Database that `GetDatabase` and `GetCollection` use |

If either is missing, start-up fails with `OptionsValidationException`. To use AWS IAM
authentication, put `authMechanism=MONGODB-AWS` in the connection string. The driver then takes
credentials from the default AWS credentials chain.
