using RugbyManager.Core.Model;

namespace RugbyManager.Core.Generation;

/// <summary>Generates a pool of hireable coaches across specialties. Deterministic per seed.</summary>
public static class CoachGenerator
{
    private static readonly string[] First =
        { "Warren", "Eddie", "Rassie", "Joe", "Steve", "Graham", "Ian", "Wayne", "Michael", "Andy", "Scott", "Ronan" };
    private static readonly string[] Last =
        { "Gatland", "Jones", "Erasmus", "Schmidt", "Hansen", "Henry", "Foster", "Pivac", "Cheika", "Farrell", "Robertson", "O'Gara" };

    public static List<Coach> Generate(int count, int seed)
    {
        var rng = new Random(seed);
        var specialties = Enum.GetValues<CoachSpecialty>();
        var coaches = new List<Coach>(count);

        for (int i = 0; i < count; i++)
        {
            int ability = rng.Next(45, 90);
            coaches.Add(new Coach
            {
                Name = $"{First[rng.Next(First.Length)]} {Last[rng.Next(Last.Length)]}",
                Specialty = specialties[rng.Next(specialties.Length)],
                Ability = ability,
                // Better coaches cost more per week.
                Wage = ability * 30 + 500,
            });
        }

        return coaches;
    }
}
