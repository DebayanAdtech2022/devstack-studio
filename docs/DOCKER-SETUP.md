# Docker setup and generated stack

## What the desktop app checks

`DockerProbe` runs `docker version`, `docker info`, and `docker compose version`. It reports CLI missing, daemon stopped/unreachable, permission denied, Compose v2 missing, or ready. The categorization depends on the CLI's diagnostic text and may not perfectly classify every vendor/platform error.

Install Docker Desktop on Windows/macOS or Docker Engine plus the Compose v2 plugin on Linux. Ensure the daemon is started and the current user can access it. Docker is required to start the generated stack, but not to edit configuration or generate files.

## Generated Compose stack

`ComposeGenerator` emits `docker-compose.yml` and `docker-compose.prod.yml` with database, backend, and frontend services; a named database volume; and a configured bridge network. The backend waits for the database health check. The frontend depends on the backend being started, which is not the same as proving the API is ready.

The default Compose file binds published ports to `127.0.0.1` when local binding is selected. The generated `.env` stores DB credentials and `.env.example` is a sample. The `.env` file is gitignored but is not a vault: protect it and do not share it. Compose environment variables can be visible to users with Docker/host access.

The generated Compose service uses development settings and sets migrations to apply on startup. `docker-compose.prod.yml` is a minimal overlay, sets ASP.NET Core to Production and disables startup migrations; it is not a complete production deployment recipe. Review secrets, TLS, network exposure, health/readiness, resource limits, image pinning, and deployment policy before production.

## Running from a generated project

From the generated project folder:

```bash
docker compose up --build -d
docker compose ps
docker compose logs -f
docker compose down
```

The expected frontend default is `http://localhost:3000`; the API health path is `/health` on the configured backend port. Actual ports can be changed in the wizard. Confirm containers are healthy and inspect logs before telling a client the stack is running correctly.

`docker compose down` preserves named volume data. `docker compose down -v` deletes the database volume. Treat the latter as destructive.

## Common troubleshooting

- CLI missing: install Docker and ensure `docker` is on PATH.
- Daemon stopped: start Docker Desktop/Engine and wait until ready.
- Permission denied on Linux: configure Docker access according to your organization's policy; group membership can grant broad host privileges.
- Compose missing: install/enable the Compose v2 plugin.
- Port in use: change the host port in the wizard; the database port receives an availability check, but verify all selected ports.
- Image pull fails: check network access, registry login/terms (especially Oracle), tag availability, and architecture support.
- Services start but endpoint fails: inspect `docker compose ps` and logs. The frontend dependency only waits for backend startup, not backend health.
