using RugbyManager.Core.Model;

namespace RugbyManager.Core.Transfers;

/// <summary>The pool of players available to sign. Mutated as players are signed and released.</summary>
public sealed class TransferMarket
{
    private readonly List<Player> _available;

    public TransferMarket(IEnumerable<Player> players) => _available = players.ToList();

    public IReadOnlyList<Player> Available => _available;

    /// <summary>Players available at a given position, best first.</summary>
    public IEnumerable<Player> AtPosition(Position pos)
        => _available.Where(p => p.NaturalPosition == pos)
                     .OrderByDescending(PlayerRating.Overall);

    public void Remove(Player p) => _available.Remove(p);
    public void Add(Player p) => _available.Add(p);
}
