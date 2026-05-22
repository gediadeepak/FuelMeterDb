using System.Net.Http.Headers;
using System.Net.Http.Json;
using FuelMeter.Core.Services;
using FuelMeter.Core.Services.Interfaces;

namespace FuelMeter.Web.Services;

/// <summary>
/// Scoped HttpClient wrapper for Blazor Server.
/// Creates a fresh HttpClient per-request via IHttpClientFactory and reads
/// the JWT token directly from the circuit-scoped AuthStateService, so the
/// correct token is always sent regardless of DI lifetime differences.
/// </summary>
public class ApiClientService(IHttpClientFactory factory, AuthStateService authState) : IApiClientService
{
    private const string ClientName = "FuelMeterApi";

    // SetToken is kept for interface compatibility but is a no-op here —
    // the token is read from AuthStateService on every request.
    public void SetToken(string? token) { }

    public async Task<TResponse?> GetAsync<TResponse>(string url)
    {
        var response = await CreateClient().GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest body)
    {
        var response = await CreateClient().PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest body)
    {
        var response = await CreateClient().PutAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    public async Task<TResponse?> DeleteAsync<TResponse>(string url)
    {
        var response = await CreateClient().DeleteAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    // Creates a client from the pool and stamps the current circuit token onto it.
    private HttpClient CreateClient()
    {
        var client = factory.CreateClient(ClientName);
        client.DefaultRequestHeaders.Authorization = authState.Token is not null
            ? new AuthenticationHeaderValue("Bearer", authState.Token)
            : null;
        return client;
    }
}
