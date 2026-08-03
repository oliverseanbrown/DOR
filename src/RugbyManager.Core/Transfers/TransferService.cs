using RugbyManager.Core.Model;

namespace RugbyManager.Core.Transfers;

/// <summary>Outcome of a transfer.</summary>
public sealed record SignResult(bool Success, string Message)
{
    public Player? SignedIn { get; init; }
    public Player? MovedOut { get; init; }
    public int Fee { get; init; }
    public int SaleReceived { get; init; }
}

/// <summary>
/// Executes transfers against a club's squad. Signing adds a player (and the best XV is
/// re-picked, so an upgrade walks straight into the team); selling removes one. The squad is
/// bounded between 15 and 30, and the club's budget gates every deal.
/// </summary>
public static class TransferService
{
    /// <summary>Fraction of a player's value recouped when sold.</summary>
    public const double SellBackFactor = 0.6;
    public const int MaxSquad = 30;
    public const int MinSquad = 15;

    public static SignResult Sign(Team club, TransferMarket market, Player target)
    {
        if (!market.Available.Contains(target))
            return new SignResult(false, "That player is no longer available.");
        if (club.Squad.Count >= MaxSquad)
            return new SignResult(false, $"Your squad is full (max {MaxSquad}). Sell someone first.");

        int fee = TransferValue.Estimate(target);
        if (club.Money < fee)
            return new SignResult(false, $"Not enough funds. Fee is {Money(fee)}, budget is {Money(club.Money)}.");

        club.Money -= fee;
        target.Scouted = 100; // your own players are fully known
        club.AddToSquad(target);
        market.Remove(target);
        club.SelectBestXV();

        return new SignResult(true, $"Signed {target.FullName} for {Money(fee)}.")
        {
            SignedIn = target,
            Fee = fee,
        };
    }

    public static SignResult Sell(Team club, TransferMarket market, Player player)
    {
        if (!club.Squad.Contains(player))
            return new SignResult(false, "That player is not in your squad.");
        if (club.Squad.Count <= MinSquad)
            return new SignResult(false, $"Can't sell — squad is at the minimum of {MinSquad}.");

        int received = (int)(TransferValue.Estimate(player) * SellBackFactor);
        club.RemoveFromSquad(player);
        club.Money += received;
        market.Add(player);
        club.SelectBestXV();

        return new SignResult(true, $"Sold {player.FullName} for {Money(received)}.")
        {
            MovedOut = player,
            SaleReceived = received,
        };
    }

    public static string Money(int pounds) => "£" + pounds.ToString("N0");
}
