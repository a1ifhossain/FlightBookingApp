using System.Net;
using FlightBooking.Api.Complete;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
namespace FlightBooking.Tests;
public sealed class SupplierTests {
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(respond(request)); }
    }
    private static HttpClient Client(Func<HttpRequestMessage, HttpResponseMessage> respond) => new(new Handler(respond)) { BaseAddress = new("http://mock") };
    [Fact] public async Task RestFailureMapsToGatewayError() {
        var supplier = new RestSupplier(Client(_ => new(HttpStatusCode.ServiceUnavailable)));
        Assert.Equal(502, (await Assert.ThrowsAsync<ApiError>(() => supplier.SearchAsync("DAC", "CXB", Rules.Today, default))).Status);
    }
    [Fact] public async Task SoapFaultIsRecognizedByNamespace() {
        var client = Client(_ => new(HttpStatusCode.InternalServerError) { Content = new StringContent("<s:Envelope xmlns:s='http://schemas.xmlsoap.org/soap/envelope/'><s:Body><s:Fault><faultstring>Failure</faultstring></s:Fault></s:Body></s:Envelope>") });
        Assert.Equal(502, (await Assert.ThrowsAsync<ApiError>(() => new SoapSupplier(client).SearchAsync("DAC", "CXB", Rules.Today, default))).Status);
    }
    [Fact] public async Task BothSupplierFailuresReturn502() {
        var gateway = new SupplierGateway(Client(_ => new(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("<empty/>") }), NullLogger<SupplierGateway>.Instance);
        Assert.Equal(502, (await Assert.ThrowsAsync<ApiError>(() => gateway.SearchAsync("DAC", "CXB", Rules.Today, default))).Status);
    }
    [Fact] public async Task OneSupplierFailureStillReturnsOffersAndWarning() {
        var client = Client(request => request.RequestUri!.AbsolutePath == "/soap" ? new(HttpStatusCode.InternalServerError) { Content = new StringContent("<empty/>") } : new(HttpStatusCode.OK) { Content = new StringContent("[{\"id\":\"R\",\"supplier\":\"REST\",\"airline\":\"Demo\",\"origin\":\"DAC\",\"destination\":\"CXB\",\"departureDate\":\"2026-12-01\",\"price\":5800,\"currency\":\"BDT\",\"availableSeats\":6}]", System.Text.Encoding.UTF8, "application/json") });
        var result = await new SupplierGateway(client, NullLogger<SupplierGateway>.Instance).SearchAsync("DAC", "CXB", Rules.Today, default);
        Assert.Single(result.Offers); Assert.Single(result.Warnings);
    }
    [Fact] public async Task CancellationIsPropagated() {
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new RestSupplier(Client(_ => new(HttpStatusCode.OK))).SearchAsync("DAC", "CXB", Rules.Today, cts.Token));
    }
}
