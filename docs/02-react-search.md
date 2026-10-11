> Historical milestone: this explains an earlier implementation. Use README.md, learning-guide.md and api.md for the current full application; its old test scripts are in legacy-scripts/.

# Lesson 2: React meets the API

## Run it

Terminal 1, from FlightBookingApp:

```powershell
dotnet run --project src/FlightBooking.Api --urls http://localhost:5080
```

Terminal 2:

```powershell
cd src/flight-booking-web
npm ci
npm run dev
```

Open http://localhost:5173. The API root at port 5080 has no webpage: a 404 there is expected. Its search route and /health are the useful endpoints. Stop a previously running API before starting another on the same port.

## Understand the files

- index.html provides the root element. main.jsx mounts React into it.
- App.jsx defines a component: a function describing the UI using JSX, HTML-like syntax inside JavaScript.
- useState remembers form values, results, loading status and errors. Calling a setter tells React to render updated UI.
- Controlled inputs use value and onChange: React state is the source of the form's values.
- onSubmit prevents a full page reload, then awaits searchFlights.
- api.js encodes query parameters, calls fetch and reads JSON. It checks response.ok because fetch does not reject automatically on HTTP 400/500.
- offers.map turns each returned offer into a card. The stable offer ID is its React key.
- vite.config.js forwards /api requests to port 5080. The browser calls its own frontend origin, so no CORS change is needed in this local setup. This proxy is development-only; deployment routing will be configured when we introduce Docker.

## Request journey

Search click -> form state -> URLSearchParams -> HTTP GET -> C# controller -> service -> mock supplier -> JSON -> React state -> offer cards.

Frontend validation helps the user; backend validation still protects the API when someone bypasses the page. Prices come from the backend. No booking button is active because booking does not exist yet.

## Check and learn

1. Search DAC to CXB: two offers, 5800 BDT first.
2. Search DAC to CGP: one offer at 4200 BDT.
3. Search CXB to DAC: an empty-results message.
4. Select identical airports: a helpful error.
5. Stop the API and search: a failure message; start it again and retry.
6. Narrow the browser: form and cards stack on smaller screens.

Open browser developer tools, Network tab. Search and inspect the request query parameters, 200 status and JSON response. Match each JSON property to the C# FlightOffer record and the JSX card.

Exercise: change the heading, then explain which file changed the page and why no C# edit was needed.

Next: booking DTOs, state transitions and CRUD; then MSSQL persistence with EF Core. Docker Desktop follows the first working frontend/API flow.
