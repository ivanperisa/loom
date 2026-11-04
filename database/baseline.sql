-- Baseline for a database that was created from the old hand-written schema.sql (i.e. production).
-- It tells EF Core that the "Initial" migration is already applied, so the migration script only
-- runs the migrations that come after it.
--
--   psql -h <host> -U <owner> -d <db> -v ON_ERROR_STOP=1 -f database/baseline.sql
--
-- Safe to run more than once, and on any database: it only marks "Initial" when the old schema is
-- there (exchange.exchange exists). An empty database is left for the migrations to build.
-- database/deploy/migrate.sh runs it before every migration.

CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId"    character varying(150) NOT NULL,
    "ProductVersion" character varying(32)  NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20261008214540_Initial', '10.0.3'
WHERE to_regclass('exchange.exchange') IS NOT NULL
ON CONFLICT ("MigrationId") DO NOTHING;
