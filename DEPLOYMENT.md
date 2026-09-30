# Deployment — zero-cost, dashboard-only

Every resource below is **$0** on the free tiers, driven entirely from web dashboards (no Azure CLI, no Vercel CLI). Total: GitHub (public repo) $0 · GitHub Actions $0 · GHCR $0 · Azure App Service F1 $0 · Vercel Hobby $0 · Supabase Free $0 · NVIDIA NIM uses your existing free credits.

## Step 0 — hygiene before going public

1. **Rotate the Supabase database password** (Project Settings → Database → Reset database password) — the current one was exposed in a terminal log during development. Update it in your user-secrets afterwards.
2. Confirm no secrets are in the repo: they never were (user-secrets + `.env.local` are git-ignored), but verify with `git log -p | findstr /i "password nvapi"` if you want certainty.

## Step 1 — GitHub

1. Create a **public** repository (e.g. `tecasistnet`) at github.com/new — public gives you free Actions minutes and free GHCR storage.
2. Push the code:

```powershell
git remote add origin https://github.com/<your-username>/tecasistnet.git
git push -u origin master
```

3. Fix the README badge URL (`<your-username>/tecassistnet` placeholder).
4. The `ci-cd.yml` workflow runs on every push: build + 49 tests → web build → Docker image to `ghcr.io/<your-username>/tecasistnet`. Let it go green.
5. **Check the package is public**: github.com → your profile → Packages → `tecasistnet` → Package settings → Danger Zone → Change visibility → **Public**. (Public packages are free; private ones consume storage quota.)

## Step 2 — Azure App Service (Free F1, container)

> Why F1: it's genuinely $0 (no charge to the student credit). Trade-off: 60 CPU-minutes/day and the app sleeps after ~20 min idle, so the first request takes ~30–60s to cold-start. Perfect for a portfolio demo. If you'd rather avoid cold starts, pick B1 instead — it bills the $100 student credit (~$13/month), never a card.

### Create the web app

1. portal.azure.com → **Create a resource → Web App**.
2. Subscription: **Azure for Students** · Resource group: create `tecasist-rg` (also create it as "Free" region choice — pick any, e.g. East US).
3. Name: `tecasist-api-<something-unique>` (this is your `AZURE_APP_NAME` and your URL `https://<name>.azurewebsites.net`).
4. **Publish: Docker Container** · Operating System: **Linux** · Region: same as the resource group.
5. Pricing plan: **Create new** → change plan → **Dev / Test → Free (F1)**, 1 GB memory. (If F1 isn't listed for your region, try another US region.)
6. Docker tab: Options → **Private Registry** (configure even if the GHCR package is public — App Service requires explicit registry details for non-Docker-Hub registries):
   - URL: `https://ghcr.io`
   - Username: your GitHub username
   - Password: a **GitHub PAT** — github.com → Settings → Developer settings → Personal access tokens (classic) → Generate new: scope **`read:packages`** only
   - Image and tag: `ghcr.io/<your-username>/tecasistnet:latest`
7. Review + create → wait for deployment.

### Configure the app (Settings → Environment variables → App settings)

Add these app settings, then **Save**:

| Setting | Value |
|---|---|
| `WEBSITES_PORT` | `8080` ← **critical**: the container listens on 8080; without this App Service probes port 80 and the app never boots |
| `ConnectionStrings__Default` | Supabase **pooled (Session)** connection string with the rotated password |
| `Supabase__Url` | `https://<project-ref>.supabase.co` |
| `Nvidia__ApiKey` | `nvapi-...` |
| `Nvidia__ChatModel` | `nvidia/nemotron-3-ultra-550b-a55b` |
| `Nvidia__EmbeddingModel` | `nvidia/nemotron-3-embed-1b` |
| `Cors__AllowedOrigins__0` | your Vercel URL, e.g. `https://tecasist.vercel.app` (add `Cors__AllowedOrigins__1` = `http://localhost:3000` for local dev) |

Leave `Auth__Disabled` **unset** — production must require real JWTs.

### Wire CI to deploy

1. App Service → **Overview → Get publish profile** (downloads a `.PublishSettings` file; open it in a text editor and copy everything).
2. GitHub repo → **Settings → Secrets and variables → Actions**:
   - Tab **Secrets** → New repository secret: name `AZURE_PUBLISH_PROFILE`, value = the file contents.
   - Tab **Variables** → New repository variable: name `AZURE_APP_NAME`, value = the app name from step 2.3.
3. Push any commit to `main` (or re-run the workflow from the Actions tab). The gated `deploy` job now runs: the image is pushed to GHCR and the App Service is switched to the new tag + restarted.
4. First deploy: the F1 host pulls the image (~1–3 minutes) — watch App Service → **Container settings → Log stream**. Later restarts are faster (cached layers).

Verify: `https://<app-name>.azurewebsites.net/health` → `Healthy` (cold start delay on the very first hit is normal).

## Step 3 — Vercel (web)

1. vercel.com → **Add New… → Project** → Import your GitHub repository.
2. **Root Directory: `web`** (expand "Root Directory" and override) — Framework Preset auto-detects Next.js.
3. Environment Variables (Production + Preview):

| Variable | Value |
|---|---|
| `NEXT_PUBLIC_SUPABASE_URL` | `https://<project-ref>.supabase.co` |
| `NEXT_PUBLIC_SUPABASE_ANON_KEY` | the project publishable key |
| `NEXT_PUBLIC_API_URL` | `https://<app-name>.azurewebsites.net` |

4. **Deploy**. Note the domain (`https://<project>.vercel.app`).

## Step 4 — Supabase dashboard touches

1. **Authentication → URL Configuration**: Site URL = your Vercel domain; add the Vercel domain under Redirect URLs.
2. Confirm at least one user exists under **Authentication → Users** (create one for demos).

## Step 5 — verify end to end

```powershell
./scripts/smoke-test.ps1 `
  -BaseUrl https://<app-name>.azurewebsites.net `
  -SupabaseUrl https://<project-ref>.supabase.co `
  -AnonKey <publishable-key> `
  -Email <test-user> `
  -Password <test-password>
```

It signs in through your live Supabase, uploads to Azure, waits for ingestion, streams a grounded answer over SSE, and cleans up. Then sign in through the Vercel app in a browser and chat with a document.

## Cost summary & gotchas

| Resource | Tier | Cost |
|---|---|---|
| GitHub repo + Actions | public repo | $0 (unlimited minutes) |
| GHCR image | public package | $0 |
| Azure App Service | F1 Free | $0 (60 CPU-min/day, cold starts, no Always On) |
| Vercel | Hobby | $0 |
| Supabase | Free | $0 |
| NVIDIA NIM | your account | existing free credits |

Gotchas: `WEBSITES_PORT=8080` is mandatory; the CORS origin must match the Vercel URL exactly (scheme + host, no trailing slash); F1 sleeps → first request is slow (mention it in interviews as a deliberate free-tier trade-off); if the deploy job fails, check that `AZURE_APP_NAME` is a **variable** and `AZURE_PUBLISH_PROFILE` is a **secret** (both under the Actions section, not Environments).
