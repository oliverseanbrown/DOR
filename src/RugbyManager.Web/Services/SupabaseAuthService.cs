using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace RugbyManager.Web.Services;

/// <summary>
/// Email/password auth against Supabase's GoTrue service, talked to directly over HTTP (no
/// SDK dependency). Session tokens persist to localStorage so a signed-in manager stays signed
/// in across browser restarts, the same way the local career save already does.
/// </summary>
public sealed class SupabaseAuthService
{
    private const string SessionKey = "rugbymanager.supabase.session";
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private bool _loaded;

    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public string? UserId { get; private set; }
    public string? Email { get; private set; }
    public bool IsSignedIn => AccessToken is not null;

    public event Action? Changed;

    public SupabaseAuthService(IJSRuntime js)
    {
        _js = js;
        _http = new HttpClient();
    }

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        _loaded = true;
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", SessionKey);
            if (string.IsNullOrEmpty(json)) return;
            var session = System.Text.Json.JsonSerializer.Deserialize<StoredSession>(json);
            if (session is null) return;

            AccessToken = session.AccessToken;
            RefreshToken = session.RefreshToken;
            UserId = session.UserId;
            Email = session.Email;

            // Best-effort refresh so a session from a prior visit is still valid.
            await RefreshAsync();
        }
        catch { /* corrupt/unavailable session — proceed signed out */ }
    }

    public async Task<string?> SignUpAsync(string email, string password)
    {
        var (resp, body) = await PostAuthAsync("signup", new { email, password });
        if (!resp.IsSuccessStatusCode)
            return body?.ErrorDescription ?? body?.Msg ?? "Sign up failed.";

        if (body?.AccessToken is null)
            return "Account created — check your email to confirm it, then sign in.";

        await ApplySessionAsync(body);
        return null;
    }

    public async Task<string?> SignInAsync(string email, string password)
    {
        var (resp, body) = await PostAuthAsync("token?grant_type=password", new { email, password });
        if (!resp.IsSuccessStatusCode || body?.AccessToken is null)
            return body?.ErrorDescription ?? body?.Msg ?? "Sign in failed — check your email and password.";

        await ApplySessionAsync(body);
        return null;
    }

    private async Task<(HttpResponseMessage Response, AuthResponse? Body)> PostAuthAsync(string path, object payload)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{SupabaseConfig.Url}/auth/v1/{path}")
        {
            Content = JsonContent.Create(payload),
        };
        req.Headers.Add("apikey", SupabaseConfig.AnonKey);
        var resp = await _http.SendAsync(req);
        AuthResponse? body = null;
        try { body = await resp.Content.ReadFromJsonAsync<AuthResponse>(); } catch { /* empty/non-JSON body */ }
        return (resp, body);
    }

    public async Task SignOutAsync()
    {
        if (AccessToken is not null)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, $"{SupabaseConfig.Url}/auth/v1/logout");
                req.Headers.Add("apikey", SupabaseConfig.AnonKey);
                req.Headers.Add("Authorization", $"Bearer {AccessToken}");
                await _http.SendAsync(req);
            }
            catch { /* best-effort */ }
        }

        AccessToken = RefreshToken = UserId = Email = null;
        try { await _js.InvokeVoidAsync("localStorage.removeItem", SessionKey); } catch { }
        Changed?.Invoke();
    }

    private async Task RefreshAsync()
    {
        if (RefreshToken is null) return;
        try
        {
            var (resp, body) = await PostAuthAsync("token?grant_type=refresh_token", new { refresh_token = RefreshToken });
            if (resp.IsSuccessStatusCode && body?.AccessToken is not null)
                await ApplySessionAsync(body);
            else
                await SignOutAsync(); // refresh token no longer valid
        }
        catch { /* offline — keep the stale token, calls will fail and prompt re-login */ }
    }

    private async Task ApplySessionAsync(AuthResponse body)
    {
        AccessToken = body.AccessToken;
        RefreshToken = body.RefreshToken;
        UserId = body.User?.Id;
        Email = body.User?.Email;

        var stored = new StoredSession
        {
            AccessToken = AccessToken, RefreshToken = RefreshToken, UserId = UserId, Email = Email,
        };
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", SessionKey,
                System.Text.Json.JsonSerializer.Serialize(stored));
        }
        catch { /* best-effort persistence */ }
        Changed?.Invoke();
    }

    private sealed record StoredSession
    {
        public string? AccessToken { get; init; }
        public string? RefreshToken { get; init; }
        public string? UserId { get; init; }
        public string? Email { get; init; }
    }

    private sealed record AuthResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; init; }
        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; init; }
        [JsonPropertyName("user")] public AuthUser? User { get; init; }
        [JsonPropertyName("msg")] public string? Msg { get; init; }
        [JsonPropertyName("error_description")] public string? ErrorDescription { get; init; }
    }

    private sealed record AuthUser
    {
        [JsonPropertyName("id")] public string? Id { get; init; }
        [JsonPropertyName("email")] public string? Email { get; init; }
    }
}
