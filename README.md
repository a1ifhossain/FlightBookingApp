# Flight Booking App

A full-stack flight booking application built with C# / ASP.NET Core 10, React with JavaScript, Microsoft SQL Server and Entity Framework Core. It supports flight search, passenger management, bookings, mock ticketing, cancellations and refunds, with customer, agent and admin roles. Local REST and SOAP supplier services simulate airline integrations.

## Run locally with SQL Server

SQL Server's default instance is `localhost`. Use Windows authentication; the database uses your SQL Server's configured data location. Install .NET 10 SDK, Node.js 24, Git and PowerShell 7.

Open three terminals in the project folder:

```powershell
./scripts/setup.ps1
./scripts/dev-api.ps1
```

```powershell
dotnet run --no-launch-profile --project src/FlightBooking.MockSupplier --urls http://127.0.0.1:5090
```

```powershell
cd src/flight-booking-web
npm ci
npm run dev
```

Open **http://127.0.0.1:5173**. The API creates/applies migrations for the `FlightBooking` database. Use fictional details and the demo password in your ignored local `.env` file. For a named instance, run `./scripts/dev-api.ps1 -SqlServer 'localhost\INSTANCE'`.



## Demo login credentials

The API creates three demo accounts on initial startup:

| Email | Role | Access |
|---|---|---|
| customer@example.com | Customer | Own bookings |
| agent@example.com | Agent | Own bookings for passengers; agent workspace |
| admin@example.com | Admin | All bookings, user/agent roles, reports |

**Password for all three accounts:** the value of `DEMO_PASSWORD` in your local `.env` file at the project root.

Run `./scripts/setup.ps1` before starting the API. It generates a password for your installation and preserves an existing `.env`. To display your demo password in PowerShell:

```powershell
Get-Content .env | Select-String '^DEMO_PASSWORD='
```

Copy the value after `DEMO_PASSWORD=` into the website's password field. Each installation generates its own password; there is no shared password in the repository. Keep `.env` out of Git.
You can register your own customer account. Passwords need 10+ characters, uppercase, lowercase, digit and symbol. Sessions are kept only in frontend memory: reloading the page signs you out. No real email delivery is configured.

## Try the full journey

1. Search DAC → CXB or DAC → CGP on a future date.
2. Select either the REST or SOAP supplier offer.
3. Sign in, add 1–6 fictional passengers (subject to mock seat availability), and create a booking.
4. Confirm with the mock supplier.
5. Mock pay and issue tickets.
6. Update contact details, cancel, then mock refund.
7. Open My bookings and load saved bookings. Admin accounts can see all bookings.
8. Sign in as admin to load reporting and assign user roles.

Dates on the **13th** simulate a REST failure. Dates on the **14th** simulate a SOAP Fault. The other supplier still returns offers with a warning. Suppliers are local HTTP endpoints, not live airline/GDS/NDC services.

## Architecture

```mermaid
flowchart LR
    Browser[React browser UI] --> Web[Vite proxy]
    Web --> API[ASP.NET Core controllers]
    API --> Business[BookingManager / rules]
    Business --> EF[EF Core / Identity]
    EF --> SQL[(Microsoft SQL Server)]
    Business --> Adapter[REST and SOAP adapters]
    Adapter --> Mock[Local mock supplier service]
```

The main backend is a **modular monolith**. A separate mock supplier service exposes REST and SOAP endpoints, which the backend consumes through HTTP adapters.

The compiled backend is in `src/FlightBooking.Api/Complete/`. Earlier milestone code is archived in docs/legacy-code as reference material and is outside the compiled applications. Lessons 1–3 describe those earlier stages; the current API contracts are documented in [docs/api.md](docs/api.md).

```text
src/FlightBooking.Api/Complete/     Models, DTOs, rules, controllers, persistence, integration
src/FlightBooking.Api/Migrations/   EF-generated initial schema and model snapshot
src/FlightBooking.MockSupplier/     Fictional REST + SOAP HTTP service
src/flight-booking-web/             React + JavaScript
tests/FlightBooking.Tests/          Business rules and supplier adapter tests
scripts/                           Startup, API integration and SQL checks
sql/learning.sql                   JOIN/GROUP BY/procedure/index exercises
docs/                              Learning guide, architecture and API contracts
compose.yaml                       Local Docker Desktop stack
```

## Verify

```powershell
dotnet test tests/FlightBooking.Tests
./scripts/integration-test.ps1
./scripts/sql-check.ps1
npm run build --prefix src/flight-booking-web
```

The integration checks run against real SQL Server through the application and create fictional test bookings. They cover authentication, ownership, roles, both supplier protocols, server-controlled prices, idempotency, stale updates, lifecycle rules, ticket/passenger relationships and reporting.

## EF Core migrations

```powershell
dotnet tool restore
dotnet ef migrations add DescribeYourChange --project src/FlightBooking.Api
```

Review the generated migration. Local startup applies migrations when `Database__Initialize=true`. The initial schema has users, roles, bookings, passengers, tickets and event history, decimal currency precision, foreign keys, unique idempotency keys, rowversion concurrency and query indexes. A second migration installs the reporting stored procedure. For a shared deployment, apply reviewed migrations separately instead of having multiple API instances apply them.

## Contributing

```powershell
git status
git diff
git add .
git commit -m "Build full-stack mock flight booking application"
git switch -c feature/next-improvement
```

`.env`, dependencies, build output and test artifacts are ignored. CI builds and tests the code on pushes and pull requests.


## Practical limits

This application uses simulated airline inventory and payment workflows. Inventory is static and does not decrement; supplier operations return deterministic fictional references. Payments/refunds are simulated. It does not implement live fares, GDS/NDC certification, real payment processing, seat holds, multi-city itineraries or an outbox/saga for cross-system recovery. EF commits related local writes atomically, but an external supplier call and database commit cannot share a transaction. Real suppliers need durable operation tracking and reconciliation. Public deployment requires HTTPS, restricted database credentials, managed secrets and operational hardening.

See [docs/learning-guide.md](docs/learning-guide.md) for an explanation of the application components and workflows.


## Preview


![Flight search preview](docs/screenshots/latest-homepage.png)

![Sign-in preview](docs/screenshots/latest-signin.png)
