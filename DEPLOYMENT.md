# Deployment

## Prerequisites

| Resource | Where |
|---|---|
| Supabase project | [supabase.com](https://supabase.com) — Postgres + Auth |
| NVIDIA NIM API key (`nvapi-...`) | [build.nvidia.com](https://build.nvidia.com) |
| GitHub repo | push this project, enable Actions |
| Azure App Service (optional, backend) | create when ready — the deploy job is pre-wired |
| Vercel (optional, web) | import the `web/` folder |

## 1. Supabase (once)

1. Run `supabase/schema.sql` then `supabase/rls.sql` in the SQL Editor.
2. Create at least one user (Authentication → Users) for testing.
3. Note the **pooled (Session)** connection string, project URL, and publishable key.

## 2. Backend — Azure App Service for Containers

The CI pipeline (`docker` job) already builds and pushes `ghcr.io/<owner>/<repo>:<sha>` on every push to `main`. To enable the deploy job:

1. Create an Azure App Service (Linux, "Container" deployment option).
2. In the GitHub repo: Settings → Secrets and variables → Actions:
   - Repository **variable** `AZURE_APP_NAME` = the App Service name,
   - Repository **secret** `AZURE_PUBLISH_PROFILE` = the app's publish profile (Download publish profile → paste contents).
3. Push to `main`. The `deploy` job runs once `AZURE_APP_NAME` is set.

Application settings for the App Service:

| Setting | Value |
|---|---|
| `ConnectionStrings__Default` | Supabase pooled connection string |
| `Supabase__Url` | `https://<ref>.supabase.co` |
| `Nvidia__ApiKey` | `nvapi-...` |
| `Nvidia__ChatModel` | `nvidia/nemotron-3-ultra-550b-a55b` |
| `Nvidia__EmbeddingModel` | `nvidia/nemotron-3-embed-1b` |
| `Cors__AllowedOrigins__0` | your Vercel URL (e.g. `https://tecasist.vercel.app`) |

`Auth__Disabled` must stay unset/false in production — the dev bypass exists only for local curl testing.

Secrets can alternatively be stored in Azure Key Vault and referenced from App Settings.

## 3. Frontend — Vercel

1. Import the repo in Vercel, **Root Directory: `web`**.
2. Environment variables:

| Variable | Value |
|---|---|
| `NEXT_PUBLIC_SUPABASE_URL` | `https://<ref>.supabase.co` |
| `NEXT_PUBLIC_SUPABASE_ANON_KEY` | the project publishable key |
| `NEXT_PUBLIC_API_URL` | your Azure URL (e.g. `https://tecasist.azurewebsites.net`) |

3. In the Supabase dashboard add your Vercel URL under Authentication → URL Configuration → Redirect URLs.

## 4. Smoke test the deployment

```powershell
./scripts/smoke-test.ps1 `
  -BaseUrl https://<your-app>.azurewebsites.net `
  -SupabaseUrl https://<ref>.supabase.co `
  -AnonKey <publishable-key> `
  -Email <test-user> `
  -Password <test-password>
```

It signs in, uploads, ingests, streams a grounded answer, verifies the SSE frames, and cleans up.

## Operational notes

- `/health` for load-balancer probes; `/health/ready` for deploy gating (checks Postgres).
- Logs are structured JSON (Serilog compact format) — Azure Log Stream / Application Insights parse them directly.
- JWKS signing keys are cached 10 minutes and refreshed on rotation; no restart is needed when Supabase rotates keys.
- Re-embedding after a model change requires re-uploading documents; the `EmbeddingDimension2048` migration documents what a dimension change involves.
