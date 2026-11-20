-- ============================================================================
-- Migration: 0001_create_plugin_cache.sql
-- Context: Plugins
-- Description: Creates the plugin_cache table for persisting dynamic plugin states and metadata.
-- Database Target: PostgreSQL ONLY (Current application design supports PostgreSQL exclusively).
-- ============================================================================

CREATE TABLE IF NOT EXISTS plugin_cache (
    id BIGSERIAL PRIMARY KEY,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    plugin_name TEXT NOT NULL,
    plugin_version TEXT NOT NULL,
    state_type TEXT NOT NULL,
    state_json TEXT NOT NULL,
    CONSTRAINT uq_plugin_cache_name_version UNIQUE (plugin_name, plugin_version)
);

CREATE INDEX IF NOT EXISTS ix_plugin_cache_plugin_name ON plugin_cache (plugin_name);
