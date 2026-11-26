-- ============================================================================
-- Migration: 0001_create_mechanical_model_curve_fit_outputs.sql
-- Context: Optimizations
-- Description: Creates the mechanical_model_curve_fit_outputs table with
--              nullable user_id (NULL = public), NULLS NOT DISTINCT tenant
--              uniqueness, and explicit per-operation Row-Level Security policies.
-- Database Target: PostgreSQL ONLY (Current application design supports PostgreSQL exclusively).
-- ============================================================================

CREATE TABLE IF NOT EXISTS mechanical_model_curve_fit_outputs (
    id BIGSERIAL PRIMARY KEY,
    creation_timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    user_id BIGINT NULL REFERENCES users (id) DEFAULT public.current_app_user_id(),
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
    CONSTRAINT uq_curve_fit_outputs_identifier_user UNIQUE NULLS NOT DISTINCT (identifier, user_id)
);

CREATE INDEX IF NOT EXISTS ix_curve_fit_outputs_user_created ON mechanical_model_curve_fit_outputs (user_id, creation_timestamp DESC);
CREATE INDEX IF NOT EXISTS ix_curve_fit_outputs_public ON mechanical_model_curve_fit_outputs (creation_timestamp DESC) WHERE user_id IS NULL;
CREATE INDEX IF NOT EXISTS ix_curve_fit_outputs_extenal_identifier ON mechanical_model_curve_fit_outputs (extenal_identifier);
CREATE INDEX IF NOT EXISTS ix_curve_fit_outputs_model_name ON mechanical_model_curve_fit_outputs (mechanical_model_name);

ALTER TABLE mechanical_model_curve_fit_outputs ENABLE ROW LEVEL SECURITY;
ALTER TABLE mechanical_model_curve_fit_outputs FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS mechanical_model_curve_fit_outputs_select ON mechanical_model_curve_fit_outputs;
CREATE POLICY mechanical_model_curve_fit_outputs_select ON mechanical_model_curve_fit_outputs
    FOR SELECT
    USING (
        user_id IS NULL
        OR user_id = (SELECT public.current_app_user_id())
        OR (SELECT public.current_app_is_superuser())
    );

DROP POLICY IF EXISTS mechanical_model_curve_fit_outputs_insert ON mechanical_model_curve_fit_outputs;
CREATE POLICY mechanical_model_curve_fit_outputs_insert ON mechanical_model_curve_fit_outputs
    FOR INSERT
    WITH CHECK (
        user_id IS NOT DISTINCT FROM (SELECT public.current_app_user_id())
        OR (SELECT public.current_app_is_superuser())
    );

DROP POLICY IF EXISTS mechanical_model_curve_fit_outputs_update ON mechanical_model_curve_fit_outputs;
CREATE POLICY mechanical_model_curve_fit_outputs_update ON mechanical_model_curve_fit_outputs
    FOR UPDATE
    USING (
        user_id = (SELECT public.current_app_user_id())
        OR (SELECT public.current_app_is_superuser())
    )
    WITH CHECK (
        user_id = (SELECT public.current_app_user_id())
        OR (SELECT public.current_app_is_superuser())
    );

DROP POLICY IF EXISTS mechanical_model_curve_fit_outputs_delete ON mechanical_model_curve_fit_outputs;
CREATE POLICY mechanical_model_curve_fit_outputs_delete ON mechanical_model_curve_fit_outputs
    FOR DELETE
    USING (
        user_id = (SELECT public.current_app_user_id())
        OR (SELECT public.current_app_is_superuser())
    );

GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE mechanical_model_curve_fit_outputs TO tools_app;
GRANT USAGE, SELECT ON SEQUENCE mechanical_model_curve_fit_outputs_id_seq TO tools_app;
