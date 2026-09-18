using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Ec2Manager.Api.DTOs;

namespace Ec2Manager.Api.Services;

public class CloudServiceClient
{
    private readonly HttpClient _http;

    public CloudServiceClient(HttpClient http, IConfiguration config)
    {
        var baseUrl = config["CloudService:BaseUrl"]
            ?? throw new InvalidOperationException("CloudService:BaseUrl is not configured");
        var apiKey = config["CloudService:ApiKey"]
            ?? throw new InvalidOperationException("CloudService:ApiKey is not configured");

        _http = http;
        _http.BaseAddress = new Uri(baseUrl);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public async Task<List<JsonElement>> ListInstancesRaw(string accountKey, string? region, List<string>? statuses, string? search)
    {
        var payload = new
        {
            accountKey,
            region,
            statuses,
            search
        };
        var resp = await _http.PostAsJsonAsync("/instances/list", payload, JsonOpts);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<List<JsonElement>>(JsonOpts);
        return result ?? new List<JsonElement>();
    }

    public async Task<JsonElement> StartInstances(string accountKey, string region, List<string> instanceIds, bool dryRun)
    {
        var payload = new { accountKey, region, instanceIds, dryRun };
        var resp = await _http.PostAsJsonAsync("/instances/start", payload, JsonOpts);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
    }

    public async Task<JsonElement> StopInstances(string accountKey, string region, List<string> instanceIds, bool dryRun)
    {
        var payload = new { accountKey, region, instanceIds, dryRun };
        var resp = await _http.PostAsJsonAsync("/instances/stop", payload, JsonOpts);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
    }

    public async Task<List<string>> GetRegions(string accountKey)
    {
        var resp = await _http.GetAsync($"/accounts/{accountKey}/regions");
        resp.EnsureSuccessStatusCode();
        var doc = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        return doc.GetProperty("regions").EnumerateArray().Select(r => r.GetString()!).ToList();
    }

    public async Task<List<AccountMetadataDto>> GetAccounts()
    {
        var resp = await _http.GetAsync("/accounts");
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<List<AccountMetadataDto>>(JsonOpts);
        return result ?? new List<AccountMetadataDto>();
    }
}
