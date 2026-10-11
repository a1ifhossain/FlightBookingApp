# Architecture and learning roadmap

Use one deployable application with clear folders first. This keeps debugging approachable. Later, extract Domain/Application/Infrastructure projects only when the boundaries are useful.

Request -> FlightsController -> FlightSearchService -> IFlightSupplier -> MockFlightSupplier
Response <- HTTP JSON <- ordered FlightOffer values

Controllers translate HTTP into application calls. Services coordinate business rules. Interfaces define capabilities. Supplier implementations hide external protocol details. Models describe data; later DTOs will separate API contracts from database entities.

## Incremental milestones

1. **Flight search (implemented):** C# classes/records, interface implementation, ControllerBase inheritance, LINQ, async/await, DI, GET routing, JSON, 200/400, cancellation and HTTP smoke checks.
2. **Booking CRUD:** create, retrieve, update contact details, cancel; POST/GET/PUT/DELETE semantics, 201/204/404/409, DTO validation. Use temporary memory storage to learn CRUD, then replace it. Reprice an offer before booking; do not trust client prices.
3. **SQL Server + EF Core:** install/configure a development SQL Server, add EF SQL Server provider, DbContext, migrations and seed data. Model Customer -> Bookings -> Passengers/Tickets; Booking -> FlightSegments. Explain foreign keys, navigation properties, decimal money types and transactions. Store UTC timestamps.
4. **SQL practice:** write SELECT/WHERE/JOIN/GROUP BY examples against our data; report bookings and revenue; add a parameterized reporting stored procedure. Compare query plans before/after indexes on booking references and route/date columns. Explain index write costs.
5. **REST supplier simulator:** separate local HTTP mock server; typed HttpClient, JSON DTO mapping, timeout/cancellation, supplier errors, repricing and booking. Retry safe searches selectively; protect booking with idempotency keys.
6. **SOAP supplier simulator:** local endpoint accepting a SOAP envelope and XML payload; build/parse namespaces and SOAP Faults, normalize offers through the same interface. This illustrates protocol handling, not a real GDS certification.
7. **Authentication and roles:** password hashing via established Identity components, token validation, customer/agent/admin authorization, ownership checks; keep development keys outside source control.
8. **Booking lifecycle:** pending/confirmed/ticketed/cancelled/refunded transitions, fake ticketing/payment, concurrency and duplicate-request protection, transaction boundaries. Explicitly separate cancellation from refund.
9. **Tests and resume release:** unit tests for rules, HTTP integration tests, database relationship/transaction tests and supplier failure tests; CI, API examples, setup guide, diagram and honest resume bullets.

Each milestone: explain the problem -> write a small slice -> run it -> test success and failure -> explain tradeoffs -> commit. The final project is a portfolio demonstration, not a production airline booking system.

## Full-stack scope

The project folder is FlightBookingApp. Add a React + JavaScript frontend at src/flight-booking-web. Build each feature end to end, starting with a search form and results connected to the existing API. Use Microsoft SQL Server (MSSQL) for persistent storage through EF Core's SQL Server provider. Teach relational SQL, joins, grouping, stored procedures and indexes against this database. Frontend and database setup are upcoming milestones.
