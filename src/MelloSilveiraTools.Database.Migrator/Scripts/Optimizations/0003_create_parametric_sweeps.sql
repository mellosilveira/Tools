-- ============================================================================
-- Migration: 0003_create_parametric_sweeps.sql
-- Context: Optimizations
-- Description: Creates the parametric_sweeps table with nullable user_id
--              (NULL = public), NULLS NOT DISTINCT tenant uniqueness, and
--              explicit per-operation Row-Level Security policies.
-- Database Target: PostgreSQL ONLY (Current application design supports PostgreSQL exclusively).
-- ============================================================================

CREATE TABLE IF NOT EXISTS parametric_sweeps (
    id BIGSERIAL PRIMARY KEY,
    creation_timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    user_id BIGINT NULL REFERENCES users (id) DEFAULT public.current_app_user_id(),
    identifier TEXT NOT NULL,
    external_identifier TEXT,
    mechanical_model_name TEXT NOT NULL,
    base_strain DOUBLE PRECISION NOT NULL,
    ramp_time_consideration INT NOT NULL,
    ramp_time DOUBLE PRECISION,
    initial_time DOUBLE PRECISION NOT NULL,
    final_time DOUBLE PRECISION NOT NULL,
    time_step DOUBLE PRECISION NOT NULL,
    root_finding_algorithm INT NOT NULL,
    base_root_finding_input_json TEXT NOT NULL,
    baseline_parameters_json TEXT NOT NULL,
    swept_variables_json TEXT NOT NULL,
    CONSTRAINT uq_parametric_sweeps_identifier_user UNIQUE NULLS NOT DISTINCT (identifier, user_id)
);

CREATE INDEX IF NOT EXISTS ix_parametric_sweeps_user_created ON parametric_sweeps (user_id, creation_timestamp DESC);
CREATE INDEX IF NOT EXISTS ix_parametric_sweeps_public ON parametric_sweeps (creation_timestamp DESC) WHERE user_id IS NULL;
CREATE INDEX IF NOT EXISTS ix_parametric_sweeps_external_identifier ON parametric_sweeps (external_identifier);
CREATE INDEX IF NOT EXISTS ix_parametric_sweeps_model_name ON parametric_sweeps (mechanical_model_name);

ALTER TABLE parametric_sweeps ENABLE ROW LEVEL SECURITY;
ALTER TABLE parametric_sweeps FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS parametric_sweeps_select ON parametric_sweeps;
CREATE POLICY parametric_sweeps_select ON parametric_sweeps
    FOR SELECT
    USING (
        user_id IS NULL
        OR user_id = (SELECT public.current_app_user_id())
        OR (SELECT public.current_app_is_superuser())
    );

DROP POLICY IF EXISTS parametric_sweeps_insert ON parametric_sweeps;
CREATE POLICY parametric_sweeps_insert ON parametric_sweeps
    FOR INSERT
    WITH CHECK (
        user_id IS NOT DISTINCT FROM (SELECT public.current_app_user_id())
        OR (SELECT public.current_app_is_superuser())
    );

DROP POLICY IF EXISTS parametric_sweeps_update ON parametric_sweeps;
CREATE POLICY parametric_sweeps_update ON parametric_sweeps
    FOR UPDATE
    USING (
        user_id = (SELECT public.current_app_user_id())
        OR (SELECT public.current_app_is_superuser())
    )
    WITH CHECK (
        user_id = (SELECT public.current_app_user_id())
        OR (SELECT public.current_app_is_superuser())
    );

DROP POLICY IF EXISTS parametric_sweeps_delete ON parametric_sweeps;
CREATE POLICY parametric_sweeps_delete ON parametric_sweeps
    FOR DELETE
    USING (
        user_id = (SELECT public.current_app_user_id())
        OR (SELECT public.current_app_is_superuser())
    );

GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE parametric_sweeps TO tools_app;
GRANT USAGE, SELECT ON SEQUENCE parametric_sweeps_id_seq TO tools_app;
