using Microsoft.JSInterop;

namespace RugbyManager.Web.Services;

public enum AppTheme
{
    Purple,
    Mono,
    Light,
}

/// <summary>
/// The user's chosen visual skin — a config setting, persisted to localStorage so it sticks
/// across sessions. Applies a <c>data-theme</c> attribute to the document root, which the CSS
/// variable overrides in app.css key off; every screen already renders entirely from those
/// variables, so no per-page changes are needed to support a new skin.
/// </summary>
public sealed class ThemeService
{
    private const string StorageKey = "rugbymanager.theme";
    private readonly IJSRuntime _js;
    private bool _loaded;

    public AppTheme Theme { get; private set; } = AppTheme.Purple;

    public event Action? Changed;

    public ThemeService(IJSRuntime js) => _js = js;

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        _loaded = true;
        try
        {
            var stored = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (stored is not null && Enum.TryParse<AppTheme>(stored, out var theme))
                Theme = theme;
        }
        catch { /* localStorage unavailable — keep the default */ }

        await ApplyAsync();
        Changed?.Invoke();
    }

    public async Task SetThemeAsync(AppTheme theme)
    {
        Theme = theme;
        Changed?.Invoke();
        await ApplyAsync();
        try { await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, theme.ToString()); }
        catch { /* best-effort persistence */ }
    }

    private async Task ApplyAsync()
    {
        try { await _js.InvokeVoidAsync("rmSetTheme", Theme.ToString().ToLowerInvariant()); }
        catch { /* JS interop unavailable (e.g. pre-render) — CSS default still applies */ }
    }
}
