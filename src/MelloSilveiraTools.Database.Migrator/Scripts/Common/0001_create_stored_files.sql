-- ============================================================================
-- Migration: 0001_create_stored_files.sql
-- Context: Common
-- Description: Creates the stored_files table for tracking storage metadata.
-- Database Target: PostgreSQL ONLY (Current application design supports PostgreSQL exclusively).
-- ============================================================================

CREATE TABLE IF NOT EXISTS stored_files (
    id BIGSERIAL PRIMARY KEY,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    external_identifier TEXT,
    storage_provider INT NOT NULL,
    location TEXT NOT NULL,
    directory TEXT NOT NULL,
    file_name TEXT,
    size_bytes BIGINT NOT NULL,
    content_type TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_stored_files_external_identifier ON stored_files (external_identifier);
CREATE INDEX IF NOT EXISTS ix_stored_files_created_at ON stored_files (created_at);
