-- ============================================================================
-- Migration: 0002_create_mechanical_model_simulations.sql
-- Context: Optimizations
-- Description: Creates the mechanical_model_simulations table with nullable
--              user_id (NULL = public), simple foreign keys to storage_entry_metadata(id)
--              (enabling private simulations to reference public datasets),
--              and explicit per-operation Row-Level Security policies.
-- Database Target: PostgreSQL ONLY (Current application design supports PostgreSQL exclusively).
-- Dependencies: storage_entry_metadata table (Common context)
-- ============================================================================

CREATE TABLE IF NOT EXISTS mechanical_model_simulations (
    id BIGSERIAL PRIMARY KEY,
    creation_timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    user_id BIGINT NULL REFERENCES users (id) DEFAULT public.current_app_user_id(),
    identifier TEXT NOT NULL,
    curve_fit_identifier TEXT NOT NULL,
    external_identifier TEXT,
    mechanical_model_name TEXT NOT NULL,
    experimental_data_file_metadata_id BIGINT,
    simulation_output_file_metadata_id BIGINT,
    asymptote_time DOUBLE PRECISION,
    initial_output_json TEXT NOT NULL,
    final_output_json TEXT NOT NULL,
    absolute_delta_output_json TEXT NOT NULL,
    percentage_delta_output_json TEXT NOT NULL,
    CONSTRAINT uq_simulations_identifier_user UNIQUE NULLS NOT DISTINCT (identifier, user_id),
    CONSTRAINT fk_simulations_experimental_data_file FOREIGN KEY (experimental_data_file_metadata_id) REFERENCES storage_entry_metadata (id),
    CONSTRAINT fk_simulations_output_file FOREIGN KEY (simulation_output_file_metadata_id) REFERENCES storage_entry_metadata (id)
);

CREATE INDEX IF NOT EXISTS ix_simulations_user_created ON mechanical_model_simulations (user_id, creation_timestamp DESC);
CREATE INDEX IF NOT EXISTS ix_simulations_public ON mechanical_model_simulations (creation_timestamp DESC) WHERE user_id IS NULL;
CREATE INDEX IF NOT EXISTS ix_simulations_curve_fit_identifier ON mechanical_model_simulations (curve_fit_identifier);
CREATE INDEX IF NOT EXISTS ix_simulations_external_identifier ON mechanical_model_simulations (external_identifier);
CREATE INDEX IF NOT EXISTS ix_simulations_model_name ON mechanical_model_simulations (mechanical_model_name);

ALTER TABLE mechanical_model_simulations ENABLE ROW LEVEL SECURITY;
ALTER TABLE mechanical_model_simulations FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS mechanical_model_simulations_select ON mechanical_model_simulations;
CREATE POLICY mechanical_model_simulations_select ON mechanical_model_simulations
    FOR SELECT
    USING (
        user_id IS NULL
        OR user_id = (SELECT public.current_app_user_id())
        OR (SELECT public.current_app_is_superuser())
    );

DROP POLICY IF EXISTS mechanical_model_simulations_insert ON mechanical_model_simulations;
CREATE POLICY mechanical_model_simulations_insert ON mechanical_model_simulations
    FOR INSERT
    WITH CHECK (
        user_id IS NOT DISTINCT FROM (SELECT public.current_app_user_id())
        OR (SELECT public.current_app_is_superuser())
    );

DROP POLICY IF EXISTS mechanical_model_simulations_update ON mechanical_model_simulations;
CREATE POLICY mechanical_model_simulations_update ON mechanical_model_simulations
    FOR UPDATE
    USING (
        user_id = (SELECT public.current_app_user_id())
        OR (SELECT public.current_app_is_superuser())
    )
    WITH CHECK (
        user_id = (SELECT public.current_app_user_id())
        OR (SELECT public.current_app_is_superuser())
    );

DROP POLICY IF EXISTS mechanical_model_simulations_delete ON mechanical_model_simulations;
CREATE POLICY mechanical_model_simulations_delete ON mechanical_model_simulations
    FOR DELETE
    USING (
        user_id = (SELECT public.current_app_user_id())
        OR (SELECT public.current_app_is_superuser())
    );

GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE mechanical_model_simulations TO tools_app;
GRANT USAGE, SELECT ON SEQUENCE mechanical_model_simulations_id_seq TO tools_app;
