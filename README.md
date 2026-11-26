# lis-sdk-nuget-storage

Part of the Livestock Information Service (LIS), which lets livestock keepers, owners, agents and
delegates record and manage their animals.

This repository builds two small NuGet packages that give LIS .NET APIs a standard way to connect
to their data stores.

Used by: [lis-api-cattle](https://github.com/DEFRA/lis-api-cattle)

## Packages

| Package | What it provides | Docs |
| ------- | ---------------- | ---- |
| [`Defra.Livestock.Sdk.Storage.Postgres`](https://www.nuget.org/packages/Defra.Livestock.Sdk.Storage.Postgres) | Read-write and read-only pooled `NpgsqlDataSource`s for the PostgreSQL cluster, optional AWS IAM authentication, and `AddPostgresDbContext` to put an EF Core `DbContext` on either pool | [README](https://github.com/DEFRA/lis-sdk-nuget-storage/blob/main/src/Postgres/README.md) |
| [`Defra.Livestock.Sdk.Storage.Mongo`](https://www.nuget.org/packages/Defra.Livestock.Sdk.Storage.Mongo) | `IMongoDbClientFactory`, camel-case BSON conventions and AWS IAM authentication, registered by `AddMongoDatabase` | [README](https://github.com/DEFRA/lis-sdk-nuget-storage/blob/main/src/Mongo/README.md) |

Each package's README is also shown on its NuGet page. Both target `net10.0`, and neither depends
on the other.

## Prerequisites

- .NET 10 SDK (the projects target `net10.0`). SDK selection and the Microsoft Testing Platform runner are configured in [global.json](https://github.com/DEFRA/lis-sdk-nuget-storage/blob/main/global.json).
- To try a change in a consuming service: a local clone of [lis-dev](https://github.com/DEFRA/lis-dev), which runs a local NuGet server

## Setup

### Development

```bash
dotnet restore Storage.slnx
dotnet build Storage.slnx
```

### Test

Tests use xUnit v3 on Microsoft Testing Platform, with Shouldly for assertions. There is nothing to
install beyond the .NET SDK. They do not need a PostgreSQL or MongoDB server.

## Running in development

This is a library, so it has no service of its own. To try a change in a consuming service before
publishing, pack it and push it to the local NuGet server that lis-dev runs on port 5555
(`nuget-server` in its compose stack, already listed in lis-dev's `nuget.config`):

```bash
dotnet pack Storage.slnx -c Release -o ./artifacts /p:Version=0.0.0-local.1
dotnet nuget push "./artifacts/*.nupkg" --source http://localhost:5555/v3/index.json --api-key LOCAL-NUGET-API-KEY
```

Then reference that version from the consuming service.

To run a service against a local database, lis-dev's compose stack runs PostgreSQL 16
(`postgres:16-alpine`).

## Running tests

Run the xUnit test executable directly after building:

```bash
dotnet build Storage.slnx
dotnet tests/Postgres.Tests/bin/Debug/net10.0/Defra.Livestock.Sdk.Storage.Postgres.Tests.dll
dotnet tests/Mongo.Tests/bin/Debug/net10.0/Defra.Livestock.Sdk.Storage.Mongo.Tests.dll
```

## Pipelines

GitHub Actions use the composite actions under `actions/` in `DEFRA/lis-github-actions`. Every workflow pins these actions to the `v0.10.0` release commit (`90a6d6da2ab2d9caaf7ac11ef878017b51c09fd2`). Update all `uses:` references together when moving to a newer release.

| Workflow | Trigger | Configured behaviour |
|---|---|---|
| [Prerelease](https://github.com/DEFRA/lis-sdk-nuget-storage/blob/main/.github/workflows/prerelease.yml) | Push to `feature/**` or `hotfix/**` | Calculate a prerelease version, validate and run Sonar analysis, then tag the commit when validation succeeds and the quality gate passes |
| [Pull Request Validation](https://github.com/DEFRA/lis-sdk-nuget-storage/blob/main/.github/workflows/pull-request.yml) | Pull request opened, updated or reopened against `main` | Calculate a prospective version, validate and run Sonar analysis; aggregate the job results into a required check |
| [Release](https://github.com/DEFRA/lis-sdk-nuget-storage/blob/main/.github/workflows/release.yml) | Pull request merged into `main` | Calculate a stable version, validate and run Sonar analysis, then tag the merged commit when validation succeeds and the quality gate passes |
| [Publish](https://github.com/DEFRA/lis-sdk-nuget-storage/blob/main/.github/workflows/publish.yml) | Push of a tag matching `*.*.*` | Run the central composite NuGet publishing action in a local job, publishing all packages |

NuGet's trusted publishing policy must use repository owner `DEFRA`, repository `lis-sdk-nuget-storage`, workflow filename `publish.yml`, and environment `release`. The `NUGET_USERNAME` secret identifies the policy creator. The publishing job stays in this repository because NuGet rejects authentication inside a reusable workflow hosted in another repository.

Publishing requires an artifact built from the tagged commit by a successful prerelease or release workflow. The artifact records its commit, version, workflow run and attempt; publishing waits for that attempt to finish successfully before authenticating with NuGet.

Dependabot branches receive patch versions. Their PR validation versions use the label `dependabot-alpha.<run>.<attempt>`; feature and hotfix prereleases require an LREG ticket.

## Branching policy

The repo uses mainline development. Branch from `main` and name the branch:

- `feature/LREG-<ticket>-<short-kebab-description>`
- `hotfix/LREG-<ticket>-<short-kebab-description>`

`LREG` must be uppercase, because the GitHub ruleset rejects other branch names. Rebase onto `main`
rather than merging `main` in.

## Contributing to this project

- Follow the [Defra software development standards](https://github.com/DEFRA/software-development-standards).
- Code style is configured in `.editorconfig` and `stylecop.json`, with StyleCop and .NET analyzers enabled during builds. Warnings are treated as errors. Run `dotnet format Storage.slnx --verify-no-changes` to check formatting locally.
- Open a pull request against `main`. CI must pass and the Sonar quality gate must be green before merge.

## Licence

THIS INFORMATION IS LICENSED UNDER THE CONDITIONS OF THE OPEN GOVERNMENT LICENCE found at:

<http://www.nationalarchives.gov.uk/doc/open-government-licence/version/3>

The following attribution statement MUST be cited in your products and applications when using this information.

> Contains public sector information licensed under the Open Government licence v3

### About the licence

The Open Government Licence (OGL) was developed by the Controller of His Majesty's Stationery Office (HMSO) to enable information providers in the public sector to license the use and re-use of their information under a common open licence.

It is designed to encourage use and re-use of information freely and flexibly, with only a few conditions.

## Maintaining code analysis and public APIs

The build runs the .NET 10 recommended analyzer rules, StyleCop and SonarAnalyzer. Tests also receive xUnit analyzers through `xunit.v3`. Analyzer warnings are treated as errors; CA1707 is disabled for test code to allow descriptive names with underscores.

Each library uses `Microsoft.CodeAnalysis.PublicApiAnalyzers` to track its public surface, including nullability and default parameter values. Released APIs are recorded in `src/<Project>/PublicAPI.Shipped.txt` and unreleased APIs in `src/<Project>/PublicAPI.Unshipped.txt`.

When intentionally changing the public API, review the analyzer diagnostic and update the unshipped baseline (the RS0016 code fix can add declarations). During release preparation, move released entries into the shipped baseline. Keep shipped declarations as the compatibility record; represent intentional removals using `*REMOVED*` entries in the unshipped file.
