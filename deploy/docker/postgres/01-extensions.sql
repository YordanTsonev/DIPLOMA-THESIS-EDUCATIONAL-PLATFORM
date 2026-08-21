-- Executed once, on first initialisation of the PostgreSQL data volume.
-- Extensions must exist before EF Core migrations that depend on them run.

CREATE EXTENSION IF NOT EXISTS vector;      -- pgvector: RAG embeddings (Phase 8)
CREATE EXTENSION IF NOT EXISTS pg_trgm;     -- trigram search for names and titles
CREATE EXTENSION IF NOT EXISTS unaccent;    -- accent-insensitive full-text search
