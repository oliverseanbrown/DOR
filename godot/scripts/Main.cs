using Godot;
using RugbyManager.Core.Generation;
using RugbyManager.Core.Match;
using RugbyManager.Core.Model;

namespace RugbyManager.GodotApp;

/// <summary>
/// Phase 3 scaffold. It simulates a match with <c>RugbyManager.Core</c> and plays the event
/// feed back on a timer with a live scoreboard — the smallest thing that proves the
/// architecture: the simulation decides everything, the visual layer only renders it.
///
/// Graphics here are placeholder labels. The low-poly, Jonah-Lomu-style match view and the
/// isometric club builder slot in as replacements for THIS rendering layer, reading the same
/// <see cref="MatchResult"/> / <see cref="MatchEvent"/> data. No simulation logic lives here.
/// </summary>
public partial class Main : Control
{
    private Label _scoreboard = null!;
    private VBoxContainer _feed = null!;
    private ScrollContainer _scroll = null!;
    private MatchResult _result = null!;
    private int _eventIndex;

    public override void _Ready()
    {
        BuildUi();
        _result = SimulateMatch();
        _scoreboard.Text = $"{_result.HomeShort} 0 - 0 {_result.AwayShort}";
        StartPlayback();
    }

    private void BuildUi()
    {
        var root = new VBoxContainer();
        root.SetAnchorsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 8);
        AddChild(root);

        var title = new Label { Text = "RugbyManager — match playback (Phase 3 scaffold)" };
        title.AddThemeFontSizeOverride("font_size", 22);
        root.AddChild(title);

        _scoreboard = new Label();
        _scoreboard.AddThemeFontSizeOverride("font_size", 40);
        root.AddChild(_scoreboard);

        _scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        root.AddChild(_scroll);

        _feed = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _scroll.AddChild(_feed);
    }

    private static MatchResult SimulateMatch()
    {
        var home = SquadGenerator.Generate("Ashcombe RFC", "ASH", 68, new Tactics
        {
            PlayStyle = PlayStyle.ForwardsOriented,
            BreakdownFocus = BreakdownFocus.Aggressive,
        }, seed: 101);

        var away = SquadGenerator.Generate("Riverside Rangers", "RIV", 66, new Tactics
        {
            PlayStyle = PlayStyle.Expansive,
            DefensiveLine = DefensiveLine.Rush,
        }, seed: 202);

        return new MatchEngine(home, away, seed: 42).Simulate();
    }

    private void StartPlayback()
    {
        var timer = new Timer { WaitTime = 0.4, Autostart = true };
        AddChild(timer);
        timer.Timeout += RevealNextEvent;
    }

    private void RevealNextEvent()
    {
        if (_eventIndex >= _result.Events.Count) return;

        var e = _result.Events[_eventIndex++];
        _scoreboard.Text = $"{_result.HomeShort} {e.HomeScore} - {e.AwayScore} {_result.AwayShort}";

        var line = new Label { Text = $"{e.Minute,2}'  {e.Text}" };
        if (e.Type == MatchEventType.Try) line.Modulate = Colors.LightGreen;
        else if (e.Type is MatchEventType.PenaltyGoal or MatchEventType.Conversion or MatchEventType.DropGoal)
            line.Modulate = Colors.LightSkyBlue;
        _feed.AddChild(line);

        // Keep the newest line in view.
        CallDeferred(nameof(ScrollToBottom));
    }

    private void ScrollToBottom() => _scroll.ScrollVertical = (int)_scroll.GetVScrollBar().MaxValue;
}
