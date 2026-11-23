-- ============================================================================
-- Migration: 0003_create_parametric_sweeps.sql
-- Context: Optimizations
-- Description: Creates the user-partitioned parametric_sweeps table with Row-Level Security.
-- Database Target: PostgreSQL ONLY (Current application design supports PostgreSQL exclusively).
-- ============================================================================

CREATE TABLE IF NOT EXISTS parametric_sweeps (
    id BIGSERIAL NOT NULL,
    creation_timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    user_id BIGINT NOT NULL DEFAULT public.current_app_user_id(),
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
    PRIMARY KEY (id, user_id),
    CONSTRAINT uq_parametric_sweeps_identifier_user UNIQUE (identifier, user_id)
) PARTITION BY LIST (user_id);

CREATE TABLE IF NOT EXISTS parametric_sweeps_default PARTITION OF parametric_sweeps DEFAULT;

CREATE INDEX IF NOT EXISTS ix_parametric_sweeps_user_id ON parametric_sweeps (user_id);
CREATE INDEX IF NOT EXISTS ix_parametric_sweeps_external_identifier ON parametric_sweeps (external_identifier);
CREATE INDEX IF NOT EXISTS ix_parametric_sweeps_model_name ON parametric_sweeps (mechanical_model_name);

ALTER TABLE parametric_sweeps ENABLE ROW LEVEL SECURITY;
ALTER TABLE parametric_sweeps FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS parametric_sweeps_user_isolation ON parametric_sweeps;
CREATE POLICY parametric_sweeps_user_isolation ON parametric_sweeps
    USING (user_id = public.current_app_user_id())
    WITH CHECK (user_id = public.current_app_user_id());

GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE parametric_sweeps TO tools_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE parametric_sweeps_default TO tools_app;
GRANT USAGE, SELECT ON SEQUENCE parametric_sweeps_id_seq TO tools_app;
