using static RugbyManager.Core.Model.Position;

namespace RugbyManager.Core.Model;

/// <summary>
/// A club with a full squad, a selected starting XV, tactics and finances. The squad is the
/// source of truth; <see cref="StartingXV"/> is (re)derived from the fittest available players
/// via <see cref="SelectBestXV"/> before each match and after any squad change.
/// </summary>
public sealed class Team
{
    private static readonly Position[] AllPositions = Enum.GetValues<Position>();

    public required string Name { get; init; }
    public required string ShortName { get; init; }

    /// <summary>Every contracted player. Includes reserves beyond the starting XV.</summary>
    public required List<Player> Squad { get; init; }

    /// <summary>The chosen starting XV, keyed by position. Derived from <see cref="Squad"/>.</summary>
    public Dictionary<Position, Player> StartingXV { get; private set; } = new();

    /// <summary>Settable so the manager can change the game plan between matches.</summary>
    public Tactics Tactics { get; set; } = new();

    /// <summary>Transfer budget, in pounds.</summary>
    public int Money { get; set; }

    /// <summary>Coaching staff. Boost matchday ratings and training in their specialties.</summary>
    public List<Coach> Coaches { get; init; } = new();

    /// <summary>Rehearsed set plays the team will attempt in matches.</summary>
    public List<SetPlay> Playbook { get; init; } = new();

    /// <summary>
    /// How well-drilled each play is, by name, 0-100. Grows through matching training and
    /// match reps (more on success than failure) — this is "have they trained it, have they
    /// done it before and had it come off". A newly learned play starts modest, not zero: the
    /// team has practiced it enough to add it to the book, just not proven it in anger yet.
    /// </summary>
    public Dictionary<string, int> PlayFamiliarity { get; init; } = new();

    private const int DefaultFamiliarity = 15;

    public int GetFamiliarity(string playName)
        => PlayFamiliarity.TryGetValue(playName, out var v) ? v : DefaultFamiliarity;

    public void BumpFamiliarity(string playName, int delta)
        => PlayFamiliarity[playName] = Math.Clamp(GetFamiliarity(playName) + delta, 0, 100);

    /// <summary>Best ability among coaches with the given specialty (0 if none).</summary>
    public int CoachRating(CoachSpecialty specialty)
        => Coaches.Where(c => c.Specialty == specialty).Select(c => c.Ability).DefaultIfEmpty(0).Max();

    public Player At(Position p) => StartingXV[p];

    /// <summary>All 15 starters.</summary>
    public IEnumerable<Player> Players => StartingXV.Values;

    /// <summary>The nominated goal kicker among the starters — the best available boot.</summary>
    public Player GoalKicker => Players.MaxBy(p => p.Attributes.GoalKicking)!;

    /// <summary>
    /// True once the manager has hand-picked a starting XV for the next fixture on the Team
    /// Sheet screen. When set, <see cref="SelectBestXV"/> is skipped for that match — the chosen
    /// lineup stands (patched only for anyone who's since become unfit via
    /// <see cref="ValidateLineupFitness"/>). Consumed (reset to false) once that match is played,
    /// so future rounds default back to auto-selection unless the manager sets a lineup again.
    /// </summary>
    public bool HasManualLineup { get; set; }

    public void AddToSquad(Player p) => Squad.Add(p);
    public bool RemoveFromSquad(Player p) => Squad.Remove(p);

    /// <summary>Manually assign one position for the next matchday squad (Team Sheet screen).</summary>
    public void SetStarter(Position pos, Player player) => StartingXV[pos] = player;

    /// <summary>
    /// Swap out anyone in a manually-chosen lineup who's since become unfit, promoting the best
    /// available fit replacement — the same fallback logic <see cref="SelectBestXV"/> uses, but
    /// only touching the gaps rather than rebuilding the whole XV from scratch.
    /// </summary>
    public void ValidateLineupFitness()
    {
        var used = new HashSet<Player>(StartingXV.Values.Where(p => p.IsFit));
        foreach (var pos in AllPositions)
        {
            if (StartingXV.TryGetValue(pos, out var current) && current.IsFit) continue;
            var fit = Squad.Where(x => x.IsFit && !used.Contains(x)).ToList();
            var best = fit.Where(x => x.NaturalPosition == pos).MaxBy(PlayerRating.Overall)
                       ?? fit.FirstOrDefault();
            if (best is not null) { StartingXV[pos] = best; used.Add(best); }
        }
    }

    /// <summary>
    /// Pick the strongest legal XV from the fit members of the squad: the best natural fit for
    /// each position, then out-of-position cover for any gaps, and — only if injuries leave too
    /// few fit players — emergency call-ups. Always fills 15 distinct slots.
    /// </summary>
    public void SelectBestXV()
    {
        var chosen = new Dictionary<Position, Player>();
        var used = new HashSet<Player>();
        var fit = Squad.Where(p => p.IsFit).ToList();

        // 1) Best natural fit per position.
        foreach (var pos in AllPositions)
        {
            var best = fit.Where(p => p.NaturalPosition == pos && !used.Contains(p))
                          .MaxBy(PlayerRating.Overall);
            if (best is not null) { chosen[pos] = best; used.Add(best); }
        }

        // 2) Fill remaining slots with the best available fit player (out of position),
        //    falling back to injured players only in an emergency so 15 are always named.
        foreach (var pos in AllPositions)
        {
            if (chosen.ContainsKey(pos)) continue;
            var best = fit.FirstOrDefault(p => !used.Contains(p))
                       ?? Squad.FirstOrDefault(p => !used.Contains(p));
            if (best is not null) { chosen[pos] = best; used.Add(best); }
        }

        StartingXV = chosen;
    }
}
