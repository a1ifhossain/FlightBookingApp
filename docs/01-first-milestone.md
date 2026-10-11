> Historical milestone: this explains an earlier implementation. Use README.md, learning-guide.md and api.md for the current full application; its old test scripts are in legacy-scripts/.

# Lesson 1: first runnable flight search

## 1. Start the application

Follow the README run commands. `dotnet build` compiles C# and catches type errors. `dotnet run` starts an HTTP server. `localhost:5080` means your own computer on port 5080. `/health` confirms the server responds; it does not yet check a database.

## 2. Follow one request

GET /api/flights/search?origin=DAC&destination=CXB&departureDate=2026-12-01

GET reads data. The route chooses FlightsController.Search. FromQuery maps URL values into FlightSearchRequest. ApiController runs validation and returns 400 when a required field or airport-code format is invalid. The method normalizes lowercase input, checks business constraints, then calls FlightSearchService. Ok serializes the result as JSON with HTTP 200. No matching flights is a valid empty search, so it returns 200 with [].

## 3. Understand C# and OOP in this code

A class groups data and behavior. FlightSearchRequest is a class with properties. FlightOffer is a record for a returned value. `decimal` is suitable for currency amounts; `6500m` is a decimal literal. `DateOnly` represents a travel date without an accidental time component. `sealed` stops another class from inheriting a type.

FlightsController inherits ControllerBase: it gains HTTP helpers such as Ok and Problem. That is inheritance. MockFlightSupplier implements IFlightSupplier: it promises to provide SearchAsync. That is interface implementation, which lets us replace the supplier without changing the controller. These are different OOP mechanisms; we do not invent an airline inheritance hierarchy just to demonstrate inheritance.

## 4. Dependency injection

Program.cs registers IFlightSupplier with MockFlightSupplier. ASP.NET constructs dependencies and passes FlightSearchService into the controller's constructor. AddSingleton shares the stateless mock across requests; AddScoped creates a service per request. Later a database context will also be scoped. Do not put mutable customer booking data in this singleton.

## 5. LINQ and async

Where keeps offers matching the route. OrderBy sorts ascending price. ToArray executes the query and materializes results. Think: filter -> sort -> produce a collection.

Task represents eventual completion. await waits without holding a request thread during genuine asynchronous I/O. Our mock uses Task.FromResult because its data is already available: it performs no network I/O and gains no asynchronous performance benefit. We keep the async interface so a real local HTTP simulator can replace it later. CancellationToken lets downstream work stop when a request is cancelled.

## 6. Try, observe, explain

- Search DAC to CXB: two offers, cheaper first.
- Use lowercase dac/cxb: same result.
- Search CXB to DAC: empty array (the mock has no return flights).
- Use origin=DA or omit departureDate: 400 validation response.
- Use the same airport twice or a past date: 400 problem response.

Exercise: add another fictional DAC-CXB offer at 6100 BDT. Predict where it appears before running the request. Then explain why changing MockFlightSupplier does not require changing FlightsController.

Next lesson: booking CRUD with explicit booking states, DTOs and server-controlled prices. Database persistence and authentication follow in their own milestones so you can understand each change.

Reference: https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-10.0
