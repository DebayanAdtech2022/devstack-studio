# Security notes

This guide describes safeguards visible in the current source. They reduce specific risks but do not constitute a full security audit or make generated output production-ready.

## Process execution

`ProcessRunner` uses `ProcessStartInfo` and argument lists rather than constructing a shell command string. The Docker integration executes the `docker` CLI. Review process arguments, environment variables, timeouts, and log handling when adding integrations.

## Generated paths and manifests

`ProjectGenerator` combines generated relative paths under the configured output root using `PathSafety.CombineUnderRoot`. It records SHA-256 hashes in `devstack-manifest.json` and can report a conflict when an existing generated file differs from its prior manifest hash and conflict protection is enabled. This is a limited generated-file conflict check, not a backup system or a universal protection for hand-edited files.

## Credentials

The root generator emits `.env` containing the configured database password and emits `.env.example`. `.env` is listed in the generated `.gitignore`; on non-Windows systems the generated secret file is assigned user read/write permissions. On Windows, the generator does not set a custom ACL. Users must protect the generated folder and avoid sharing `.env` or logs that may contain secrets. Use a proper secrets manager for production.

## SQL generation

The SQL generator uses provider-specific identifier quoting and provider-specific default value formatting. The schema validator restricts defaults and check constraints to supported safe forms. Inspect generated SQL and migrations before applying them to important data; schema generation is not a substitute for code review.

## Container deployment

The development Compose file uses a named database volume, exposes configured host ports, and uses a local loopback bind by default when configured. The production overlay is intentionally minimal. Review external exposure, TLS, credentials, database license/edition, backups, upgrades, resource limits, and container hardening before any production deployment.

## Logging and operational limitations

The desktop app logs to console and a rolling file under the user's local application data directory. The source includes a secret redactor helper, but verify every log path and diagnostic before sharing logs externally. Docker readiness diagnosis depends partly on matching CLI error text and can misclassify unfamiliar failures.
