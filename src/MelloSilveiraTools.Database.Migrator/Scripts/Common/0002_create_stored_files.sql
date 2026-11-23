-- ============================================================================
-- Migration: 0002_create_stored_files.sql
-- Context: Common
-- Description: Creates the user-partitioned stored_files table with Row-Level Security.
-- Database Target: PostgreSQL ONLY (Current application design supports PostgreSQL exclusively).
-- ============================================================================

CREATE TABLE IF NOT EXISTS stored_files (
    id BIGSERIAL NOT NULL,
    creation_timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    user_id BIGINT NOT NULL DEFAULT public.current_app_user_id(),
    external_identifier TEXT,
    storage_provider INT NOT NULL,
    location TEXT NOT NULL,
    directory TEXT NOT NULL,
    file_name TEXT,
    size_bytes BIGINT NOT NULL,
    content_type TEXT NOT NULL,
    PRIMARY KEY (id, user_id)
) PARTITION BY LIST (user_id);

CREATE TABLE IF NOT EXISTS stored_files_default PARTITION OF stored_files DEFAULT;

CREATE INDEX IF NOT EXISTS ix_stored_files_user_id ON stored_files (user_id);
CREATE INDEX IF NOT EXISTS ix_stored_files_external_identifier ON stored_files (external_identifier);
CREATE INDEX IF NOT EXISTS ix_stored_files_creation_timestamp ON stored_files (creation_timestamp);

ALTER TABLE stored_files ENABLE ROW LEVEL SECURITY;
ALTER TABLE stored_files FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS stored_files_user_isolation ON stored_files;
CREATE POLICY stored_files_user_isolation ON stored_files
    USING (user_id = public.current_app_user_id())
    WITH CHECK (user_id = public.current_app_user_id());

GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE stored_files TO tools_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE stored_files_default TO tools_app;
GRANT USAGE, SELECT ON SEQUENCE stored_files_id_seq TO tools_app;
