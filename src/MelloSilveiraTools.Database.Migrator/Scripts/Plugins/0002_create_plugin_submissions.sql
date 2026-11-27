-- ============================================================================
-- Migration: 0002_create_plugin_submissions.sql
-- Context: Plugins
-- Description: Creates the plugin_submissions table for community plugin uploads
--              referencing storage_entry_metadata via FK (no duplicated file metadata),
--              with SHA-256 integrity, per-plugin author ownership, moderation workflow,
--              and strict PostgreSQL Row-Level Security.
-- Status codes: 0 = Pending, 1 = Approved, 2 = Rejected, 3 = Revoked.
-- Database Target: PostgreSQL ONLY (Current application design supports PostgreSQL exclusively).
-- ============================================================================

CREATE TABLE IF NOT EXISTS plugin_submissions (
    id BIGSERIAL PRIMARY KEY,
    creation_timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    user_id BIGINT NULL REFERENCES users (id),
    storage_entry_metadata_id BIGINT NOT NULL REFERENCES storage_entry_metadata (id) ON DELETE RESTRICT,
    plugin_name TEXT NOT NULL,
    plugin_version TEXT NOT NULL,
    sha256_hash TEXT NOT NULL,
    status INTEGER NOT NULL,
    reviewed_by_user_id BIGINT NULL REFERENCES users (id),
    reviewed_at TIMESTAMPTZ NULL,
    review_notes TEXT NULL,
    CONSTRAINT uq_plugin_submissions_name_version UNIQUE (plugin_name, plugin_version)
);

CREATE INDEX IF NOT EXISTS ix_plugin_submissions_plugin_name ON plugin_submissions (LOWER(plugin_name));
CREATE INDEX IF NOT EXISTS ix_plugin_submissions_status ON plugin_submissions (status);
CREATE INDEX IF NOT EXISTS ix_plugin_submissions_user_id_status ON plugin_submissions (user_id, status);
CREATE INDEX IF NOT EXISTS ix_plugin_submissions_storage_file ON plugin_submissions (storage_entry_metadata_id);

ALTER TABLE plugin_submissions ENABLE ROW LEVEL SECURITY;
ALTER TABLE plugin_submissions FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS plugin_submissions_select ON plugin_submissions;
DROP POLICY IF EXISTS plugin_submissions_insert ON plugin_submissions;
DROP POLICY IF EXISTS plugin_submissions_update ON plugin_submissions;
DROP POLICY IF EXISTS plugin_submissions_delete ON plugin_submissions;

CREATE POLICY plugin_submissions_select ON plugin_submissions
    FOR SELECT
    USING (
        status = 1
        OR user_id = (SELECT public.current_app_user_id())
        OR (SELECT public.current_app_is_superuser())
    );

CREATE POLICY plugin_submissions_insert ON plugin_submissions
    FOR INSERT
    WITH CHECK (
        (
            (SELECT public.current_app_user_id()) IS NOT NULL
            AND user_id = (SELECT public.current_app_user_id())
            AND status = 0
        )
        OR (SELECT public.current_app_is_superuser())
    );

CREATE POLICY plugin_submissions_update ON plugin_submissions
    FOR UPDATE
    USING ((SELECT public.current_app_is_superuser()))
    WITH CHECK ((SELECT public.current_app_is_superuser()));

CREATE POLICY plugin_submissions_delete ON plugin_submissions
    FOR DELETE
    USING (
        (
            (SELECT public.current_app_user_id()) IS NOT NULL
            AND user_id = (SELECT public.current_app_user_id())
            AND status = 0
        )
        OR (SELECT public.current_app_is_superuser())
    );

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'tools_app') THEN
        GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE plugin_submissions TO tools_app;
        GRANT USAGE, SELECT ON SEQUENCE plugin_submissions_id_seq TO tools_app;
    END IF;
END
$$;
