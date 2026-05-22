namespace FuelMeter.Core.Services.Interfaces;

/// <summary>
/// Thin abstraction over an HttpClient that injects the bearer token
/// before every authenticated call.
/// </summary>
public interface IApiClientService
{
    void SetToken(string? token);

    Task<TResponse?> GetAsync<TResponse>(string url);
    Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest body);
    Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest body);
    Task<TResponse?> DeleteAsync<TResponse>(string url);
}
