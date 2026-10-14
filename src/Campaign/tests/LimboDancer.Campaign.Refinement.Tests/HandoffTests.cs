using System.Text;
using System.Text.Json;
using LimboDancer.Campaign.PilotHost;
using LimboDancer.Campaign.Refinement;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Vocabulary;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Play;

namespace LimboDancer.Campaign.Refinement.Tests;

public sealed class HandoffTests
{
    private static readonly PilotInputs Inputs = new(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "goodwood.json")));
    [Theory]
    [InlineData("uk-rifle-01", "B/M-6", "Allied", 4, 0)]
    [InlineData("de-rifle-01", "C/Y-4", "German", 4, 0)]
    [InlineData("uk-engineer-01", "B/M-6", "Allied", 4, 0)]
    [InlineData("de-81mm-01", "C/Y-4", "German", 12, 0)]
    public async Task ExistingAslSetupPreservesDeployedIdentitiesAndRoundTrips(string id, string anchor, string side, int ready, int needsData)
    {
        const string seed = "00112233445566778899aabbccddeeff";
        var selection = new FormationSelection(id, anchor);
        var d = new PilotDeployment(Inputs, new PilotGenerator(Inputs).Generate(seed, FormationDeployment.CropFor(Inputs, selection)), seed, selection);
        var context = new DeploymentContext("handoff", "snapshot", side == "Allied" ? [anchor] : [], side == "German" ? [anchor] : [], selection);
        var setup = d.SetComplete(context, d.AutoDeploy(context, d.Create(context)), true);
        var before = JsonSerializer.Serialize(setup);
        var handoff = await PilotHandoffs.Prepare(d, context, setup);
        Assert.Equal(ready, handoff.Units.Count(u => u.Status == "Ready"));
        Assert.Equal(needsData, handoff.Units.Count(u => u.Status == "Needs data"));
        var record = GameEventReader.Read(Encoding.UTF8.GetBytes(handoff.GameRecord));
        Assert.False(record.HasErrors);
        var vocabulary = UnitVocabulary.Asl();
        var history = GameProjector.Project(record.Record!.Events, vocabulary, [UnitCatalogs.Read(UnitCatalogs.ScenarioA1, vocabulary)!.Catalog!],
            new HexFactLocationChains([(d.Package.Board, d.Package.Version, d.Facts)]), LiveGames.Sources);
        Assert.False(history.HasErrors, string.Join("; ", history.Diagnostics.Select(x => x.Message)));
        var instances = history.Current!.Units.ToArray();
        Assert.Equal(ready, instances.Length + history.Current.Equipment.Count);
        foreach (var item in history.Current.Equipment)
        {
            var source = d.Units.Single(u => PilotHandoffs.AslId(u.Id) == item.Id);
            Assert.Equal(source.Definition, item.Definition);
            Assert.Equal(PilotHandoffs.AslId(source.Holding!.Holder), item.Holding!.Holder);
            Assert.Equal(HoldingRole.Manned, item.Holding.Role);
            Assert.Equal(history.Current.Units.Single(u => u.Id == item.Holding.Holder).Position, item.Position);
        }
        foreach (var instance in instances)
        {
            var unit = d.Units.Single(u => PilotHandoffs.AslId(u.Id) == instance.Id);
            Assert.Equal(unit.Definition, instance.Definition);
            Assert.Equal(unit.Side.ToLowerInvariant(), instance.Side);
            Assert.Equal(unit.Id, handoff.Units.Single(r => r.AslId == instance.Id).Id);
            var at = Assert.IsType<MapPosition>(instance.Position).Location;
            Assert.Equal(d.Package.Board, at.Board);
            Assert.Equal(setup.Placements.Single(p => p.UnitId == unit.Id).Hex, at.Hex.ToString());
            Assert.Equal(0, at.Level); // Relative ground location, including elevated source hexes.
        }
        var restored = JsonSerializer.Deserialize<PilotHandoff>(JsonSerializer.Serialize(handoff))!;
        var reload = new PilotDeployment(Inputs, d.Bundle, selection: selection);
        await PilotHandoffs.Validate(reload, context, setup, restored);
        Assert.Equal(before, JsonSerializer.Serialize(setup));
        Assert.Equal(d.Bundle, reload.Bundle);
        await Assert.ThrowsAsync<InvalidDataException>(() => PilotHandoffs.Validate(d, context, setup, restored with { GameRecord = "{}" }));
        await Assert.ThrowsAsync<InvalidDataException>(() => PilotHandoffs.Validate(d, context, setup, restored with { SourceHash = "wrong" }));
        await Assert.ThrowsAsync<InvalidDataException>(() => PilotHandoffs.Validate(d, context, setup, restored with { Version = "future" }));
        await Assert.ThrowsAsync<InvalidDataException>(() => PilotHandoffs.Validate(d, context, setup with { Revision = setup.Revision + 1 }, restored));
        await Assert.ThrowsAsync<InvalidDataException>(() => PilotHandoffs.Prepare(d, context, setup with { Complete = false }));
    }
}
