# API Reference

Base URL (local): `http://localhost:5028` — all routes require `Authorization: Bearer <supabase-jwt>` except the health endpoints.

Errors follow RFC 7807: validation → `400`, unknown/unowned resource → `404` (existence is not leaked), AI provider failure → `502`, ingestion queue full → `503`, always as `application/problem+json`.

## Health

| Method | Route | Notes |
|---|---|---|
| `GET` | `/health` | Liveness. Always `200 Healthy` when the process is up. |
| `GET` | `/health/ready` | Readiness. `200` when Postgres is reachable, `503` otherwise. |

## Documents

### Upload a file

```
POST /api/documents
Content-Type: multipart/form-data

<form field "file"> report.pdf
```

`202 Accepted` with the created document and a `Location` header pointing at it:

```json
{
  "id": "2b7c06a9-ae12-4f23-bb97-5c1e1791bfd9",
  "title": "report",
  "sourceType": "pdf",
  "status": "pending",
  "createdAt": "2026-09-29T13:45:35.711Z",
  "chunkCount": 0
}
```

Allowed: `.pdf`, `.md`, `.txt` up to 25 MB. Text extraction happens immediately (PDFs without a text layer are rejected with 400); embedding runs in the background. Poll `GET /api/documents/{id}` for the status transition `pending → processing → ready|failed`.

### Create from pasted text

```
POST /api/documents/text
Content-Type: application/json

{ "title": "Calibration notes", "content": "...", "sourceType": "text" }
```

`sourceType` accepts `text` or `markdown`.

### List / get / delete

```
GET    /api/documents          → [{ ...document, chunkCount }]
GET    /api/documents/{id}     → document | 404
DELETE /api/documents/{id}     → 204 | 404 (chunks cascade)
```

## Conversations

```
POST   /api/conversations          { "title": "optional" } → 201
GET    /api/conversations           → list with message counts + last activity
PATCH  /api/conversations/{id}     { "title": "new" } → 200 | 404
DELETE /api/conversations/{id}     → 204 | 404 (messages + citations cascade)
GET    /api/conversations/{id}/messages → ascending history with citations
```

Conversations with no title are auto-titled from the first message (truncated to 80 characters).

### Send a message (SSE streaming)

```
POST /api/conversations/{id}/messages
Content-Type: application/json
Accept: text/event-stream

{ "content": "What did the Q3 report say about voltage compliance?" }
```

Response `200` + `text/event-stream`, frames in order:

```
event: token
data: {"text":"Based"}

event: token
data: {"text":" on the"}

event: citation
data: {"chunkId":"...","documentTitle":"Q3 Report.pdf","snippet":"...","score":0.596}

event: done
data: {"userMessageId":"...","assistantMessageId":"..."}
```

- Zero or more `token` frames stream the answer incrementally.
- One `citation` frame per cited chunk (parsed from the answer's inline `[n]` markers; if the model cites nothing, all retrieved chunks are returned as context citations). `score` is cosine similarity at retrieval time — historical citations replay from the database without a score.
- `done` is always the last frame and carries the persisted message ids.
- If generation fails after the stream starts, an `event: error` frame `{"message": "..."}` replaces the remaining frames.
- Pre-stream failures return regular problem+json statuses: `404` for a missing/foreign conversation, `502` for context-retrieval or model failure, `400` for invalid payloads.

### Generate an AI conversation title

```
POST /api/conversations/{id}/title
```

Asks the model (a separate, background stream) to name the conversation from its first exchange. No-op when the conversation already has a title (manual renames are never overwritten).

```json
{ "id": "7ff47656-…", "title": "Voltage compliance in Q3" }
```

The web app fires this automatically once the first streamed response completes, so new chats get a short, meaningful name in the sidebar without the user doing anything.

## Example with curl

```bash
TOKEN=eyJ...   # a Supabase access token

curl -N http://localhost:5028/api/conversations/$CONVERSATION_ID/messages \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"content":"Summarize the calibration schedule"}'
```

`scripts/smoke-test.ps1` exercises the entire surface — upload, ingestion polling, streaming chat, history, cleanup — and can sign in to Supabase itself for the bearer token.
