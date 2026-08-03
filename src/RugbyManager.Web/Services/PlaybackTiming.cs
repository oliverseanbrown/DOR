using RugbyManager.Core.Match;

namespace RugbyManager.Web.Services;

/// <summary>How long to linger on an event before revealing the next one, by pace setting.</summary>
public static class PlaybackTiming
{
    public static int DelayMs(MatchEvent e, CommentaryPace pace)
    {
        bool highlight = e.Drama == Drama.Highlight;
        return (pace, highlight) switch
        {
            (CommentaryPace.Fast, false) => 90,
            (CommentaryPace.Fast, true) => 450,
            (CommentaryPace.Normal, false) => 200,
            (CommentaryPace.Normal, true) => 1000,
            (CommentaryPace.Cinematic, false) => 260,
            (CommentaryPace.Cinematic, true) => 1900,
            _ => 200,
        };
    }
}
