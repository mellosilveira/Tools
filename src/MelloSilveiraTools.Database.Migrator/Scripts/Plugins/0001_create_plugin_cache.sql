-- ============================================================================
-- Migration: 0001_create_plugin_cache.sql
-- Context: Plugins
-- Description: Creates the global plugin_cache table for persisting dynamic plugin states and metadata.
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

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'tools_app') THEN
        GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE plugin_cache TO tools_app;
        GRANT USAGE, SELECT ON SEQUENCE plugin_cache_id_seq TO tools_app;
    END IF;
END
$$;
