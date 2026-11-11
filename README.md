# DevStack Studio

**Cross-platform desktop project generator and Docker Compose manager**

DevStack Studio is an Avalonia desktop wizard for defining a project and database schema, generating an editable .NET API and Next.js frontend with database/Compose files, and optionally managing the generated services through Docker Compose. The desktop app targets .NET 10; it is separate from the generated project, which can be edited and used without the studio.

## What it currently includes

- Cross-platform Avalonia UI with an eight-step setup and schema designer.
- Host and Docker readiness checks.
- PostgreSQL, MySQL, SQL Server, and Oracle provider metadata and mappings.
- Generation of provider-specific schema SQL, Docker Compose files, backend CQRS-oriented code, and Next.js TypeScript pages.
- Core, generator, and integration test projects.
- A static download page and PowerShell packaging script.

The generated backend projects are `Api`, `Models`, `Repository`, `Services`, `SharedRepository`, `SharedWeb`, and `UnitTests`; the output is not split into `Application`, `Domain`, and `Infrastructure` projects. React + Vite appears in the catalog but is not implemented by the frontend generator. Check [the client demo guide](docs/CLIENT-DEMO-GUIDE.md) for further claims and caveats.

## Repository map

```text
src/DevStackStudio.App/             Avalonia desktop application
src/DevStackStudio.Core/            Models, providers, validation, contracts
src/DevStackStudio.Infrastructure/  Environment, process, Docker integration
src/DevStackStudio.Generators/      SQL, Compose, backend, frontend generation
tests/                              Core, generator, integration tests
web/                                Static downloads page
scripts/                            Packaging script
docs/                               Product and development guides
```

## Requirements and commands

Install .NET SDK `10.0.401` as pinned in `global.json`. Docker with Compose v2 is needed only to run a stack. From the repository root:

```powershell
dotnet restore DevStackStudio.slnx
dotnet build DevStackStudio.slnx
dotnet test DevStackStudio.slnx
dotnet run --project src/DevStackStudio.App/DevStackStudio.App.csproj
```

The packaging script is `scripts/package.ps1`. Review its effects and prerequisites before using it: it recreates artifact folders and currently refers to installer scripts that are not present in `scripts/`.

## Documentation

- [Client demonstration guide](docs/CLIENT-DEMO-GUIDE.md)
- [AXAML and Avalonia explanation](docs/AXAML-AND-AVALONIA.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Development](docs/DEVELOPMENT.md)
- [Backend generation](docs/BACKEND-GENERATION.md)
- [Database providers](docs/DATABASE-PROVIDERS.md)
- [Docker setup](docs/DOCKER-SETUP.md)
- [Security notes](docs/SECURITY.md)
