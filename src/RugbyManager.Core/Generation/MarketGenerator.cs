using RugbyManager.Core.Model;
using RugbyManager.Core.Transfers;

namespace RugbyManager.Core.Generation;

/// <summary>
/// Builds a pool of free agents to sign — mostly journeymen with the occasional gem, spread
/// across every position. Deterministic for a given seed.
/// </summary>
public static class MarketGenerator
{
    public static TransferMarket Generate(int count, int seed)
    {
        var rng = new Random(seed);
        var positions = Enum.GetValues<Position>();
        var players = new List<Player>(count);

        for (int i = 0; i < count; i++)
        {
            var pos = positions[rng.Next(positions.Length)];
            // Mostly average, with a ~1-in-6 chance of a genuinely good player.
            int quality = rng.Next(6) == 0 ? rng.Next(74, 84) : rng.Next(56, 72);
            var player = SquadGenerator.CreatePlayer(pos, quality, rng);
            player.Scouted = rng.Next(0, 30); // largely unknown until scouted
            players.Add(player);
        }

        return new TransferMarket(players);
    }
}
