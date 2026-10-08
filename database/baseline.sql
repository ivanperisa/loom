-- One-time baseline for a database that was created from the old hand-written schema.sql
-- (i.e. production). It tells EF Core that the "Initial" migration is already applied,
-- so `efbundle` / `dotnet ef database update` only runs the migrations that come after it.
--
-- Run once, before the first migration-based deploy:
--   psql -h <host> -U <owner> -d <db> -v ON_ERROR_STOP=1 -f database/baseline.sql
-- Safe to run more than once.

CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId"    character varying(150) NOT NULL,
    "ProductVersion" character varying(32)  NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261008214540_Initial', '10.0.3')
ON CONFLICT ("MigrationId") DO NOTHING;
