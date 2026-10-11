# Current API contract

All responses use JSON except SOAP traffic between backend and simulator. Authorization header: `Bearer <accessToken>`. Login is POST /api/auth/login?useCookies=false with email/password. Registration is POST /api/auth/register. Identity also provides refresh, password-management and verification endpoints; email delivery is not configured in this demo. UI does not use refresh.

| Method | Route | Access / purpose |
|---|---|---|
| GET | /health, /health/ready | Liveness / database readiness |
| GET | /api/flights/search | Public; origin, destination, departureDate query params |
| POST | /api/auth/register | Public customer registration |
| POST | /api/auth/login?useCookies=false | Public; returns protected bearer token |
| GET | /api/account/me | Signed-in identity/roles |
| POST | /api/bookings | Signed-in; Idempotency-Key required |
| GET | /api/bookings?page=1&pageSize=20 | Own bookings; admin sees all |
| GET | /api/bookings/{reference} | Owner or admin |
| PUT | /api/bookings/{reference}/contact | Email and current version |
| POST | /api/bookings/{reference}/{action} | confirm/ticket/cancel/refund; current version |
| DELETE | /api/bookings/{reference} | Cancel, preserve audit; If-Match contains version |
| GET | /api/admin/users | Admin; up to 100 users |
| PUT | /api/admin/users/{id}/role | Admin; Customer/Agent/Admin |
| GET | /api/admin/reports | Admin; EF LINQ grouping |
| GET | /api/admin/sql-summary | Admin; parameterized stored procedure |

Booking creation body:

```json
{
  "offerId": "REST-001",
  "supplier": "REST",
  "origin": "DAC",
  "destination": "CXB",
  "departureDate": "2026-12-20",
  "contactEmail": "demo@example.com",
  "passengers": [{"fullName": "Demo Passenger"}]
}
```

Use a future date. The creation response contains reference, unitPrice, totalPrice, status, passengers, tickets, events and version. Pass the latest version back on an update/action:

```json
{"version":"base64-rowversion-value"}
```

Search response is now `{ "offers": [...], "warnings": [...] }`, replacing the original bare array. Earlier milestone smoke scripts target the historical API; use integration-test.ps1 for the current system.

## Troubleshooting

- API root `/` has no HTML page; open frontend 8088 (Docker) or 5173 (Vite).
- 401: login again. 403: account lacks role. 404: nonexistent/foreign booking or malformed route.
- 409: refresh stale booking, use valid lifecycle action, or resolve reused idempotency key.
- 502/504: inspect supplier service logs and readiness. Day 13/14 failures are deliberate test scenarios.
- SQL startup failed: confirm Docker Linux engine and enough RAM, then inspect SQL logs.
- Port conflict: stop earlier local servers before starting Docker API/supplier.
- Database login failure after editing SQL password: a persisted SQL volume keeps its original password; preserve your original .env rather than regenerating it.
