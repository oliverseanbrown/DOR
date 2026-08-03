using RugbyManager.Core.Model;

namespace RugbyManager.Core.Transfers;

/// <summary>Estimates a player's transfer fee (in pounds) from ability and age.</summary>
public static class TransferValue
{
    public static int Estimate(Player p)
    {
        double ovr = PlayerRating.Overall(p);

        // Ability drives value steeply: a great player is worth far more than a good one.
        double baseValue = Math.Pow(ovr / 40.0, 3) * 8000; // ovr 60 ~ £27k, 75 ~ £53k, 85 ~ £77k

        // Age curve: a small premium for youth (potential), decline past the late 20s.
        double ageFactor = p.Age switch
        {
            <= 23 => 1.15,
            <= 27 => 1.0,
            <= 30 => 0.8,
            <= 32 => 0.55,
            _ => 0.35,
        };

        double value = baseValue * ageFactor;
        // Round to the nearest £500 for tidy numbers.
        return (int)(Math.Round(value / 500.0) * 500);
    }
}
