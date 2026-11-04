#!/bin/sh
# Backs up the Loom database: one compressed dump per run, checked after writing, old ones rotated.
#
#   PGHOST=... PGPORT=5432 PGUSER=... PGDATABASE=... PGPASSWORD=... ./backup.sh [backup dir]
#   (or a ~/.pgpass file instead of PGPASSWORD)
#
# Keeps the last KEEP_DAILY dumps (default 14) and one dump per week for KEEP_WEEKLY weeks (default 8).
# Restore with restore.sh. pg_dump must be the server's major version or newer.
set -eu

DIR="${1:-${BACKUP_DIR:-/var/backups/loom}}"
KEEP_DAILY="${KEEP_DAILY:-14}"
KEEP_WEEKLY="${KEEP_WEEKLY:-8}"
DB="${PGDATABASE:?Set PGDATABASE (and PGHOST, PGUSER, ...)}"

mkdir -p "$DIR/daily" "$DIR/weekly"
STAMP="$(date -u +%Y-%m-%dT%H%M%SZ)"
FILE="$DIR/daily/$DB-$STAMP.dump"

# Custom format: compressed, restorable table by table, and readable by pg_restore --list.
pg_dump --format=custom --compress=6 --no-owner --no-privileges --file="$FILE.partial" "$DB"
# A dump that pg_restore cannot list is not a backup.
pg_restore --list "$FILE.partial" > /dev/null
mv "$FILE.partial" "$FILE"

# Sunday's dump is also kept as the weekly one.
if [ "$(date -u +%u)" = "7" ]; then cp "$FILE" "$DIR/weekly/"; fi

prune() { ls -1t "$1"/*.dump 2>/dev/null | tail -n "+$(($2 + 1))" | while read -r old; do rm -f "$old"; done; }
prune "$DIR/daily" "$KEEP_DAILY"
prune "$DIR/weekly" "$KEEP_WEEKLY"

echo "$(date -u +%FT%TZ) backup ok: $FILE ($(du -h "$FILE" | cut -f1))"
