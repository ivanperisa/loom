#!/bin/sh
# Restores a dump made by backup.sh into a NEW database, never over an existing one.
#
#   PGHOST=... PGUSER=... PGPASSWORD=... ./restore.sh <file.dump> <new database name>
#
# Check the restored copy, then point the app at it (or rename the databases) during a maintenance window.
set -eu

FILE="${1:?Usage: restore.sh <file.dump> <new database name>}"
TARGET="${2:?Usage: restore.sh <file.dump> <new database name>}"

pg_restore --list "$FILE" > /dev/null
exists="$(psql -d postgres -tA -v name="$TARGET" <<'SQL'
SELECT 1 FROM pg_database WHERE datname = :'name';
SQL
)"
if [ -n "$exists" ]; then
  echo "Database $TARGET already exists. Restore into a new name." >&2
  exit 1
fi

createdb "$TARGET"
pg_restore --no-owner --no-privileges --exit-on-error --dbname="$TARGET" "$FILE"

exchanges="$(psql -d "$TARGET" -tAc 'SELECT count(*) FROM exchange.exchange')"
migration="$(psql -d "$TARGET" -tAc 'SELECT max("MigrationId") FROM "__EFMigrationsHistory"')"
echo "restored into $TARGET: $exchanges exchanges, last migration $migration"
