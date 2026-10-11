using System.Globalization;
using System.Xml;
using System.Xml.Linq;
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", mode = "Fictional supplier" }));
app.MapGet("/rest/flights", (string origin, string destination, DateOnly date) => {
    if (date.Day == 13) return Results.StatusCode(503);
    return Results.Ok(Offers("REST", origin, destination, date));
});
app.MapPost("/rest/{action}", (string action, Operation request) => action is "confirm" or "ticket" or "cancel" or "refund" ? Results.Ok(new { reference = $"REST-{request.BookingId:N}" }) : Results.NotFound());
app.MapPost("/soap", async (HttpRequest request) => {
    XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
    XNamespace travel = "urn:flightbooking:mock";
    try {
        using var stream = new StreamReader(request.Body);
        var raw = await stream.ReadToEndAsync(request.HttpContext.RequestAborted);
        using var reader = XmlReader.Create(new StringReader(raw), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        var xml = XDocument.Load(reader);
        var operation = xml.Root?.Element(soap + "Body")?.Elements().SingleOrDefault() ?? throw new FormatException();
        XElement response;
        if (operation.Name == travel + "Search") {
            var date = DateOnly.ParseExact(operation.Element(travel + "Date")!.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (date.Day == 14) throw new FormatException();
            var offers = Offers("SOAP", operation.Element(travel + "Origin")!.Value, operation.Element(travel + "Destination")!.Value, date);
            response = new(travel + "SearchResponse", offers.Select(o => new XElement(travel + "Offer", new XElement(travel + "Id", o.Id), new XElement(travel + "Airline", o.Airline), new XElement(travel + "Price", o.Price), new XElement(travel + "Seats", o.AvailableSeats))));
        } else if (operation.Name.Namespace == travel && operation.Name.LocalName is "confirm" or "ticket" or "cancel" or "refund") {
            var id = Guid.Parse(operation.Element(travel + "BookingId")!.Value);
            response = new(travel + "OperationResponse", new XElement(travel + "Reference", $"SOAP-{id:N}"));
        } else throw new FormatException();
        return Results.Text(new XDocument(new XElement(soap + "Envelope", new XElement(soap + "Body", response))).ToString(), "text/xml");
    } catch (Exception ex) when (ex is FormatException or XmlException or NullReferenceException or InvalidOperationException) {
        return Results.Text(new XElement(soap + "Envelope", new XElement(soap + "Body", new XElement(soap + "Fault", new XElement("faultcode", "soap:Client"), new XElement("faultstring", "Mock SOAP failure")))).ToString(), "text/xml", statusCode: 500);
    }
});
app.Run();
static IReadOnlyList<Offer> Offers(string supplier, string origin, string destination, DateOnly date) {
    if (origin != "DAC" || destination is not ("CXB" or "CGP")) return [];
    return supplier == "REST" ? [new("REST-001", "REST", "Demo Air", origin, destination, date, destination == "CXB" ? 5800 : 4200, "BDT", 6)] : [new("SOAP-001", "SOAP", "Sample Airways", origin, destination, date, destination == "CXB" ? 6500 : 4600, "BDT", 4)];
}
record Offer(string Id, string Supplier, string Airline, string Origin, string Destination, DateOnly DepartureDate, decimal Price, string Currency, int AvailableSeats);
record Operation(Guid BookingId);
