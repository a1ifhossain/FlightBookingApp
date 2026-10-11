# Verification record — 11 October 2026

Verified against the installed default SQL Server instance on this computer. Database files are in D:\MSSQL\MSSQL17.MSSQLSERVER\MSSQL\DATA\.

- API and mock supplier compile; React production build passes.
- 16 unit tests pass.
- 44 HTTP integration checks pass against real MSSQL.
- Additional checks pass for registration, role assignment, old-session revocation, simultaneous duplicate creates and competing updates.
- Bookings and passenger relationships survive a full API restart.
- SQL joins, grouping, reporting procedure and index exercises execute successfully.
- Both migrations are applied; ticket/passenger orphan count is zero.
- Browser verified: search, customer login, passenger booking, supplier confirmation, mock ticket issuance, admin reporting and role controls.
- Docker Compose configuration validates. Full Docker image builds were not reverified after the earlier engine storage error; installed MSSQL is the verified local run path.
- Vercel/Render configuration and instructions are prepared; no cloud deployment or GitHub upload has occurred.

These checks use fictional accounts, passengers, inventory and payments. Integration scripts add sample records. CI repeats the checks when the user uploads the repository.
