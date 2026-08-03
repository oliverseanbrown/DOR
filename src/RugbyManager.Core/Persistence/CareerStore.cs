using System.Text.Json;
using System.Text.Json.Serialization;
using RugbyManager.Core.Competition;
using RugbyManager.Core.Facilities;
using RugbyManager.Core.Match;
using RugbyManager.Core.Model;
using RugbyManager.Core.Transfers;

namespace RugbyManager.Core.Persistence;

/// <summary>Saves and loads a <see cref="Career"/> to/from a JSON file.</summary>
public static class CareerStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static void Save(Career career, string path)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, Serialize(career));
    }

    public static Career Load(string path) => Deserialize(File.ReadAllText(path));

    /// <summary>Serialize a career to a JSON string (used by web localStorage saves).</summary>
    public static string Serialize(Career career) => JsonSerializer.Serialize(ToData(career), Options);

    /// <summary>Rebuild a career from a JSON string.</summary>
    public static Career Deserialize(string json)
    {
        var data = JsonSerializer.Deserialize<SaveData>(json, Options)
                   ?? throw new InvalidDataException("Save data was empty or invalid.");
        return FromData(data);
    }

    // --- career -> data ---

    private static SaveData ToData(Career career)
    {
        var teams = new List<TeamData>();
        foreach (var team in career.League.Teams)
        {
            var players = team.Squad.Select(ToPlayerData).ToList();
            teams.Add(new TeamData
            {
                Name = team.Name,
                ShortName = team.ShortName,
                HomeGround = team.HomeGround,
                PrimaryColour = team.PrimaryColour,
                SecondaryColour = team.SecondaryColour,
                CrestImageDataUrl = team.CrestImageDataUrl,
                Money = team.Money,
                Tactics = team.Tactics,
                Players = players,
                Coaches = new List<Coach>(team.Coaches),
                Playbook = new List<SetPlay>(team.Playbook),
                PlayFamiliarity = new Dictionary<string, int>(team.PlayFamiliarity),
                PitchLevel = team.PitchLevel,
                StadiumLevel = team.StadiumLevel,
                TrainingGroundLevel = team.TrainingGroundLevel,
                ClubhouseLevel = team.ClubhouseLevel,
                FacilityProjects = team.FacilityProjects.Select(p => new FacilityProjectData
                {
                    Area = p.Area,
                    TargetLevel = p.TargetLevel,
                    TotalWeeks = p.TotalWeeks,
                    WeeksRemaining = p.WeeksRemaining,
                }).ToList(),
            });
        }

        var market = career.Market.Available.Select(ToPlayerData).ToList();

        var results = new List<ResultData>();
        foreach (var fx in career.Season.Fixtures)
        {
            if (fx.Result is not { } r) continue;
            results.Add(new ResultData
            {
                FixtureIndex = fx.Index,
                HomeScore = r.HomeScore,
                AwayScore = r.AwayScore,
                HomeStats = r.HomeStats,
                AwayStats = r.AwayStats,
            });
        }

        return new SaveData
        {
            LeagueName = career.League.Name,
            Seed = career.Seed,
            MyTeamIndex = career.MyTeamIndex,
            NextRound = career.Season.NextRound,
            SeasonNumber = career.SeasonNumber,
            Tier = career.Tier,
            News = new List<string>(career.News),
            Training = career.Training,
            ManagerName = career.ManagerName,
            Teams = teams,
            Results = results,
            Market = market,
            CoachMarket = new List<Coach>(career.CoachMarket),
        };
    }

    private static PlayerData ToPlayerData(Player p) => new()
    {
        Position = p.NaturalPosition,
        FirstName = p.FirstName,
        LastName = p.LastName,
        Age = p.Age,
        Nationality = p.Nationality,
        Condition = p.Condition,
        InjuredWeeksRemaining = p.InjuredWeeksRemaining,
        Scouted = p.Scouted,
        TrainingGains = new Dictionary<string, int>(p.TrainingGains),
        CareerStats = new Dictionary<int, CareerPlayerStats>(p.CareerStats),
        Attributes = p.Attributes,
    };

    private static Player PlayerFrom(PlayerData pd) => new()
    {
        FirstName = pd.FirstName,
        LastName = pd.LastName,
        Age = pd.Age,
        NaturalPosition = pd.Position,
        Nationality = pd.Nationality,
        Condition = pd.Condition,
        InjuredWeeksRemaining = pd.InjuredWeeksRemaining,
        Scouted = pd.Scouted,
        TrainingGains = new Dictionary<string, int>(pd.TrainingGains),
        CareerStats = new Dictionary<int, CareerPlayerStats>(pd.CareerStats),
        Attributes = pd.Attributes,
    };

    // --- data -> career ---

    private static Career FromData(SaveData data)
    {
        var teams = new List<Team>();
        foreach (var td in data.Teams)
        {
            var team = new Team
            {
                Name = td.Name,
                ShortName = td.ShortName,
                HomeGround = td.HomeGround,
                PrimaryColour = td.PrimaryColour,
                SecondaryColour = td.SecondaryColour,
                CrestImageDataUrl = td.CrestImageDataUrl,
                Money = td.Money,
                Squad = td.Players.Select(PlayerFrom).ToList(),
                Tactics = td.Tactics,
                Coaches = new List<Coach>(td.Coaches),
                Playbook = new List<SetPlay>(td.Playbook),
                PlayFamiliarity = new Dictionary<string, int>(td.PlayFamiliarity),
                PitchLevel = td.PitchLevel,
                StadiumLevel = td.StadiumLevel,
                TrainingGroundLevel = td.TrainingGroundLevel,
                ClubhouseLevel = td.ClubhouseLevel,
                FacilityProjects = td.FacilityProjects.Select(p => new FacilityProject
                {
                    Area = p.Area,
                    TargetLevel = p.TargetLevel,
                    TotalWeeks = p.TotalWeeks,
                    WeeksRemaining = p.WeeksRemaining,
                }).ToList(),
            };
            team.SelectBestXV();
            teams.Add(team);
        }

        var league = new League { Name = data.LeagueName, Teams = teams };

        // Regenerate the identical schedule, then re-apply the saved results.
        var season = new Season(league, data.Seed) { SeasonNumber = data.SeasonNumber };
        var byIndex = season.Fixtures.ToDictionary(f => f.Index);
        foreach (var rd in data.Results)
        {
            if (!byIndex.TryGetValue(rd.FixtureIndex, out var fx)) continue;
            fx.Result = new MatchResult
            {
                HomeName = fx.Home.Name,
                AwayName = fx.Away.Name,
                HomeShort = fx.Home.ShortName,
                AwayShort = fx.Away.ShortName,
                HomeScore = rd.HomeScore,
                AwayScore = rd.AwayScore,
                HomeStats = rd.HomeStats,
                AwayStats = rd.AwayStats,
                Events = Array.Empty<MatchEvent>(),
                Seed = 0,
            };
        }
        season.RestoreProgress(data.NextRound);

        var market = new TransferMarket(data.Market.Select(PlayerFrom));
        var career = new Career(season, data.MyTeamIndex, market, new List<Coach>(data.CoachMarket))
        {
            Training = data.Training,
            SeasonNumber = data.SeasonNumber,
            Tier = data.Tier,
            ManagerName = data.ManagerName,
        };
        career.News.AddRange(data.News);
        return career;
    }
}
