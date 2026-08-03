using Microsoft.JSInterop;

namespace RugbyManager.Web.Services;

/// <summary>
/// The user's preferred commentary pacing — a config setting, persisted to localStorage so it
/// sticks across matches and sessions. Shared by every match-playback screen (quick match and
/// career matchday), so it only needs to be set once.
/// </summary>
public sealed class PlaybackSettingsService
{
    private const string StorageKey = "rugbymanager.pace";
    private readonly IJSRuntime _js;
    private bool _loaded;

    public CommentaryPace Pace { get; private set; } = CommentaryPace.Normal;

    public event Action? Changed;

    public PlaybackSettingsService(IJSRuntime js) => _js = js;

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        _loaded = true;
        try
        {
            var stored = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (stored is not null && Enum.TryParse<CommentaryPace>(stored, out var pace))
            {
                Pace = pace;
                Changed?.Invoke();
            }
        }
        catch { /* localStorage unavailable — keep the default */ }
    }

    public async Task SetPaceAsync(CommentaryPace pace)
    {
        Pace = pace;
        Changed?.Invoke();
        try { await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, pace.ToString()); }
        catch { /* best-effort persistence */ }
    }
}
