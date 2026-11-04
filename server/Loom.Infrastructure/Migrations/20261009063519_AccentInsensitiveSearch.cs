using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Loom.Infrastructure.Migrations
{
    /// <summary>
    /// Accent-insensitive search ("cakovec" finds "Čakovec"): the unaccent extension behind an immutable wrapper
    /// (<c>f_unaccent</c>, mapped to <c>TextSearch.Unaccent</c>), and trigram indexes for the one list that searches
    /// the whole user table (name, email, JMBAG). pg_trgm and unaccent are trusted extensions (PostgreSQL 13+): the database owner can
    /// create them without superuser rights.
    /// </summary>
    public partial class AccentInsensitiveSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE EXTENSION IF NOT EXISTS unaccent SCHEMA public;
                CREATE EXTENSION IF NOT EXISTS pg_trgm SCHEMA public;

                -- unaccent() is only STABLE (its dictionary could change); a fixed dictionary makes it safe to index.
                CREATE OR REPLACE FUNCTION public.f_unaccent(text) RETURNS text
                    LANGUAGE sql IMMUTABLE PARALLEL SAFE STRICT
                    AS $$ SELECT public.unaccent('public.unaccent'::regdictionary, $1) $$;

                CREATE INDEX IF NOT EXISTS idx_user_name_search ON public."user" USING gin (public.f_unaccent(lower(name)) public.gin_trgm_ops);
                CREATE INDEX IF NOT EXISTS idx_user_email_search ON public."user" USING gin (public.f_unaccent(lower(email)) public.gin_trgm_ops);
                CREATE INDEX IF NOT EXISTS idx_user_jmbag_search ON public."user" USING gin (public.f_unaccent(lower(jmbag)) public.gin_trgm_ops);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The extensions stay: other objects in the database may use them.
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS public.idx_user_jmbag_search;
                DROP INDEX IF EXISTS public.idx_user_email_search;
                DROP INDEX IF EXISTS public.idx_user_name_search;
                DROP FUNCTION IF EXISTS public.f_unaccent(text);
                """);
        }
    }
}
