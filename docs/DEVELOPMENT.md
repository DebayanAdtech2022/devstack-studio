# Development guide

## Requirements

- .NET SDK `10.0.401`, pinned in `global.json` (roll forward is limited to the latest patch).
- Docker Engine/Desktop with Compose v2 only when running Docker integration or a generated stack.
- Node.js/npm only when working directly with the generated frontend.

## Build and launch

From the repository root:

```powershell
dotnet restore DevStackStudio.slnx
dotnet build DevStackStudio.slnx
dotnet run --project src/DevStackStudio.App/DevStackStudio.App.csproj
```

To run the existing test projects:

```powershell
dotnet test DevStackStudio.slnx
```

The solution contains Core, Generator, and Integration test projects. Running these commands is a separate verification step; documentation alone does not establish that the current checkout passes.

## Project structure and change locations

- Avalonia markup and UI: `src/DevStackStudio.App/Views/` and `App.axaml`.
- UI state/actions: `src/DevStackStudio.App/ViewModels/MainViewModel.cs`.
- Domain/configuration and validation: `src/DevStackStudio.Core/`.
- Host and Docker integrations: `src/DevStackStudio.Infrastructure/`.
- Generated outputs: `src/DevStackStudio.Generators/`.
- Provider metadata, catalog/template information: `src/DevStackStudio.Core/Providers/` and `src/DevStackStudio.Templates/`.
- Package versions: `Directory.Packages.props`; shared target framework and compiler settings: `Directory.Build.props`.

## Adding or changing a database provider

Implement `IDatabaseProvider`, register the provider in `DatabaseProviderCatalog`, and define provider metadata, supported architectures, image/tag, port, license requirement, type mappings, identifier quoting, connection strings, environment variables, and health-check command. Then update schema/Compose behavior as needed and add focused tests. Keep metadata claims aligned with tested image and EF provider versions.

## Adding a generator feature

The pipeline in `ProjectGenerator` currently combines root files, SQL schema, Compose, backend, and frontend outputs. Generated content is represented as `GeneratedFile` entries with relative paths. Use the existing path-safety validation and update generator tests to cover output shape and conflict behavior.

## Packaging caveat

`scripts/package.ps1` publishes six runtime identifiers and recreates `artifacts/dist` and `artifacts/staging/universal`. It copies `scripts/install.bat` and `scripts/install.command` into the universal bundle. The script also replaces published output folders and copies release archives into `web/downloads`; review these effects before running it. Packaging was not run as part of this documentation review.
