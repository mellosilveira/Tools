-- ============================================================================
-- Migration: 0001_create_users.sql
-- Context: Common
-- Description: Creates the global users table, the non-superuser tools_app role
--              for Row-Level Security enforcement, the current_app_user_id()
--              session accessor, and ensure_user_partitions(BIGINT).
-- Database Target: PostgreSQL ONLY (Current application design supports PostgreSQL exclusively).
-- ============================================================================

CREATE TABLE IF NOT EXISTS users (
    id BIGSERIAL PRIMARY KEY,
    creation_timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    username TEXT NOT NULL,
    password_hash TEXT NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_users_username ON users (username);

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'tools_app') THEN
        CREATE ROLE tools_app LOGIN PASSWORD 'tools_app_pass' NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS;
    END IF;
END
$$;

GRANT USAGE ON SCHEMA public TO tools_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE users TO tools_app;
GRANT USAGE, SELECT ON SEQUENCE users_id_seq TO tools_app;

CREATE OR REPLACE FUNCTION public.current_app_user_id()
RETURNS BIGINT
LANGUAGE sql
STABLE
AS $$
    SELECT NULLIF(current_setting('app.current_user_id', true), '')::BIGINT;
$$;

GRANT EXECUTE ON FUNCTION public.current_app_user_id() TO tools_app;

CREATE OR REPLACE FUNCTION public.ensure_user_partitions(p_user_id BIGINT)
RETURNS VOID
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
    v_table TEXT;
    v_partition TEXT;
BEGIN
    IF p_user_id IS NULL OR p_user_id <= 0 THEN
        RAISE EXCEPTION 'p_user_id must be a positive integer.';
    END IF;

    FOREACH v_table IN ARRAY ARRAY[
        'stored_files',
        'mechanical_model_curve_fit_outputs',
        'mechanical_model_simulations',
        'parametric_sweeps'
    ]
    LOOP
        IF to_regclass(format('public.%I', v_table)) IS NOT NULL THEN
            v_partition := format('%s_u_%s', v_table, p_user_id);
            EXECUTE format(
                'CREATE TABLE IF NOT EXISTS public.%I PARTITION OF public.%I FOR VALUES IN (%s)',
                v_partition,
                v_table,
                p_user_id
            );
            EXECUTE format(
                'GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.%I TO tools_app',
                v_partition
            );
        END IF;
    END LOOP;
END;
$$;

REVOKE ALL ON FUNCTION public.ensure_user_partitions(BIGINT) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION public.ensure_user_partitions(BIGINT) TO tools_app;
