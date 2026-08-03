namespace RugbyManager.Core.Util;

/// <summary>
/// A seeded random source. Every roll in a match goes through one Dice instance,
/// so a match is fully deterministic and replayable from its seed — invaluable for
/// balancing and debugging.
/// </summary>
public sealed class Dice
{
    private readonly Random _rng;

    public int Seed { get; }

    public Dice(int seed)
    {
        Seed = seed;
        _rng = new Random(seed);
    }

    /// <summary>Uniform double in [0, 1).</summary>
    public double NextDouble() => _rng.NextDouble();

    /// <summary>Integer in [minInclusive, maxExclusive).</summary>
    public int NextInt(int minInclusive, int maxExclusive) => _rng.Next(minInclusive, maxExclusive);

    /// <summary>True with probability <paramref name="p"/> (clamped to [0,1]).</summary>
    public bool Chance(double p) => _rng.NextDouble() < Math.Clamp(p, 0, 1);

    /// <summary>Approximately normal value via central limit; result is clamped to +/-3 sigma.</summary>
    public double Gaussian(double mean, double stdDev)
    {
        // Sum of 12 uniforms - 6 approximates a standard normal.
        double sum = 0;
        for (int i = 0; i < 12; i++) sum += _rng.NextDouble();
        double z = Math.Clamp(sum - 6.0, -3.0, 3.0);
        return mean + z * stdDev;
    }

    /// <summary>
    /// Logistic win probability for rating A vs rating B (Elo-style). A gap of
    /// <paramref name="sensitivity"/> points gives the stronger side ~76% odds.
    /// </summary>
    public double ContestProb(double a, double b, double sensitivity = 15.0)
        => 1.0 / (1.0 + Math.Pow(10.0, (b - a) / sensitivity));

    /// <summary>Resolve a contest: true if A wins.</summary>
    public bool Contest(double a, double b, double sensitivity = 15.0)
        => Chance(ContestProb(a, b, sensitivity));

    /// <summary>Pick a uniformly random element.</summary>
    public T Pick<T>(IReadOnlyList<T> items) => items[_rng.Next(items.Count)];
}
