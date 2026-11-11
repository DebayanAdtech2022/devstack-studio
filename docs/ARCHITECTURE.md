# DevStack Studio architecture

## What the product does

DevStack Studio is a cross-platform Avalonia desktop wizard. It collects project, database, and entity-schema settings, validates them, and writes a separate editable project containing a database schema, Docker Compose files, a .NET API, and a Next.js frontend. The desktop app can then ask Docker Compose to start or stop that generated stack. The generated project is ordinary source code and does not depend on the studio at runtime.

## Repository projects

| Project | Responsibility |
|---|---|
| `src/DevStackStudio.App` | Avalonia desktop UI, MVVM view model, dependency registration, logging, and wizard workflow. |
| `src/DevStackStudio.Core` | Configuration/domain models, provider catalog, validation, serialization, and safety helpers. |
| `src/DevStackStudio.Infrastructure` | Host/environment detection, Docker probing and orchestration, process execution, and port checks. |
| `src/DevStackStudio.Generators` | Generation pipeline for SQL, Compose, backend, frontend, and root project files. |
| `src/DevStackStudio.Templates` | Template catalog/version metadata and Adtech template inspection result. |
| `tests/` | Core, generator, and end-to-end generation/build test projects. |
| `web/` | Static download page and packaged release downloads. |
| `scripts/` | PowerShell packaging script. |
| `docs/` | Product, architecture, development, and demonstration guides. |

The solution is `DevStackStudio.slnx`. Shared build settings are in `Directory.Build.props`; centrally managed package versions are in `Directory.Packages.props`.

## Runtime flow

1. `Program.cs` configures Serilog and starts Avalonia with platform detection and Inter font support.
2. `App.axaml` loads application resources and Fluent styling. `App.axaml.cs` registers services and creates `MainWindow` with `MainViewModel` as its data context.
3. `MainWindow.axaml` renders the eight wizard pages. The view model handles edits, checks, save/load, generation, and Docker actions.
4. `ProjectConfigurationValidator` and `SchemaValidator` check names, schema, provider support, licensing consent, and host compatibility.
5. `ProjectGenerator` composes root, SQL, Compose, backend, and frontend generators. It validates paths beneath the output directory and records generated-file hashes in `devstack-manifest.json`.
6. `DockerOrchestrator` invokes `docker compose` in the generated project's directory. The app probes Docker first to display readiness information.

## Generated project layout

The generator writes `README.md`, `.env.example`, `.env`, `.gitignore`, `devstack-project.json`, `devstack-manifest.json`, `database/schema.sql`, Compose files, a backend solution and projects, and a Next.js frontend. The actual generated backend project names are `Api`, `Models`, `Repository`, `Services`, `SharedRepository`, `SharedWeb`, and `UnitTests`; it is inaccurate to describe the emitted solution as separate `Application`, `Domain`, and `Infrastructure` projects. The generated frontend is Next.js App Router with TypeScript. React + Vite is mentioned in the catalog but the generator explicitly reports it as not implemented.

## Useful source landmarks

- UI: `src/DevStackStudio.App/Views/MainWindow.axaml`, `ViewModels/MainViewModel.cs`
- Startup and DI: `Program.cs`, `App.axaml`, `App.axaml.cs`
- Configuration and validation: `src/DevStackStudio.Core/Models/`, `Validation/`
- Database behavior: `src/DevStackStudio.Core/Providers/`
- Generation: `src/DevStackStudio.Generators/Pipeline/ProjectGenerator.cs`
- Compose and SQL: `Generators/Docker/ComposeGenerator.cs`, `Generators/Schema/SchemaSqlGenerator.cs`
- Backend and frontend: `Generators/Backend/BackendCqrsGenerator.cs`, `Generators/Frontend/NextJsFrontendGenerator.cs`
- Docker/process operations: `src/DevStackStudio.Infrastructure/`

## Important boundaries

- Docker is optional for configuring and generating a project; it is needed to run the generated stack.
- Generated `.env` contains credentials. Treat it as sensitive, keep it out of source control, and use a secrets manager for production deployments.
- `docker compose down` preserves named volumes. `docker compose down -v` deletes the database volume.
- The product is versioned `0.1.0` in shared build properties; describe it as an early release unless a release process says otherwise.
