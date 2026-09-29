CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260929110108_InitialCreate') THEN
    CREATE EXTENSION IF NOT EXISTS vector;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260929110108_InitialCreate') THEN
    CREATE TABLE conversations (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        user_id uuid NOT NULL,
        title text,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT pk_conversations PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260929110108_InitialCreate') THEN
    CREATE TABLE documents (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        user_id uuid NOT NULL,
        title text NOT NULL,
        source_type character varying(16) NOT NULL,
        status character varying(16) NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT pk_documents PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260929110108_InitialCreate') THEN
    CREATE TABLE messages (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        conversation_id uuid NOT NULL,
        role character varying(16) NOT NULL,
        content text NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT pk_messages PRIMARY KEY (id),
        CONSTRAINT fk_messages_conversations_conversation_id FOREIGN KEY (conversation_id) REFERENCES conversations (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260929110108_InitialCreate') THEN
    CREATE TABLE document_chunks (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        document_id uuid NOT NULL,
        chunk_index integer NOT NULL,
        content text NOT NULL,
        token_count integer NOT NULL,
        embedding vector(1024),
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT pk_document_chunks PRIMARY KEY (id),
        CONSTRAINT fk_document_chunks_documents_document_id FOREIGN KEY (document_id) REFERENCES documents (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260929110108_InitialCreate') THEN
    CREATE TABLE message_citations (
        message_id uuid NOT NULL,
        chunk_id uuid NOT NULL,
        CONSTRAINT pk_message_citations PRIMARY KEY (message_id, chunk_id),
        CONSTRAINT fk_message_citations_document_chunks_chunk_id FOREIGN KEY (chunk_id) REFERENCES document_chunks (id) ON DELETE CASCADE,
        CONSTRAINT fk_message_citations_messages_message_id FOREIGN KEY (message_id) REFERENCES messages (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260929110108_InitialCreate') THEN
    CREATE INDEX ix_conversations_user_id ON conversations (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260929110108_InitialCreate') THEN
    CREATE INDEX ix_document_chunks_document_id ON document_chunks (document_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260929110108_InitialCreate') THEN
    CREATE INDEX ix_documents_user_id ON documents (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260929110108_InitialCreate') THEN
    CREATE INDEX ix_message_citations_chunk_id ON message_citations (chunk_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260929110108_InitialCreate') THEN
    CREATE INDEX ix_messages_conversation_id ON messages (conversation_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260929110108_InitialCreate') THEN
    alter table documents
        add constraint ck_documents_source_type check (source_type in ('text', 'markdown', 'pdf'));

    alter table documents
        add constraint ck_documents_status check (status in ('pending', 'processing', 'ready', 'failed'));

    alter table messages
        add constraint ck_messages_role check (role in ('user', 'assistant'));

    create index ix_document_chunks_embedding_hnsw on document_chunks using hnsw (embedding vector_cosine_ops);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260929110108_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260929110108_InitialCreate', '10.0.12');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260929124705_EmbeddingDimension2048') THEN
    drop index if exists ix_document_chunks_embedding_hnsw;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260929124705_EmbeddingDimension2048') THEN
    ALTER TABLE document_chunks ALTER COLUMN embedding TYPE vector(2048);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260929124705_EmbeddingDimension2048') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260929124705_EmbeddingDimension2048', '10.0.12');
    END IF;
END $EF$;
COMMIT;

