-- Supabase-specific schema hardening for TecAssist.NET.
--
-- The EF Core migrations create the tables, but they cannot create foreign keys
-- referencing auth.users because that schema only exists inside Supabase
-- (integration tests run against plain Postgres containers).
--
-- Run this once in the Supabase SQL Editor after applying the EF migrations.

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
