using System.Net.Http.Headers;
using System.Net.Http.Json;
using FuelMeter.Core.Services.Interfaces;

namespace FuelMeter.Services;

/// <summary>
/// HttpClient wrapper that automatically attaches the JWT bearer token.
/// Registered as Singleton in MAUI (one per app lifetime).
/// </summary>
public class ApiClientService(HttpClient http) : IApiClientService
{
    /// <summary>
    /// Exposes the underlying HttpClient for callers that need to read
    /// non-JSON content (e.g. CSV exports) without going through
    /// ReadFromJsonAsync.
    /// </summary>
    public HttpClient GetHttpClient() => http;

    public void SetToken(string? token)
    {
        http.DefaultRequestHeaders.Authorization = token is not null
            ? new AuthenticationHeaderValue("Bearer", token)
            : null;
    }

    public async Task<TResponse?> GetAsync<TResponse>(string url)
    {
        var response = await http.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest body)
    {
        var response = await http.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest body)
    {
        var response = await http.PutAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    public async Task<TResponse?> DeleteAsync<TResponse>(string url)
    {
        var response = await http.DeleteAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }
}
