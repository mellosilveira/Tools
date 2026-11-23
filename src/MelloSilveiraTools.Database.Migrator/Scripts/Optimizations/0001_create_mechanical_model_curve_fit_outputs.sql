-- ============================================================================
-- Migration: 0001_create_mechanical_model_curve_fit_outputs.sql
-- Context: Optimizations
-- Description: Creates the user-partitioned mechanical_model_curve_fit_outputs
--              table with Row-Level Security.
-- Database Target: PostgreSQL ONLY (Current application design supports PostgreSQL exclusively).
-- ============================================================================

CREATE TABLE IF NOT EXISTS mechanical_model_curve_fit_outputs (
    id BIGSERIAL NOT NULL,
    creation_timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    user_id BIGINT NOT NULL DEFAULT public.current_app_user_id(),
    identifier TEXT NOT NULL,
    extenal_identifier TEXT,
    mechanical_model_name TEXT NOT NULL,
    initial_accepted_range DOUBLE PRECISION NOT NULL,
    final_accepted_range DOUBLE PRECISION NOT NULL,
    load_response_relationship INT NOT NULL,
    ramp_time_consideration INT NOT NULL,
    viscoelastic_effect INT NOT NULL,
    error DOUBLE PRECISION NOT NULL,
    iterations INT NOT NULL,
    constitutive_parameters TEXT NOT NULL,
    PRIMARY KEY (id, user_id),
    CONSTRAINT uq_curve_fit_outputs_identifier_user UNIQUE (identifier, user_id)
) PARTITION BY LIST (user_id);

CREATE TABLE IF NOT EXISTS mechanical_model_curve_fit_outputs_default PARTITION OF mechanical_model_curve_fit_outputs DEFAULT;

CREATE INDEX IF NOT EXISTS ix_curve_fit_outputs_user_id ON mechanical_model_curve_fit_outputs (user_id);
CREATE INDEX IF NOT EXISTS ix_curve_fit_outputs_extenal_identifier ON mechanical_model_curve_fit_outputs (extenal_identifier);
CREATE INDEX IF NOT EXISTS ix_curve_fit_outputs_model_name ON mechanical_model_curve_fit_outputs (mechanical_model_name);

ALTER TABLE mechanical_model_curve_fit_outputs ENABLE ROW LEVEL SECURITY;
ALTER TABLE mechanical_model_curve_fit_outputs FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS mechanical_model_curve_fit_outputs_user_isolation ON mechanical_model_curve_fit_outputs;
CREATE POLICY mechanical_model_curve_fit_outputs_user_isolation ON mechanical_model_curve_fit_outputs
    USING (user_id = public.current_app_user_id())
    WITH CHECK (user_id = public.current_app_user_id());

GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE mechanical_model_curve_fit_outputs TO tools_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE mechanical_model_curve_fit_outputs_default TO tools_app;
GRANT USAGE, SELECT ON SEQUENCE mechanical_model_curve_fit_outputs_id_seq TO tools_app;
