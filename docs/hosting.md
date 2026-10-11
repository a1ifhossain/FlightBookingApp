# GitHub and optional free hosting

The app is designed to run locally with installed Microsoft SQL Server. Publishing is a separate step: no GitHub repository or cloud resources have been created by this task.

## 1. Prepare GitHub

From the project root:

```powershell
git status
git check-ignore .env
git add .
git diff --cached --stat
git commit -m "Build full-stack flight booking portfolio application"
```

Review the staged files before committing. Do not include `.env`, passwords, connection strings with credentials, build output, `.vs` or dependencies. Keep `.env.example` as a placeholder guide. If Git asks for your identity, configure your own name and email. Create a GitHub repository, then use the repository's displayed remote URL and push instructions. Use README screenshots and describe the suppliers/payments as mocks.

The included CI workflow runs unit tests, frontend builds, Docker integration tests and SQL exercises. It has not run on GitHub until the repository is uploaded.

## 2. Hosting architecture

```text
Vercel: React static frontend
        ↓ HTTPS + bearer token
Render: ASP.NET Core API
        ├── Azure SQL / externally hosted Microsoft SQL Server
        └── Render: fictional REST/SOAP supplier
```

Vercel supports the Vite frontend. Use Render Docker web services for the long-running ASP.NET applications. Your MSSQL on D: is for local development; a hosted API cannot connect to `localhost` on your computer.

A durable SQL Server-compatible database must be hosted separately. Microsoft's Azure SQL free offer currently includes monthly limits of 100,000 vCore seconds and 32 GB per database, with subscription/eligibility conditions. Choose the free-limit behavior that pauses usage rather than enabling paid overages if you want to stay within the offer. Check the portal's current pricing and eligibility before creating it. Do not replace SQL Server with PostgreSQL simply because Render offers it; that changes the project's intended database.

Sources: [Vercel Vite deployment](https://vercel.com/docs/frameworks/frontend/vite), [Render free service limitations](https://render.com/docs/free), [Azure SQL free offer](https://learn.microsoft.com/en-us/azure/azure-sql/database/free-offer?view=azuresql).

## 3. Deploy the mock supplier on Render

Create a Docker web service from your GitHub repository:

- Dockerfile: `src/FlightBooking.MockSupplier/Dockerfile`
- Docker context: repository root (`.`)
- Plan: Free, if offered for your account
- Health path: `/health`
- Environment: `ASPNETCORE_HTTP_PORTS=8080`

Copy the assigned public HTTPS URL. Use that exact URL as the API's `Supplier__Url`; the free supplier may need time to wake after inactivity.

## 4. Deploy the API on Render

Create another Docker web service:

- Dockerfile: `src/FlightBooking.Api/Dockerfile`
- Docker context: repository root (`.`)
- Health path: `/health`

Set these environment variables in Render, not in Git:

| Variable | Value |
|---|---|
| `ASPNETCORE_HTTP_PORTS` | `8080` |
| `ConnectionStrings__SqlServer` | External SQL Server/Azure SQL connection string; Encrypt=True, TrustServerCertificate=False |
| `Supplier__Url` | Actual supplier HTTPS URL from step 3 |
| `Supplier__TimeoutSeconds` | `90` to tolerate a free supplier wake-up |
| `Frontend__Origins` | Exact frontend origin, e.g. https://your-app.vercel.app; comma-separated for multiple known origins |
| `Database__Initialize` | `true` for the initial single-instance demo; applies reviewed migrations and seeds roles |
| `Seed__Password` | Strong unique demo account password; choose it privately |

Do not configure a local filesystem data-protection directory on a Render free service expecting it to be durable. The filesystem is ephemeral: after API restarts, existing tokens may stop working and users sign in again. A production service needs durable keys, HTTPS and a restricted database account; the current setup is a portfolio demonstration.

Review the cloud SQL firewall and permit only the necessary hosting connections. Avoid exposing your Windows SQL Server to the public internet. `Database__Initialize=true` needs migration permissions; for a shared deployment, apply migrations separately and use a more restricted runtime account.

The repository also includes `render.yaml` as an optional Blueprint for the API and supplier; secrets and external URLs require values during setup. Neither database nor frontend resources are created by that file.

## 5. Deploy React on Vercel

Import your repository:

- Root directory: `src/flight-booking-web`
- Framework: Vite
- Build command: `npm run build`
- Output directory: `dist`
- Node.js: 24
- Environment: `VITE_API_BASE_URL=https://YOUR-ACTUAL-API.onrender.com/api`

The VITE variable is compiled into public browser JavaScript. It contains only an API URL, never a secret. After changing it, rebuild/redeploy. Set the resulting Vercel origin in the API's `Frontend__Origins` and redeploy the API. The explicit CORS allowlist accepts bearer requests from that frontend.

You can also use a Render static site for React with the same build/output settings and public API variable.

## 6. Verify the hosted demo

Open the deployed frontend and check:

1. Search returns both offers.
2. Register/sign in works.
3. Create, confirm, mock ticket, cancel and refund a fictional booking.
4. Customer cannot see another owner's bookings.
5. Admin can load reports and roles.
6. API restart preserves SQL records; logging in again may be needed.
7. Browser console shows no CORS or mixed-content errors.

Render free web services currently sleep after 15 minutes without traffic and have ephemeral local storage. Expect cold starts. The UI does not automatically retry booking operations. If an unchanged create response is lost, retrying the same form keeps its idempotency key. Requests after navigating away or reloading can use a new key, so check My bookings before trying again.

Keep the README honest: mock airlines, mock money, single application backend, static inventory and a separate simulator. No live GDS, NDC, airline or payment credentials are required.
