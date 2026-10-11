> Historical milestone: this explains an earlier implementation. Use README.md, learning-guide.md and api.md for the current full application; its old test scripts are in legacy-scripts/.

# Lesson 3: create and retrieve a booking

## What we are building

Select a flight, enter one passenger's details, create a Pending booking and retrieve it by reference. This implements the C and R of CRUD (Create, Read). Update and cancellation will follow. Pending is a local draft, not supplier confirmation or a ticket.

## Architecture

React BookingPanel -> POST /api/bookings -> BookingsController -> BookingService -> FlightSearchService + IBookingStore.

GET /api/bookings/{reference} reads that saved booking. InMemoryBookingStore uses a ConcurrentDictionary so simultaneous requests can read/write safely. Singleton registration keeps the store shared across requests. BookingService is scoped to a request. Memory data is lost on restart; the MSSQL milestone replaces this store with EF Core persistence.

## Why these parts exist

1. CreateBookingRequest is a DTO: the data the client is allowed to send. It contains no price, status, reference or timestamp. Those values belong to the server.
2. Data annotations reject missing/invalid fields with HTTP 400. The controller also rejects past dates and whitespace-only names.
3. The service searches again for the offer on the requested route/date. A mismatched offer returns 409. The price comes from the supplier result, never from the browser. The mock always returns its static fares; real availability and expiring quotes come later.
4. The service creates an immutable Booking record with a GUID reference, UTC creation timestamp and Pending state.
5. CreatedAtAction returns HTTP 201 Created, the booking body and a Location header pointing to its GET endpoint. GET returns 200 or 404.
6. React sends JSON using POST and Content-Type: application/json. On success it stores the result in state. Reload uses GET, proving the data is in the backend rather than only in React state.

## Run and explore

Start the API and React as in lesson 2. Select DAC-CXB, enter Demo Passenger and demo@example.com, then create a booking. Copy its reference into the lookup form. Restart the API and lookup again: it returns 404 because memory was reset.

Use fictional data. This local educational endpoint has no login or ownership checks yet. Before any public deployment we will add authentication and customer/agent ownership checks. Repeated POSTs currently create distinct bookings; idempotency is a later milestone. Do not automatically retry POST on network failure, since it may already have succeeded.

## HTTP exercises

Inspect Network in the browser: POST request JSON, 201 response, Location header, then GET 200. Try an unknown reference (404), invalid email (400), and mismatched route/offer (409) using the test script.

Run from the project root while the API is running:

```powershell
./scripts/booking-test.ps1
```

Read in this order: Contracts/CreateBookingRequest.cs, Models/Booking.cs, Services/InMemoryBookingStore.cs, Services/BookingService.cs, Controllers/BookingsController.cs, frontend BookingPanel.jsx.

Question to answer: Why does the frontend send an offer ID but no price? Because client input can be altered, and booking must use a server-checked fare.

Next: update contact details and cancel bookings, then Microsoft SQL Server relationships and EF Core migrations. Docker Desktop will provide our local SQL Server environment when we reach persistence.
