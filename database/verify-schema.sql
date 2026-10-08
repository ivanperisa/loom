-- Canonical, sorted description of the Loom schema (columns, constraints, indexes).
-- Run against two databases and diff the outputs, e.g.:
--   psql -d prod  -tA -f database/verify-schema.sql > prod.txt
--   psql -d local -tA -f database/verify-schema.sql > local.txt
--   diff prod.txt local.txt
-- Read-only.
SELECT 'COL '||table_schema||'.'||table_name||'.'||column_name||' '||
  CASE WHEN data_type='character varying' THEN 'varchar('||coalesce(character_maximum_length::text,'')||')'
       WHEN data_type='numeric' THEN 'numeric('||coalesce(numeric_precision::text,'')||','||coalesce(numeric_scale::text,'')||')'
       WHEN data_type='ARRAY' THEN udt_name ELSE data_type END
  ||' '||is_nullable||' default='||coalesce(regexp_replace(column_default,'nextval\(.*\)','<seq>'),'')
FROM information_schema.columns WHERE table_schema IN ('public','home','partner','exchange') AND table_name<>'__EFMigrationsHistory'
UNION ALL
SELECT 'CON '||n.nspname||'.'||t.relname||' '||c.conname||' '||c.contype::text||' '||pg_get_constraintdef(c.oid)
FROM pg_constraint c JOIN pg_class t ON t.oid=c.conrelid JOIN pg_namespace n ON n.oid=t.relnamespace
WHERE n.nspname IN ('public','home','partner','exchange') AND t.relname<>'__EFMigrationsHistory' AND c.contype::text<>'n'
UNION ALL
SELECT 'IDX '||schemaname||'.'||tablename||' '||indexdef
FROM pg_indexes WHERE schemaname IN ('public','home','partner','exchange') AND tablename<>'__EFMigrationsHistory'
ORDER BY 1;
