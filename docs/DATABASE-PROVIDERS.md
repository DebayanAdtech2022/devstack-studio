# Database providers

Provider metadata lives in `src/DevStackStudio.Core/Providers/`. The selected provider determines the container image/tag, default host port/user, supported architectures, license prompt, type mappings, SQL quoting, environment variables, health check, and EF Core package.

| Provider | Image tag in source | Default host port | Architecture metadata | EF Core provider |
|---|---|---:|---|---|
| PostgreSQL | `postgres:16.6` | 5432 | x64, ARM64 | `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.0 |
| MySQL | `mysql:8.4.3` | 3306 | x64, ARM64 | `Pomelo.EntityFrameworkCore.MySql` 9.0.0 |
| SQL Server | `mcr.microsoft.com/mssql/server:2022-CU16-ubuntu-22.04` | 1433 | x64 | `Microsoft.EntityFrameworkCore.SqlServer` 10.0.0 |
| Oracle | `container-registry.oracle.com/database/free:23.5.0.0` | 1521 | x64 | `Oracle.EntityFrameworkCore` 9.23.90 |

These are implementation defaults, not a guarantee of current upstream availability or continued vendor support. Confirm image tags, provider compatibility, and licensing at release time. MySQL's provider metadata explains that 9.0.0 is used because a GA 10.x provider was not verified when implemented. Oracle metadata asks users to confirm current .NET 10 compatibility and handles image pull/edition/architecture compatibility as user responsibilities.

## Licensing and platform checks

SQL Server and Oracle require explicit user acknowledgement in the wizard. SQL Server metadata includes `ACCEPT_EULA=Y` and its Developer edition (`MSSQL_PID=Developer`); treat this as a development-oriented configuration and verify the license/edition appropriate to your use. Oracle image pulls require an Oracle account and acceptance of Oracle terms. DevStack Studio does not redistribute Oracle binaries.

The compatibility checker uses provider architecture metadata to warn/block unsupported host/provider combinations. PostgreSQL and MySQL metadata include ARM64; SQL Server and Oracle are x64-only here. Do not infer that a local emulation path is officially supported.

## Logical types and SQL

The schema editor offers logical types (for example `uuid`, `string`, `decimal`, `timestamp`, and `json`). Providers map these to engine-specific SQL and CLR types. Identifiers are quoted according to each engine. `database/schema.sql` is generated as a convenient schema artifact; the generated backend's EF Core migration is described as the primary database evolution mechanism in the SQL output.

Some mappings are approximations: SQL Server JSON is represented as `nvarchar(max)`, Oracle boolean as `NUMBER(1)`, and UUID representations differ by engine. Review the emitted SQL and EF model for the selected provider.

## Ports and persistence

The Compose generator publishes database ports to localhost by default when `PortBindingMode.Localhost` is selected; configuration can remove the loopback binding. It uses a named volume for database files. `docker compose down` keeps that volume; `docker compose down -v` removes it and its data.
