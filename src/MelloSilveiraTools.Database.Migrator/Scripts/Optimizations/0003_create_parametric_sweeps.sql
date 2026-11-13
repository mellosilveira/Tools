-- ============================================================================
-- Migration: 0003_create_parametric_sweeps.sql
-- Context: Optimizations
-- Description: Creates the parametric_sweeps table for storing OAAT sensitivity analysis records.
-- Database Target: PostgreSQL ONLY (Current application design supports PostgreSQL exclusively).
-- ============================================================================

CREATE TABLE IF NOT EXISTS parametric_sweeps (
    id BIGSERIAL PRIMARY KEY,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
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
    CONSTRAINT uq_parametric_sweeps_identifier UNIQUE (identifier)
);

CREATE INDEX IF NOT EXISTS ix_parametric_sweeps_external_identifier ON parametric_sweeps (external_identifier);
CREATE INDEX IF NOT EXISTS ix_parametric_sweeps_model_name ON parametric_sweeps (mechanical_model_name);
