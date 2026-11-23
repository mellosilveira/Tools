-- ============================================================================
-- Migration: 0002_create_mechanical_model_simulations.sql
-- Context: Optimizations
-- Description: Creates the user-partitioned mechanical_model_simulations table
--              with composite foreign keys to stored_files and Row-Level Security.
-- Database Target: PostgreSQL ONLY (Current application design supports PostgreSQL exclusively).
-- Dependencies: stored_files table (Common context)
-- ============================================================================

CREATE TABLE IF NOT EXISTS mechanical_model_simulations (
    id BIGSERIAL NOT NULL,
    creation_timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    user_id BIGINT NOT NULL DEFAULT public.current_app_user_id(),
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
    PRIMARY KEY (id, user_id),
    CONSTRAINT uq_simulations_identifier_user UNIQUE (identifier, user_id),
    CONSTRAINT fk_simulations_experimental_data_file FOREIGN KEY (experimental_data_file_metadata_id, user_id) REFERENCES stored_files (id, user_id),
    CONSTRAINT fk_simulations_output_file FOREIGN KEY (simulation_output_file_metadata_id, user_id) REFERENCES stored_files (id, user_id)
) PARTITION BY LIST (user_id);

CREATE TABLE IF NOT EXISTS mechanical_model_simulations_default PARTITION OF mechanical_model_simulations DEFAULT;

CREATE INDEX IF NOT EXISTS ix_simulations_user_id ON mechanical_model_simulations (user_id);
CREATE INDEX IF NOT EXISTS ix_simulations_curve_fit_identifier ON mechanical_model_simulations (curve_fit_identifier);
CREATE INDEX IF NOT EXISTS ix_simulations_external_identifier ON mechanical_model_simulations (external_identifier);
CREATE INDEX IF NOT EXISTS ix_simulations_model_name ON mechanical_model_simulations (mechanical_model_name);

ALTER TABLE mechanical_model_simulations ENABLE ROW LEVEL SECURITY;
ALTER TABLE mechanical_model_simulations FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS mechanical_model_simulations_user_isolation ON mechanical_model_simulations;
CREATE POLICY mechanical_model_simulations_user_isolation ON mechanical_model_simulations
    USING (user_id = public.current_app_user_id())
    WITH CHECK (user_id = public.current_app_user_id());

GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE mechanical_model_simulations TO tools_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE mechanical_model_simulations_default TO tools_app;
GRANT USAGE, SELECT ON SEQUENCE mechanical_model_simulations_id_seq TO tools_app;
