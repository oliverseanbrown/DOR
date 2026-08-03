using RugbyManager.Core.Injuries;
using RugbyManager.Core.Match;
using RugbyManager.Core.Model;
using RugbyManager.Core.Util;

namespace RugbyManager.Core.Competition;

/// <summary>
/// A season in progress: the fixture list plus the machinery to play it out, round by round.
/// Each match's seed is derived from the season seed and the fixture index, so the whole
/// season is deterministic and reproducible.
/// </summary>
public sealed class Season
{
    public League League { get; }
    public int Seed { get; }
    public IReadOnlyList<Fixture> Fixtures { get; }
    public int Rounds { get; }

    private int _nextRound;

    public Season(League league, int seed)
    {
        League = league;
        Seed = seed;
        Fixtures = FixtureGenerator.DoubleRoundRobin(league.Teams);
        Rounds = Fixtures.Count == 0 ? 0 : Fixtures.Max(f => f.Round) + 1;
    }

    /// <summary>0-based index of the next round to be played (== Rounds when finished).</summary>
    public int NextRound => _nextRound;
    public bool IsComplete => _nextRound >= Rounds;

    public IReadOnlyList<Fixture> FixturesInRound(int round)
        => Fixtures.Where(f => f.Round == round).ToList();

    /// <summary>Play every unplayed fixture in the next round and return them.</summary>
    public IReadOnlyList<Fixture> PlayNextRound()
    {
        if (IsComplete) return Array.Empty<Fixture>();
        BeginRound();
        var played = PlayRoundExcept(null);
        CompleteRound();
        return played;
    }

    /// <summary>A new week: everyone's injuries heal by one week. Call once before playing
    /// any fixtures in a round — separated out so the Match Centre can run its own fixture
    /// interactively while the rest of the round is simulated instantly around it.</summary>
    public void BeginRound()
    {
        foreach (var team in League.Teams) InjuryService.HealWeek(team);
    }

    /// <summary>
    /// Plays every unplayed fixture in the current round except <paramref name="exclude"/> (if
    /// given), auto-selecting each side's best XV. Used to advance the rest of the league
    /// instantly while one specific fixture — the manager's own — is played interactively.
    /// </summary>
    public IReadOnlyList<Fixture> PlayRoundExcept(Fixture? exclude)
    {
        var roundFixtures = FixturesInRound(_nextRound);
        foreach (var fx in roundFixtures)
        {
            if (fx.IsPlayed || ReferenceEquals(fx, exclude)) continue;
            fx.Home.SelectBestXV();
            fx.Away.SelectBestXV();
            var result = new MatchEngine(fx.Home, fx.Away, MatchSeed(fx)).Simulate();
            RecordFixtureResult(fx, result);
        }
        return roundFixtures;
    }

    /// <summary>Records a fixture's outcome (from any source — instant sim or a live, stepped
    /// Match Centre engine) and rolls the deterministic post-match injuries for it.</summary>
    public void RecordFixtureResult(Fixture fx, MatchResult result)
    {
        fx.Result = result;
        var injuryDice = new Dice(unchecked(MatchSeed(fx) * 7 + 13));
        InjuryService.RollMatchInjuries(fx.Home, injuryDice);
        InjuryService.RollMatchInjuries(fx.Away, injuryDice);
    }

    /// <summary>Advances to the next round once every fixture in the current one is settled.</summary>
    public void CompleteRound() => _nextRound++;

    /// <summary>The fixture involving <paramref name="team"/> in the given round, if any
    /// (byes are possible with an odd number of clubs).</summary>
    public Fixture? FindFixtureFor(Team team, int round)
        => FixturesInRound(round).FirstOrDefault(f => ReferenceEquals(f.Home, team) || ReferenceEquals(f.Away, team));

    /// <summary>The deterministic seed a fixture's match would use — exposed so a live Match
    /// Centre engine can be constructed with the exact same seed the instant-sim path would use.</summary>
    public int MatchSeedFor(Fixture fx) => MatchSeed(fx);

    /// <summary>Play the season to completion.</summary>
    public void PlayAll()
    {
        while (!IsComplete) PlayNextRound();
    }

    /// <summary>Current standings from all fixtures played so far.</summary>
    public LeagueTable BuildTable() => LeagueTable.Build(League, Fixtures);

    /// <summary>
    /// Restore how far the season has progressed (used when loading a save). Fixture results
    /// themselves are set directly on the fixtures; the schedule is regenerated deterministically.
    /// </summary>
    public void RestoreProgress(int nextRound) => _nextRound = Math.Clamp(nextRound, 0, Rounds);

    /// <summary>
    /// Deterministic per-match seed. Deliberately NOT <see cref="HashCode.Combine"/>, whose
    /// result is randomised per process — this stable mix guarantees the same season seed
    /// reproduces the exact same season on every run.
    /// </summary>
    private int MatchSeed(Fixture fx)
    {
        unchecked
        {
            int h = 17;
            h = h * 31 + Seed;
            h = h * 31 + fx.Index;
            return h;
        }
    }
}
