# Generated backend

## Technology and emitted projects

The backend generator targets ASP.NET Core on .NET 10 and emits a CQRS-style API using MediatR and FluentValidation, with Entity Framework Core provider packages. The solution is generated at `backend/<ProjectName>.slnx` and currently contains these projects:

| Project | Generated responsibility |
|---|---|
| `Api` | ASP.NET Core host, controllers, startup, health endpoint, CQRS requests/handlers, and response models. |
| `Models` | Entity models. |
| `Repository` | EF Core context, provider setup, entity configuration, and repositories. |
| `Services` | Shared service contracts and implementations. |
| `SharedRepository` | Shared repository interfaces and paging/filter models. |
| `SharedWeb` | Shared API results, filters, and startup helpers. |
| `UnitTests` | Generated validation test project. |

The output is not organized as separate `Application`, `Domain`, and `Infrastructure` projects. The UI and some older architecture notes use that conventional Clean Architecture label, but it does not match the emitted solution structure. Describe the backend as a layered CQRS-oriented generated API rather than claiming those specific project boundaries.

## Per-entity API behavior

For each configured entity, the generator emits a pluralized controller with POST, GET by ID, GET list, PUT by ID, and DELETE by ID endpoints. It emits create/update/delete command handling and get/list query handling, with validators in the corresponding CQRS folders. List behavior uses generated repository paging/filter types; do not promise a particular SQL query count or search behavior without checking the generated implementation for the selected template.

The API maps health checks at `/health`. The generated project also includes an `ApiResponse<T>` contract and shared result types. Verify actual HTTP status codes and envelope behavior in the generated project before promising a specific wire format; controllers currently wrap mediator results in `ObjectResult`.

## Provider and version notes

The backend uses EF Core provider packages selected from provider metadata. SQL Server uses Microsoft EF Core provider 10.0.0, PostgreSQL uses Npgsql 10.0.0, MySQL uses Pomelo 9.0.0, and Oracle uses Oracle.EntityFrameworkCore 9.23.90. MySQL and Oracle therefore require an explicit compatibility review for a .NET 10 production target. Oracle metadata also says to confirm current .NET 10 support.

The generator emits a hand-authored initial migration and configures development Compose to apply migrations on startup. Review migrations and settings before production use; generated files are starting points, not a replacement for application review.

## Generated backend landmarks

- `backend/Api/Program.cs`, `StartupSetup.cs`
- `backend/Api/Controllers/`
- `backend/Api/CQRS/<Entity>/Commands/` and `Queries/`
- `backend/Models/Entities/`
- `backend/Repository/Context/AppDbContext.cs`
- `backend/Repository/Migrations/`
- `backend/UnitTests/`

## Architecture & Design Note

The generated backend is structured following layered Clean Architecture and CQRS patterns (`Api`, `Models`, `Repository`, `Services`, `SharedRepository`, `SharedWeb`, `UnitTests`) with MediatR, FluentValidation, and Entity Framework Core. Generated solutions compile and run completely independently as self-contained projects.
