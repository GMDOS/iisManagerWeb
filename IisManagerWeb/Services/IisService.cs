using System.Net.Http.Json;
using IisManagerWeb.Models;

namespace IisManagerWeb.Services;

public class IisService : IIisService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public IisService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
    }

    public async Task<List<WebSite>> GetAllSites()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/iis");
            if (response.IsSuccessStatusCode)
            {
                var sites = await response.Content.ReadFromJsonAsync<List<WebSite>>();
                return sites ?? new List<WebSite>();
            }
            return new List<WebSite>();
        }
        catch
        {
            return new List<WebSite>();
        }
    }

    public async Task<WebSite?> GetSite(string id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/iis/{id}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<WebSite>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task StartSite(string id)
    {
        try
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/iis/{id}/start", null);
            response.EnsureSuccessStatusCode();
        }
        catch
        {
            // Ignora erros
        }
    }

    public async Task StopSite(string id)
    {
        try
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/iis/{id}/stop", null);
            response.EnsureSuccessStatusCode();
        }
        catch
        {
            // Ignora erros
        }
    }
} 