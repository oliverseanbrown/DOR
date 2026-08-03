using Microsoft.AspNetCore.Components;
using RugbyManager.Web.Services;

namespace RugbyManager.Web.Pages.Career;

/// <summary>
/// Base class for every career screen: guards against a missing career (redirects to the
/// New Career page) and re-renders automatically whenever <see cref="GameService"/> changes,
/// so pages don't each need their own subscribe/unsubscribe boilerplate.
/// </summary>
public abstract class CareerComponentBase : ComponentBase, IDisposable
{
    [Inject] protected GameService Game { get; set; } = null!;
    [Inject] protected NavigationManager Nav { get; set; } = null!;

    protected override void OnInitialized()
    {
        if (Game.Career is null)
        {
            Nav.NavigateTo("/career/new");
            return;
        }
        Game.Changed += OnChanged;
    }

    private void OnChanged() => InvokeAsync(StateHasChanged);

    public void Dispose() => Game.Changed -= OnChanged;
}
