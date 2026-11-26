-- ============================================================================
-- Migration: 0001_create_users.sql
-- Context: Common
-- Description: Creates the global users table (with superuser flag), the
--              non-superuser tools_app role for Row-Level Security enforcement,
--              and DB-backed current_app_user_id() / current_app_is_superuser()
--              helpers that verify active user state directly against users.
-- Database Target: PostgreSQL ONLY (Current application design supports PostgreSQL exclusively).
-- ============================================================================

CREATE TABLE IF NOT EXISTS users (
    id BIGSERIAL PRIMARY KEY,
    creation_timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    username TEXT NOT NULL,
    password_hash TEXT NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_superuser BOOLEAN NOT NULL DEFAULT FALSE
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
SECURITY DEFINER
SET search_path = public
AS $$
    SELECT u.id
    FROM public.users u
    WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::BIGINT
      AND u.is_active = TRUE;
$$;

CREATE OR REPLACE FUNCTION public.current_app_is_superuser()
RETURNS BOOLEAN
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = public
AS $$
    SELECT EXISTS (
        SELECT 1
        FROM public.users u
        WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::BIGINT
          AND u.is_active = TRUE
          AND u.is_superuser = TRUE
    );
$$;

REVOKE ALL ON FUNCTION public.current_app_user_id() FROM PUBLIC;
GRANT EXECUTE ON FUNCTION public.current_app_user_id() TO tools_app;

REVOKE ALL ON FUNCTION public.current_app_is_superuser() FROM PUBLIC;
GRANT EXECUTE ON FUNCTION public.current_app_is_superuser() TO tools_app;
