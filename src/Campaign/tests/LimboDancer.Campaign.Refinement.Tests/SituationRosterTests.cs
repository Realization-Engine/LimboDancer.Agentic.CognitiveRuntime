using System.Text.Json;
using LimboDancer.Campaign.Refinement;

namespace LimboDancer.Campaign.Refinement.Tests;

public sealed class SituationRosterTests
{
    private static PilotInputs Inputs() => new(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "goodwood.json")));

    [Fact]
    public void EverySourceInstanceSurvivesRosterReloadExactlyOnce()
    {
        var inputs = Inputs();
        var roster = SituationRoster.Create(inputs);
        var restored = JsonSerializer.Deserialize<SituationRosterSnapshot>(JsonSerializer.Serialize(roster))!;
        SituationRoster.Validate(inputs, restored);
        Assert.Equal(114, restored.Ground.Length);
        Assert.Equal(624, restored.Ground.Sum(f => f.Units.Length));
        Assert.Equal(8, restored.AirSupport.Aircraft.Length);
        Assert.Equal(5, restored.AirSupport.MaxConcurrentAircraft);
        Assert.All(restored.AirSupport.Aircraft, a => Assert.Equal(1, a.AvailableFromTurn));
        Assert.Equal(inputs.FormationTypes.Keys.Order(StringComparer.Ordinal), restored.Ground.Select(f => f.Id)
            .Concat(restored.AirSupport.Aircraft.Select(a => a.Id)).Order(StringComparer.Ordinal));
        Assert.DoesNotContain(restored.Ground.SelectMany(f => f.Units), u => u.ParentId.StartsWith("uk-typhoon", StringComparison.Ordinal));
        Assert.All(restored.Ground, f => Assert.All(f.Units.Where(u => u.Holding is not null),
            u => Assert.Contains(f.Units, crew => crew.Id == u.Holding!.Holder)));
    }

    [Fact]
    public void MissingAircraftChangedLimitsAndFutureVersionsAreRejected()
    {
        var inputs = Inputs();
        var roster = SituationRoster.Create(inputs);
        Assert.Throws<InvalidDataException>(() => SituationRoster.Validate(inputs, roster with { Version = "future" }));
        Assert.Throws<InvalidDataException>(() => SituationRoster.Validate(inputs, roster with
        { AirSupport = roster.AirSupport with { Aircraft = roster.AirSupport.Aircraft[..^1] } }));
        Assert.Throws<InvalidDataException>(() => SituationRoster.Validate(inputs, roster with
        { AirSupport = roster.AirSupport with { MaxConcurrentAircraft = 8 } }));
        Assert.Throws<InvalidDataException>(() => SituationRoster.Validate(inputs, roster with { Ground = roster.Ground[..^1] }));
    }
}
