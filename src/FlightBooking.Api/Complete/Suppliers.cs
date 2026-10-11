using System.Net.Http.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;
namespace FlightBooking.Api.Complete;
public interface IFlightSupplier {
    Task<IReadOnlyList<Offer>> SearchAsync(string origin, string destination, DateOnly date, CancellationToken ct);
    Task<SupplierResult> OperateAsync(string action, Guid bookingId, CancellationToken ct);
}
public abstract class HttpSupplier(HttpClient client) : IFlightSupplier {
    protected HttpClient Client { get; } = client;
    public abstract Task<IReadOnlyList<Offer>> SearchAsync(string origin, string destination, DateOnly date, CancellationToken ct);
    public abstract Task<SupplierResult> OperateAsync(string action, Guid bookingId, CancellationToken ct);
    protected static void Check(HttpResponseMessage response) {
        if (!response.IsSuccessStatusCode) throw new ApiError(502, "Mock supplier rejected the request. Please try later.");
    }
}
public sealed class RestSupplier(HttpClient client) : HttpSupplier(client) {
    public override async Task<IReadOnlyList<Offer>> SearchAsync(string origin, string destination, DateOnly date, CancellationToken ct) {
        using var response = await Client.GetAsync($"/rest/flights?origin={Uri.EscapeDataString(origin)}&destination={Uri.EscapeDataString(destination)}&date={date:yyyy-MM-dd}", ct);
        Check(response);
        return await response.Content.ReadFromJsonAsync<List<Offer>>(ct) ?? [];
    }
    public override async Task<SupplierResult> OperateAsync(string action, Guid bookingId, CancellationToken ct) {
        using var response = await Client.PostAsJsonAsync($"/rest/{action}", new { bookingId }, ct);
        Check(response);
        return await response.Content.ReadFromJsonAsync<SupplierResult>(ct) ?? throw new ApiError(502, "Invalid supplier response.");
    }
}
public sealed class SoapSupplier(HttpClient client) : HttpSupplier(client) {
    private static readonly XNamespace Soap = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace Travel = "urn:flightbooking:mock";
    private async Task<XDocument> Send(string operation, IEnumerable<XElement> fields, CancellationToken ct) {
        var envelope = new XDocument(new XElement(Soap + "Envelope", new XElement(Soap + "Body", new XElement(Travel + operation, fields))));
        using var request = new HttpRequestMessage(HttpMethod.Post, "/soap") { Content = new StringContent(envelope.ToString(), Encoding.UTF8, "text/xml") };
        request.Headers.Add("SOAPAction", $"\"urn:flightbooking:mock/{operation}\"");
        using var response = await Client.SendAsync(request, ct);
        var xml = await response.Content.ReadAsStringAsync(ct);
        using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        var document = XDocument.Load(reader);
        if (document.Descendants(Soap + "Fault").Any()) throw new ApiError(502, "SOAP supplier returned a fault.");
        Check(response);
        return document;
    }
    public override async Task<IReadOnlyList<Offer>> SearchAsync(string origin, string destination, DateOnly date, CancellationToken ct) {
        var xml = await Send("Search", [new(Travel + "Origin", origin), new(Travel + "Destination", destination), new(Travel + "Date", date.ToString("yyyy-MM-dd"))], ct);
        return xml.Descendants(Travel + "Offer").Select(e => new Offer(e.Element(Travel + "Id")!.Value, "SOAP", e.Element(Travel + "Airline")!.Value, origin, destination, date, decimal.Parse(e.Element(Travel + "Price")!.Value, System.Globalization.CultureInfo.InvariantCulture), "BDT", int.Parse(e.Element(Travel + "Seats")!.Value))).ToArray();
    }
    public override async Task<SupplierResult> OperateAsync(string action, Guid bookingId, CancellationToken ct) {
        var xml = await Send(action, [new(Travel + "BookingId", bookingId)], ct);
        return new(xml.Descendants(Travel + "Reference").Single().Value);
    }
}
public sealed class SupplierGateway(HttpClient client, ILogger<SupplierGateway> logger) {
    public IFlightSupplier Get(string supplier) => supplier switch { "REST" => new RestSupplier(client), "SOAP" => new SoapSupplier(client), _ => throw new ApiError(400, "Unknown supplier.") };
    public async Task<SearchResponse> SearchAsync(string origin, string destination, DateOnly date, CancellationToken ct) {
        async Task<(IReadOnlyList<Offer> offers, string? warning)> Search(string supplier) {
            try { return (await Get(supplier).SearchAsync(origin, destination, date, ct), null); }
            catch (Exception ex) when (!ct.IsCancellationRequested && (ex is HttpRequestException or TaskCanceledException or ApiError or XmlException)) {
                logger.LogWarning("Search from {Supplier} failed with {ErrorType}", supplier, ex.GetType().Name);
                return ([], $"{supplier} supplier is temporarily unavailable.");
            }
        }
        var results = await Task.WhenAll(Search("REST"), Search("SOAP"));
        if (results.All(x => x.warning != null)) throw new ApiError(502, "Both mock suppliers are unavailable.");
        return new(results.SelectMany(x => x.offers).OrderBy(x => x.Price).ToArray(), results.Where(x => x.warning != null).Select(x => x.warning!).ToArray());
    }
}
