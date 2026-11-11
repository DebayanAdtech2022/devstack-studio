# DevStack Studio client demonstration guide

## Purpose and suggested opening

DevStack Studio is a desktop wizard that turns a project and data model into an editable starter stack: a .NET API, a Next.js frontend, a database schema, and Docker Compose configuration. The app also checks the local environment and can start or stop the generated services through Docker Compose.

Suggested opening: “I’ll show how a project definition becomes a set of inspectable source files. We’ll review the environment check, choose the database, model a couple of entities, generate the project, and then—if Docker is ready—run the stack.”

## Before the meeting

- Open the solution `DevStackStudio.slnx` and build/run the desktop app in advance. The repository pins .NET SDK `10.0.401` in `global.json` and targets `net10.0`.
- Keep Docker Desktop/Engine and Compose v2 running if you plan to show orchestration. Docker is not required for a generation-only demo.
- Use a disposable output path, for example `C:\Users\<you>\DevStackDemo\CatalogDemo`. Generation writes files there and may overwrite generated files.
- Confirm free host ports. The wizard attempts to resolve a database port conflict, but the API/frontend ports can also conflict.
- Do not expose a real `.env` or credentials on screen. Generated `.env` is a sensitive local file.
- Build and run the app with the commands in `DEVELOPMENT.md`; for a client session, launch it beforehand and keep a file explorer ready for the output folder.

## 8–12 minute walkthrough

### 1. Welcome and environment (1 minute)

Point out the detected OS/runtime and Docker readiness summary. Explain that Docker has distinct states (CLI missing, daemon stopped, permission issue, Compose missing, ready). Use Recheck to show it can probe again. If Docker is unavailable, say that generation can still be demonstrated and skip the run step.

### 2. Project details (1 minute)

Set a simple project name such as `CatalogDemo`, and choose a fresh output folder. Explain that this is where the generated editable source tree will be written. The app falls back to a `DevStackProjects/<ProjectName>` path under the user profile if the output is blank.

### 3. Database provider (1 minute)

Show the four provider choices: PostgreSQL, MySQL, SQL Server, and Oracle. Explain that provider selection changes database type mapping, schema SQL, container image, health check, and connection configuration. SQL Server and Oracle require explicit license acceptance in the wizard. SQL Server and Oracle are marked x64-only by this project's provider metadata; MySQL's EF provider version is 9.0.0 and Oracle's is 9.23.90, so validate their current .NET 10 compatibility before production use.

### 4. Entity schema designer (2 minutes)

The default configuration is a sample catalog project. Add or edit an entity and its columns; demonstrate a primary key, a string with a length, nullable state, and (optionally) a foreign key. Types are logical choices mapped by the selected provider. Explain that each entity requires one primary key and that validation checks names, types, and relationships before generation.

### 5–6. Backend and frontend overview (1 minute)

These pages summarize what the generators intend to produce. Be precise: the backend output currently uses `Api`, `Models`, `Repository`, `Services`, `SharedRepository`, `SharedWeb`, and `UnitTests` projects. It is not emitted as the conventional `Application`/`Domain`/`Infrastructure` Clean Architecture project split. The frontend generator emits Next.js App Router/TypeScript pages and helpers. React + Vite is not implemented.

### 7. Generate and inspect (2 minutes)

Click Generate Complete Project and show the reported output path and manifest. In File Explorer, point to:

- `README.md` and `.env.example` for generated-project instructions
- `backend/` and `frontend/` for editable application code
- `database/schema.sql` for provider-specific DDL
- `docker-compose.yml` and `docker-compose.prod.yml` for service orchestration
- `devstack-project.json` for saved configuration and `devstack-manifest.json` for generated-file hashes

The generator validates configuration, builds the output file list, and checks generated paths stay beneath the selected output directory. The manifest is used to detect some modified generated files when conflict protection is enabled; avoid promising that it fully preserves every hand edit.

### 8. Docker orchestration (optional, 2–4 minutes)

If Docker is ready, click Start Stack, then inspect the displayed Compose output. Open the frontend at `http://localhost:<configured frontend port>` and the API health endpoint at `http://localhost:<configured backend port>/health` if those services build successfully. Stop Stack runs `docker compose down`, which keeps the named database volume. Do not use `down -v` unless you intend to remove its data.

If Docker or an image pull is unavailable, show the generated Compose file and describe how it is intended to run. Do not claim a live stack is healthy unless the containers actually start and the endpoint responds.

## Short source-code tour

| What to show | File |
|---|---|
| Desktop app startup and logging | `src/DevStackStudio.App/Program.cs` |
| App theme and Avalonia resources | `src/DevStackStudio.App/App.axaml` |
| Main wizard layout | `src/DevStackStudio.App/Views/MainWindow.axaml` |
| UI workflow and commands | `src/DevStackStudio.App/ViewModels/MainViewModel.cs` |
| Configuration model and validation | `src/DevStackStudio.Core/Models/`, `src/DevStackStudio.Core/Validation/` |
| Provider-specific mappings | `src/DevStackStudio.Core/Providers/` |
| Generation pipeline | `src/DevStackStudio.Generators/Pipeline/ProjectGenerator.cs` |
| Generated Compose and SQL | `src/DevStackStudio.Generators/Docker/ComposeGenerator.cs`, `Schema/SchemaSqlGenerator.cs` |
| Docker/process integration | `src/DevStackStudio.Infrastructure/` |

For a plain-language explanation of the UI markup and Avalonia packages, see [AXAML-AND-AVALONIA.md](AXAML-AND-AVALONIA.md).

## Accurate claims and known caveats

- Repository release/build version is `0.1.0`; avoid calling this a production-ready product without a separate acceptance/release review.
- The UI's backend summary labels the generated projects as `.Api`, `.Application`, `.Domain`, `.Infrastructure`, `.Tests`; the generator actually emits the project names listed above. This documentation describes the emitted files, but the UI copy should be corrected separately.
- The `docs` folder contains this guide and the guides linked from the root README; older references to SCHEMA-DESIGNER, FRONTEND-GENERATION, PROJECT-GENERATION, BUILD-AND-RELEASE, and TROUBLESHOOTING were removed because those files were not present in the repository.
- Packaging script `scripts/package.ps1` deletes/recreates `artifacts/dist` and the universal staging folder, then copies release archives into `web/downloads`. Do not run it during a client demo unless you intend to rebuild those artifacts. Installer scripts are present, but the packaging flow was not executed during this review.
- The project includes test projects, but a client demo should show only checks that have actually been run and passed in the current environment.
