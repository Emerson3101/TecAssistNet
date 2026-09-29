-- Supabase-specific schema hardening for TecAssist.NET.
--
-- The EF Core migrations create the tables, but they cannot create foreign keys
-- referencing auth.users because that schema only exists inside Supabase
-- (integration tests run against plain Postgres containers).
--
-- Row-level security is enabled on EVERY user-scoped table:
--  - documents and conversations own a user_id directly;
--  - document_chunks, messages and message_citations have no user_id column,
--    so they are scoped through their parent rows.
--
-- Without RLS on the child tables, Supabase's default grants would let anyone
-- holding the public anon key read every user's chunks and messages through
-- the PostgREST API. The .NET API connects as the postgres role (bypasses
-- RLS); ownership is enforced in the Application layer. These policies are
-- defense-in-depth for the anon/authenticated API surface.
--
-- Run this once in the Supabase SQL Editor after applying supabase/schema.sql.

do $$
begin
    if not exists (select 1 from pg_constraint where conname = 'fk_documents_user_id') then
        alter table public.documents
            add constraint fk_documents_user_id
            foreign key (user_id) references auth.users (id) on delete cascade;
    end if;

    if not exists (select 1 from pg_constraint where conname = 'fk_conversations_user_id') then
        alter table public.conversations
            add constraint fk_conversations_user_id
            foreign key (user_id) references auth.users (id) on delete cascade;
    end if;
end
$$;

alter table public.documents enable row level security;
alter table public.conversations enable row level security;
alter table public.document_chunks enable row level security;
alter table public.messages enable row level security;
alter table public.message_citations enable row level security;

drop policy if exists "own documents" on public.documents;
create policy "own documents" on public.documents
    for all to authenticated
    using (auth.uid() = user_id)
    with check (auth.uid() = user_id);

drop policy if exists "own conversations" on public.conversations;
create policy "own conversations" on public.conversations
    for all to authenticated
    using (auth.uid() = user_id)
    with check (auth.uid() = user_id);

drop policy if exists "own document chunks" on public.document_chunks;
create policy "own document chunks" on public.document_chunks
    for all to authenticated
    using (exists (
        select 1 from public.documents d
        where d.id = document_id and d.user_id = auth.uid()
    ))
    with check (exists (
        select 1 from public.documents d
        where d.id = document_id and d.user_id = auth.uid()
    ));

drop policy if exists "own messages" on public.messages;
create policy "own messages" on public.messages
    for all to authenticated
    using (exists (
        select 1 from public.conversations c
        where c.id = conversation_id and c.user_id = auth.uid()
    ))
    with check (exists (
        select 1 from public.conversations c
        where c.id = conversation_id and c.user_id = auth.uid()
    ));

drop policy if exists "own message citations" on public.message_citations;
create policy "own message citations" on public.message_citations
    for all to authenticated
    using (exists (
        select 1 from public.messages m
        join public.conversations c on c.id = m.conversation_id
        where m.id = message_id and c.user_id = auth.uid()
    ))
    with check (
        exists (
            select 1 from public.messages m
            join public.conversations c on c.id = m.conversation_id
            where m.id = message_id and c.user_id = auth.uid()
        )
        and exists (
            select 1 from public.document_chunks k
            join public.documents d on d.id = k.document_id
            where k.id = chunk_id and d.user_id = auth.uid()
        )
    );
