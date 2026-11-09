-- ============================================================================
-- Migration: 0001_create_mechanical_model_curve_fit_outputs.sql
-- Context: Optimizations
-- Description: Creates the mechanical_model_curve_fit_outputs table for storing fitted constitutive parameters.
-- Database Target: PostgreSQL ONLY (Current application design supports PostgreSQL exclusively).
-- ============================================================================

CREATE TABLE IF NOT EXISTS mechanical_model_curve_fit_outputs (
    id BIGSERIAL PRIMARY KEY,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
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
    CONSTRAINT uq_curve_fit_outputs_identifier UNIQUE (identifier)
);

CREATE INDEX IF NOT EXISTS ix_curve_fit_outputs_extenal_identifier ON mechanical_model_curve_fit_outputs (extenal_identifier);
CREATE INDEX IF NOT EXISTS ix_curve_fit_outputs_model_name ON mechanical_model_curve_fit_outputs (mechanical_model_name);
