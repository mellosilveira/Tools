-- ============================================================================
-- Migration: 0002_create_mechanical_model_simulations.sql
-- Context: Optimizations
-- Description: Creates the mechanical_model_simulations table for storing simulation runs and snapshots.
-- Database Target: PostgreSQL ONLY (Current application design supports PostgreSQL exclusively).
-- Dependencies: stored_files table (Common context)
-- ============================================================================

CREATE TABLE IF NOT EXISTS mechanical_model_simulations (
    id BIGSERIAL PRIMARY KEY,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    identifier TEXT NOT NULL,
    curve_fit_identifier TEXT NOT NULL,
    external_identifier TEXT,
    mechanical_model_name TEXT NOT NULL,
    experimental_data_file_metadata_id BIGINT REFERENCES stored_files(id),
    simulation_output_file_metadata_id BIGINT REFERENCES stored_files(id),
    asymptote_time DOUBLE PRECISION,
    initial_output_json TEXT NOT NULL,
    final_output_json TEXT NOT NULL,
    absolute_delta_output_json TEXT NOT NULL,
    percentage_delta_output_json TEXT NOT NULL,
    CONSTRAINT uq_simulations_identifier UNIQUE (identifier)
);

CREATE INDEX IF NOT EXISTS ix_simulations_curve_fit_identifier ON mechanical_model_simulations (curve_fit_identifier);
CREATE INDEX IF NOT EXISTS ix_simulations_external_identifier ON mechanical_model_simulations (external_identifier);
CREATE INDEX IF NOT EXISTS ix_simulations_model_name ON mechanical_model_simulations (mechanical_model_name);
