#!/bin/sh
# Applies pending EF Core migrations to the database, run by the deploy workflow on the server
# before the new API is unpacked.
#
#   PGHOST=... PGPORT=... PGUSER=... PGPASSWORD=... PGDATABASE=... ./migrate.sh <dir> [backup dir]
#
# <dir> holds what the workflow ships: migrations.sql (idempotent, generated with --no-transactions),
# migrations.txt (the migration ids), baseline.sql and backup.sh. When nothing is pending it does
# nothing. Otherwise it backs up first, then runs the whole script as one transaction: on any error
# the database stays as it was and the script exits non-zero, so the workflow stops before the new
# API replaces the old one.
set -eu

DIR="${1:?Usage: migrate.sh <dir> [backup dir]}"
BACKUP_DIR="${2:-${BACKUP_DIR:-$HOME/loom-backups}}"
: "${PGDATABASE:?Set PGDATABASE (and PGHOST, PGUSER, PGPASSWORD)}"
# The scripts are UTF-8 (the EF script starts with a BOM, which psql skips only in UTF-8). Without this,
# psql uses the system's encoding, e.g. WIN1250 on Windows, and fails on the first line.
export PGCLIENTENCODING=UTF8

for tool in psql pg_dump pg_restore; do
  command -v "$tool" > /dev/null || { echo "migrate: $tool is not installed on this server" >&2; exit 1; }
done

# A database still on the old schema.sql gets "Initial" marked as applied (no-op afterwards).
psql -q -v ON_ERROR_STOP=1 -f "$DIR/baseline.sql"

applied="$(psql -tA -v ON_ERROR_STOP=1 -c 'SELECT "MigrationId" FROM "__EFMigrationsHistory"')"
pending="$(printf '%s\n' "$applied" | grep -vxF -f - "$DIR/migrations.txt" || true)"
if [ -z "$pending" ]; then
  echo "migrate: database is up to date"
  exit 0
fi

echo "migrate: pending migrations:"
printf '  %s\n' $pending
sh "$DIR/backup.sh" "$BACKUP_DIR"
psql -q -1 -v ON_ERROR_STOP=1 -f "$DIR/migrations.sql"
echo "migrate: done"
