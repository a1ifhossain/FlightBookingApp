# Learn the complete application

Use the current README for startup. The first three lessons capture how we built the early versions. This guide covers the finished local portfolio app.

## 1. C#, OOP and data flow

Start with Complete/Contracts.cs. A DTO defines the allowed input/output shape. DataAnnotations validate fields. A record is useful for value-like results such as Offer. Entity classes represent mutable database records. Nullable properties express values which do not yet exist, such as a supplier reference on a Pending booking.

IFlightSupplier defines two capabilities: search and an operation. HttpSupplier is an abstract class sharing the HTTP client and response checking. RestSupplier and SoapSupplier inherit it and implement different protocol behavior. Controllers inherit ControllerBase to gain HTTP helpers. This demonstrates interfaces, encapsulation, inheritance and polymorphism for actual reasons.

LINQ filters authorized bookings, sorts fares, groups reports and maps entities to output records. EF LINQ becomes SQL when queries are executed with ToListAsync/SingleAsync; in-memory LINQ acts on data already loaded. Avoid loading every record to filter it in C#.

Exercise: explain why BookingRequest has neither TotalPrice nor Status, but BookingView contains both.

## 2. ASP.NET Core, dependency injection and async

Program.cs wires controllers, scoped TravelDb/BookingManager, Identity and a typed HttpClient. A scoped DbContext belongs to one request and must not be shared across threads. Supplier search uses Task.WhenAll for two independent HTTP requests, then combines normalized offers. It does not run parallel queries on the same DbContext.

await releases the request thread during I/O. Cancellation flows from the controller into HTTP and database calls. HttpClient has an eight-second timeout. Partial supplier failure produces offers plus a warning; total failure becomes 502. A timeout on a booking operation becomes 504. Read ApiExceptionHandler to see consistent ProblemDetails with a trace identifier.

Exercise: use a date on the 13th, then the 14th; inspect the response warnings in browser Network.

## 3. REST CRUD and business semantics

POST /bookings creates; GET reads; PUT /contact updates; DELETE cancels while preserving the audit trail. POST lifecycle endpoints represent business operations. DELETE never physically deletes financial history. A create returns 201 and Location, replay returns 200, missing/foreign booking 404, invalid input 400, stale versions/invalid transitions 409, missing login 401, restricted admin endpoint 403.

The frontend form state is maintained by useState. JSX renders data; event handlers perform requests; api.js encodes JSON and includes Authorization. It checks HTTP status because fetch only rejects for transport failures. Loading disables buttons and API errors appear next to the operation.

## 4. Database relationships and migrations

TravelDb extends IdentityDbContext, so EF manages Identity tables alongside our domain tables. One user owns many bookings. One booking has many passengers, tickets and events. Each ticket points to one passenger. Foreign keys prevent orphan records. Restrict on the ticket/passenger relationship avoids SQL Server multiple cascade paths.

Currency uses decimal(18,2), not floating point. DepartureDate is a date; audit timestamps are UTC datetimeoffset. Booking.Version is SQL Server rowversion, an automatically changing binary value, not a wall-clock timestamp. EF includes the original version in UPDATE predicates. If another request changes the row first, EF raises a concurrency exception and the API returns 409.

Migrations version the schema in Git. Read the initial Up/Down and snapshot. Never edit a previously applied migration to change a shared database; add a new migration. SaveChanges commits the booking, passenger and event records in a transaction. The role-change endpoint also uses an explicit transaction because multiple Identity calls otherwise commit separately.

Exercise: create two passengers and check Bookings/Passengers/Tickets in SSMS. Restart API and verify the booking remains.

## 5. MSSQL queries, procedures and indexes

Run scripts/sql-check.ps1 and read sql/learning.sql. INNER JOIN finds matching rows; LEFT JOIN retains rows without a match. GROUP BY summarizes a group. Joining passengers multiplies booking rows, so summing booking totals after that join can overcount revenue.

The BookingSummary stored procedure accepts a half-open UTC date interval. The admin sql-summary endpoint calls it with SqlParameter values, avoiding SQL text concatenation. Compare this with the LINQ reporting endpoint. Procedures can centralize reporting logic; they are not automatically faster than equivalent parameterized SQL.

Indexes accelerate selected lookups but cost storage and writes. OwnerId/CreatedAtUtc helps a user's booking list; Origin/Destination/DepartureDate helps route/date lookups; OwnerId/IdempotencyKey enforces a unique retry key. In SSMS enable Actual Execution Plan and STATISTICS IO. A tiny table may be scanned even with an index; that is often sensible. Do not claim performance improvements until measured on realistic data.

## 6. REST and SOAP integration

RestSupplier uses GET + JSON and POST + JSON. SoapSupplier builds an XML envelope with SOAP and travel namespaces, a SOAPAction header, and parses XML results/Faults. XElements escape text correctly. The XML parser prohibits DTDs and external resolution. Both adapters return the same Offer contract, hiding protocol differences from the booking workflow.

The separate mock service resembles an external boundary but is deliberately simple. Deterministic references make repeated supplier operations harmless in the simulator. Real GDS/NDC protocols, certificates, fare rules and ticketing agreements vary and are not implemented here.

## 7. Booking states, idempotency and reliability

Pending -> Confirmed -> Ticketed -> Cancelled -> Refunded. Pending/Confirmed can also be cancelled, but an unpaid cancellation cannot be refunded. Rules.Next defines allowed transitions. Ticketing creates one ticket per passenger and a fake payment reference. Cancellation keeps tickets/history for audit.

Creating a booking requires Idempotency-Key. A unique owner/key database index prevents concurrent duplicate creates. An identical retry returns the saved booking; using the key with a different body returns 409. React reuses the same key when retrying unchanged input after a lost response. New keys represent new bookings.

The backend checks supplier fares and seats again, and ignores a client-supplied price. Static supplier seats are not a real reservation. The SQL transaction protects local writes, not an HTTP supplier call. For real integration, an outbox, reconciliation job or saga would track what succeeded across systems. Do not blindly retry non-idempotent supplier requests.

## 8. Authentication and authorization

ASP.NET Core Identity hashes passwords, validates strength and issues short-lived protected bearer tokens (these are not JWTs). The browser holds its token in memory and includes it in Authorization headers. Refresh/reload signs out in this UI. Each booking lookup filters by owner unless the user is Admin. Guessing another booking reference does not grant access. Agents own their customer bookings; they cannot browse other agents' work.

Admin can assign roles. Role changes update the security stamp, and middleware rejects old sessions on later requests. Admin cannot demote themselves. The protected controllers accept bearer authentication; the React app does not rely on auth cookies. No public admin registration is provided. Seed accounts exist only because the local environment requests them.

Exercise: try to retrieve a customer's booking as agent. Expect 404. Try admin reports as customer. Expect 403.

## 9. Docker Desktop and networking

An image is a built application filesystem; a container is a running instance. compose.yaml starts SQL, supplier, API and web. Inside the Docker network, `sql` and `supplier` are DNS names; localhost would mean the current container. Browser ports are mapped to localhost on Windows. Nginx forwards /api to the backend. SQL and data-protection keys have named volumes that survive restarts.

The SQL healthcheck runs a query before the API applies migrations. `docker compose down` stops/removes containers but preserves named volumes. Avoid `down -v` unless you intentionally want to erase the local database. Rebuild after code changes with scripts/start.ps1. Use `docker compose logs --tail=60 api` for startup failures.

## 10. Tests, Git and interview practice

Unit tests cover allowed/forbidden state transitions, route/date checks, stale versions, SOAP Faults, partial/total supplier failures and cancellation. Integration checks use real HTTP and SQL Server for auth, relational writes, concurrency/version responses, idempotency and both full supplier workflows. These are different layers: passing unit tests does not prove the SQL schema works.

Use feature branches and small commits. Explain the changed behavior, how you tested it, and any limitations in a PR. CI repeats builds and tests; the workflow is included but has not run on GitHub until you publish a repository.

Practice explaining: What is DI? Why decimal for money? Why 409? How does a foreign key help? What happens if a supplier succeeds but the DB write fails? Why aren't four containers automatically four business microservices? How does a JOIN accidentally inflate revenue?
