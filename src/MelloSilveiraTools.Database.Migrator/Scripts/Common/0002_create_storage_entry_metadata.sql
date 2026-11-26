-- ============================================================================
-- Migration: 0002_create_storage_entry_metadata.sql
-- Context: Common
-- Description: Creates the storage_entry_metadata table with nullable user_id
--              (NULL = public) and explicit per-operation Row-Level Security policies.
-- Database Target: PostgreSQL ONLY (Current application design supports PostgreSQL exclusively).
-- ============================================================================

CREATE TABLE IF NOT EXISTS storage_entry_metadata (
    id BIGSERIAL PRIMARY KEY,
    creation_timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    user_id BIGINT NULL REFERENCES users (id) DEFAULT public.current_app_user_id(),
    external_identifier TEXT,
    storage_provider INT NOT NULL,
    location TEXT NOT NULL,
    directory TEXT NOT NULL,
    file_name TEXT,
    size_bytes BIGINT NOT NULL,
    content_type TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_storage_entry_metadata_user_created ON storage_entry_metadata (user_id, creation_timestamp DESC);
CREATE INDEX IF NOT EXISTS ix_storage_entry_metadata_public ON storage_entry_metadata (creation_timestamp DESC) WHERE user_id IS NULL;
CREATE INDEX IF NOT EXISTS ix_storage_entry_metadata_external_identifier ON storage_entry_metadata (external_identifier);

ALTER TABLE storage_entry_metadata ENABLE ROW LEVEL SECURITY;
ALTER TABLE storage_entry_metadata FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS storage_entry_metadata_select ON storage_entry_metadata;
CREATE POLICY storage_entry_metadata_select ON storage_entry_metadata
    FOR SELECT
    USING (
        user_id IS NULL
        OR user_id = (SELECT public.current_app_user_id())
        OR (SELECT public.current_app_is_superuser())
    );

DROP POLICY IF EXISTS storage_entry_metadata_insert ON storage_entry_metadata;
CREATE POLICY storage_entry_metadata_insert ON storage_entry_metadata
    FOR INSERT
    WITH CHECK (
        user_id IS NOT DISTINCT FROM (SELECT public.current_app_user_id())
        OR (SELECT public.current_app_is_superuser())
    );

DROP POLICY IF EXISTS storage_entry_metadata_update ON storage_entry_metadata;
CREATE POLICY storage_entry_metadata_update ON storage_entry_metadata
    FOR UPDATE
    USING (
        user_id = (SELECT public.current_app_user_id())
        OR (SELECT public.current_app_is_superuser())
    )
    WITH CHECK (
        user_id = (SELECT public.current_app_user_id())
        OR (SELECT public.current_app_is_superuser())
    );

DROP POLICY IF EXISTS storage_entry_metadata_delete ON storage_entry_metadata;
CREATE POLICY storage_entry_metadata_delete ON storage_entry_metadata
    FOR DELETE
    USING (
        user_id = (SELECT public.current_app_user_id())
        OR (SELECT public.current_app_is_superuser())
    );

GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE storage_entry_metadata TO tools_app;
GRANT USAGE, SELECT ON SEQUENCE storage_entry_metadata_id_seq TO tools_app;
