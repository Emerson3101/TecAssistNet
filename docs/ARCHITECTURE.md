# Architecture

TecAssist.NET is a Clean Architecture solution with four projects and a strict dependency direction:

```
Api → Application → Domain
        ↑
Infrastructure (implements Application abstractions)
```

- **Domain** — entities (`Document`, `DocumentChunk`, `Conversation`, `Message`, `MessageCitation`) with no external dependencies except the `Pgvector` value type.
- **Application** — use cases (`ChatService`, `IngestionPipeline`, `DocumentService`, `ConversationService`), abstractions (`IChatClient`, `IEmbeddingClient`, `IChunkSearcher`, `ITextExtractor`, `IChunker`, `IIngestionQueue`), request/response contracts, and the token-aware chunker + prompt builder (pure, unit-testable logic).
- **Infrastructure** — EF Core (`TecAssistDbContext`, snake_case naming via `EFCore.NamingConventions`), pgvector cosine search, NVIDIA NIM clients (raw typed `HttpClient`), JWKS provider, and the channel-based ingestion worker.
- **Api** — controllers, exception-to-problem-details middleware, JWT bearer configuration, health endpoints, DI composition root.

`Domain` and `Application` never reference Npgsql or the NVIDIA clients — integration tests swap the AI clients for fakes through DI and never call NVIDIA at all.

## RAG pipeline

```
Upload (multipart or pasted text)
  → validate + extract text (PdfPig for PDFs)
  → insert documents row (status: pending)
  → enqueue (bounded Channel) → 202 Accepted

IngestionWorker (BackgroundService)
  → dequeue job → chunk (~500 tokens, ~50 overlap, paragraph-aware)
  → batch-embed with input_type=passage
  → insert document_chunks with pgvector embeddings
  → status: ready (failed on error, retried by the user)

Question (POST /api/conversations/{id}/messages)
  → embed question with input_type=query
  → pgvector cosine-distance top-k retrieval (scoped to the caller's documents)
  → system prompt: numbered context excerpts + history + question
  → NVIDIA NIM chat completions (stream: true)
  → SSE frames forwarded: token* → citation* → done
  → parse "[n]" markers from the answer → persist message_citations
```

## Streaming design

`ChatService.StartMessageStreamAsync` performs validation, persists the user message, retrieves context, and returns a `ChatMessageStream` (an `IAsyncEnumerable<ChatStreamEvent>`). This split matters: everything before the first token can still fail with a proper HTTP status (404/502 as `application/problem+json`); once tokens flow, failures become SSE `event: error` frames because the 200 header is already committed.

## Authentication

The API is a pure **resource server**. Supabase Auth issues ES256 JWTs; the API:

1. warms a JWKS cache at startup (`JwksWarmUpService`) and refreshes it every 10 minutes (`IMemoryCache` + a gate),
2. validates issuer (`https://<ref>.supabase.co/auth/v1`), audience (`authenticated`), lifetime, and the ES256 signature via `IssuerSigningKeyResolver`,
3. maps `sub` to the caller's user id, which scopes every query.

Legacy symmetric projects are supported by configuring `Supabase:JwtSecret` instead — the configuration class picks HS256 + the shared secret automatically.

## Authorization — two layers

1. **Application layer** — every service call resolves the user id from `ICurrentUser` and filters by it (`WHERE user_id = ...`). Another user's rows are simply absent (404).
2. **Postgres RLS** — all five tables have Row-Level Security policies scoped through `auth.users`. Even if the API's connection were compromised and used with the anon key through PostgREST, rows cannot cross user boundaries. (The API connects as the `postgres` role, which bypasses RLS by design — RLS hardens the *Supabase API surface*, while the app layer guards API requests.)

## Notable engineering decisions

- **Raw typed HTTP clients for NVIDIA** instead of the OpenAI .NET SDK. Two empirical reasons (repro'd in the commit history): `nemotron-3-embed-1b` requires the non-standard `input_type` field, and the SDK's streaming parser silently drops NEM's `reasoning_content` deltas — the official SDK yielded zero tokens while the raw stream carried them. Small typed clients with explicit wire records win.
- **`vector(2048)` without an HNSW index.** pgvector's HNSW caps at 2000 dimensions; `nemotron-3-embed-1b` emits 2048 and has no dimension-truncation parameter. The `EmbeddingDimension2048` migration drops the index and widens the column; exact cosine scans are more than adequate at this scale, and the migration `Down()` recreates the index.
- **snake_case database naming** via `EFCore.NamingConventions` so the schema matches the Supabase conventions everywhere else (and this README's SQL).
- **`supabase/schema.sql` + `supabase/rls.sql`** instead of network migrations: the EF migrations cannot reference `auth.users` (it only exists in Supabase), so the idempotent scripts are applied once through the SQL Editor. The same migrations are applied by `dotnet ef database update` and by the integration-test factory.
- **Testcontainers, not mocks** — integration tests run the full HTTP pipeline against a real Postgres 16 + pgvector container. The suite caught a real bug: with `Auth:Disabled=true` (the dev bypass), `[Authorize]` endpoints 500'd because no challenge scheme existed. Fixed with a permissive default authorization policy for that mode.

## Frontend

Next.js 16 App Router with a `proxy.ts` (Next 16's renamed middleware) that refreshes Supabase sessions and guards routes. The chat consumes the SSE stream with `fetch` + `ReadableStream`, parses frames incrementally, and renders tokens live with react-markdown + syntax highlighting. The command palette is hand-rolled (cmdk 1.1.1 is broken on React 19.2 — filtering, keyboard navigation, groups).
