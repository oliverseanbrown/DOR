using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using RugbyManager.Core.Competition;
using RugbyManager.Core.Persistence;

namespace RugbyManager.Web.Services;

public sealed record CloudSaveSummary(string Id, string Name, DateTimeOffset UpdatedAt);

/// <summary>
/// Saves/loads full careers to the <c>careers</c> table in Supabase over its auto-generated
/// REST API (PostgREST), scoped to the signed-in user by Row Level Security — this service
/// never has to check ownership itself, the database enforces it.
/// </summary>
public sealed class CloudSaveService
{
    private readonly HttpClient _http = new();
    private readonly SupabaseAuthService _auth;

    public CloudSaveService(SupabaseAuthService auth) => _auth = auth;

    public async Task<(List<CloudSaveSummary>? Saves, string? Error)> ListAsync()
    {
        using var req = Request(HttpMethod.Get, "careers?select=id,name,updated_at&order=updated_at.desc");
        var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return (null, await ErrorText(resp));

        var rows = await resp.Content.ReadFromJsonAsync<List<Row>>() ?? new();
        return (rows.Select(r => new CloudSaveSummary(r.Id, r.Name, r.UpdatedAt)).ToList(), null);
    }

    public async Task<string?> SaveAsync(string name, Career career, string? existingId)
    {
        if (_auth.UserId is null) return "Not signed in.";

        var careerJson = CareerStore.Serialize(career);
        var dataNode = JsonNode.Parse(careerJson);
        var body = new JsonObject
        {
            ["user_id"] = _auth.UserId,
            ["name"] = name,
            ["data"] = dataNode,
        };

        HttpRequestMessage req;
        if (existingId is null)
        {
            req = Request(HttpMethod.Post, "careers");
        }
        else
        {
            req = Request(HttpMethod.Patch, $"careers?id=eq.{existingId}");
        }
        req.Content = JsonContent.Create<object>(body);

        var resp = await _http.SendAsync(req);
        return resp.IsSuccessStatusCode ? null : await ErrorText(resp);
    }

    public async Task<(Career? Career, string? Error)> LoadAsync(string id)
    {
        using var req = Request(HttpMethod.Get, $"careers?id=eq.{id}&select=data");
        var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return (null, await ErrorText(resp));

        var rows = await resp.Content.ReadFromJsonAsync<List<DataRow>>();
        if (rows is not { Count: > 0 }) return (null, "Save not found.");

        try
        {
            var career = CareerStore.Deserialize(rows[0].Data.ToJsonString());
            return (career, null);
        }
        catch (Exception ex)
        {
            return (null, $"Could not load that save: {ex.Message}");
        }
    }

    public async Task<string?> DeleteAsync(string id)
    {
        using var req = Request(HttpMethod.Delete, $"careers?id=eq.{id}");
        var resp = await _http.SendAsync(req);
        return resp.IsSuccessStatusCode ? null : await ErrorText(resp);
    }

    private HttpRequestMessage Request(HttpMethod method, string path)
    {
        var req = new HttpRequestMessage(method, $"{SupabaseConfig.Url}/rest/v1/{path}");
        req.Headers.Add("apikey", SupabaseConfig.AnonKey);
        req.Headers.Add("Authorization", $"Bearer {_auth.AccessToken}");
        req.Headers.Add("Prefer", "return=minimal");
        return req;
    }

    private static async Task<string> ErrorText(HttpResponseMessage resp)
    {
        try
        {
            var text = await resp.Content.ReadAsStringAsync();
            return string.IsNullOrWhiteSpace(text) ? $"Request failed ({(int)resp.StatusCode})." : text;
        }
        catch { return $"Request failed ({(int)resp.StatusCode})."; }
    }

    private sealed record Row
    {
        [JsonPropertyName("id")] public string Id { get; init; } = "";
        [JsonPropertyName("name")] public string Name { get; init; } = "";
        [JsonPropertyName("updated_at")] public DateTimeOffset UpdatedAt { get; init; }
    }

    private sealed record DataRow
    {
        [JsonPropertyName("data")] public JsonNode Data { get; init; } = JsonValue.Create("")!;
    }
}
