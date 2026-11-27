-- ============================================================================
-- Migration: 0001_create_plugin_cache.sql
-- Context: Plugins
-- Description: Creates the global plugin_cache table with Row-Level Security
--              (public read, superuser-only mutations).
-- Database Target: PostgreSQL ONLY (Current application design supports PostgreSQL exclusively).
-- ============================================================================

CREATE TABLE IF NOT EXISTS plugin_cache (
    id BIGSERIAL PRIMARY KEY,
    creation_timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    plugin_name TEXT NOT NULL,
    plugin_version TEXT NOT NULL,
    state_type TEXT NOT NULL,
    state_json TEXT NOT NULL,
    CONSTRAINT uq_plugin_cache_name_version UNIQUE (plugin_name, plugin_version)
);

CREATE INDEX IF NOT EXISTS ix_plugin_cache_plugin_name ON plugin_cache (plugin_name);

ALTER TABLE plugin_cache ENABLE ROW LEVEL SECURITY;
ALTER TABLE plugin_cache FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS plugin_cache_select ON plugin_cache;
DROP POLICY IF EXISTS plugin_cache_insert ON plugin_cache;
DROP POLICY IF EXISTS plugin_cache_update ON plugin_cache;
DROP POLICY IF EXISTS plugin_cache_delete ON plugin_cache;

CREATE POLICY plugin_cache_select ON plugin_cache
    FOR SELECT
    USING (TRUE);

CREATE POLICY plugin_cache_insert ON plugin_cache
    FOR INSERT
    WITH CHECK ((SELECT public.current_app_is_superuser()));

CREATE POLICY plugin_cache_update ON plugin_cache
    FOR UPDATE
    USING ((SELECT public.current_app_is_superuser()))
    WITH CHECK ((SELECT public.current_app_is_superuser()));

CREATE POLICY plugin_cache_delete ON plugin_cache
    FOR DELETE
    USING ((SELECT public.current_app_is_superuser()));

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'tools_app') THEN
        GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE plugin_cache TO tools_app;
        GRANT USAGE, SELECT ON SEQUENCE plugin_cache_id_seq TO tools_app;
    END IF;
END
$$;
