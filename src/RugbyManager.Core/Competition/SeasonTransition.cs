using RugbyManager.Core.Generation;
using RugbyManager.Core.Model;

namespace RugbyManager.Core.Competition;

/// <summary>
/// Rolls a completed season into the next: applies promotion/relegation, ages and refreshes the
/// player's squad (retirements, decline, youth intake), regenerates the division at the new tier,
/// and refreshes the transfer and coach markets. Returns a brand-new <see cref="Career"/>.
/// </summary>
public static class SeasonTransition
{
    private const int RetirementAge = 36;
    private const int YouthTarget = 26;      // top the squad up toward this each pre-season
    private const int MaxYouthPerSeason = 2;

    private static readonly Position[] AllPositions = Enum.GetValues<Position>();

    public static (Career Next, SeasonResult Result) Advance(Career career, int teamCount = 10)
    {
        if (!career.Season.IsComplete)
            throw new InvalidOperationException("The season is not finished yet.");

        var club = career.MyClub;
        int position = career.Season.BuildTable().RowFor(club).Position;
        var result = Pyramid.ResultFor(position, career.League.TeamCount);
        int newTier = Pyramid.NextTier(career.Tier, result);

        int seasonNumber = career.SeasonNumber + 1;
        int seed = unchecked(career.Seed + seasonNumber * 1000);
        var rng = new Random(seed);

        var news = new List<string> { ResultHeadline(career, position, result, newTier) };

        PreSeason(club, rng, news);
        YouthIntake(club, newTier, rng, news);
        club.SelectBestXV();

        // New division at the new tier, keeping the player's club at index 0.
        var generated = LeagueGenerator.Generate(Pyramid.Name(newTier), teamCount, seed, baseQuality: Pyramid.BaseQuality(newTier));
        var teams = new List<Team> { club };
        teams.AddRange(generated.Teams.Skip(1));
        var league = new League { Name = Pyramid.Name(newTier), Teams = teams };

        var market = MarketGenerator.Generate(40, seed + 5000);
        var coachMarket = CoachGenerator.Generate(20, seed + 7000);
        var season = new Season(league, seed);

        var next = new Career(season, myTeamIndex: 0, market, coachMarket)
        {
            Training = career.Training,
            SeasonNumber = seasonNumber,
            Tier = newTier,
        };
        next.News.AddRange(news);
        return (next, result);
    }

    private static string ResultHeadline(Career career, int position, SeasonResult result, int newTier)
    {
        string club = career.MyClub.Name;
        string tier = Pyramid.Name(career.Tier);
        return result switch
        {
            SeasonResult.Promoted when career.Tier == Pyramid.TopTier =>
                $"CHAMPIONS! {club} finished {Ordinal(position)} and are champions of the {tier}!",
            SeasonResult.Promoted =>
                $"PROMOTED! {club} finished {Ordinal(position)} in the {tier} and go up to the {Pyramid.Name(newTier)}.",
            SeasonResult.Relegated =>
                $"RELEGATED. {club} finished {Ordinal(position)} in the {tier} and drop to the {Pyramid.Name(newTier)}.",
            _ =>
                $"{club} finished {Ordinal(position)} in the {tier} and stay up.",
        };
    }

    private static void PreSeason(Team club, Random rng, List<string> news)
    {
        foreach (var p in club.Squad) p.Age++;

        foreach (var retiree in club.Squad.Where(p => p.Age >= RetirementAge).ToList())
        {
            club.RemoveFromSquad(retiree);
            news.Add($"{retiree.FullName} ({retiree.NaturalPosition.ShortName()}) retires, aged {retiree.Age}.");
        }

        foreach (var p in club.Squad)
        {
            if (p.Age >= 31) Decline(p, rng);
            p.Condition = 100;              // fresh for pre-season
            p.InjuredWeeksRemaining = 0;    // injuries clear over the summer
        }
    }

    /// <summary>Older players lose a little pace and power each year.</summary>
    private static void Decline(Player p, Random rng)
    {
        double chance = (p.Age - 30) * 0.10;
        var a = p.Attributes;
        if (rng.NextDouble() < chance) a.Pace = Math.Max(1, a.Pace - 1);
        if (rng.NextDouble() < chance) a.Acceleration = Math.Max(1, a.Acceleration - 1);
        if (rng.NextDouble() < chance) a.Stamina = Math.Max(1, a.Stamina - 1);
        if (rng.NextDouble() < chance) a.Strength = Math.Max(1, a.Strength - 1);
        if (rng.NextDouble() < chance) a.Agility = Math.Max(1, a.Agility - 1);
    }

    private static void YouthIntake(Team club, int tier, Random rng, List<string> news)
    {
        int slots = Math.Min(MaxYouthPerSeason, Math.Max(0, YouthTarget - club.Squad.Count));
        for (int i = 0; i < slots; i++)
        {
            var pos = AllPositions[rng.Next(AllPositions.Length)];
            int quality = Math.Max(30, Pyramid.BaseQuality(tier) - 14);        // raw but raw-boned
            int potential = Math.Clamp(Pyramid.BaseQuality(tier) + rng.Next(2, 18), 40, 95);
            var youth = SquadGenerator.CreatePlayer(pos, quality, rng, ageOverride: rng.Next(18, 20), potentialOverride: potential);
            club.AddToSquad(youth);
            news.Add($"Academy graduate {youth.FullName} ({pos.ShortName()}) joins, potential {potential}.");
        }
    }

    private static string Ordinal(int n)
    {
        if (n is >= 11 and <= 13) return $"{n}th";
        return (n % 10) switch { 1 => $"{n}st", 2 => $"{n}nd", 3 => $"{n}rd", _ => $"{n}th" };
    }
}
