using System.Text.Json;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

public sealed class FormationHandoffTests
{
    [Fact]
    public void FormationAccessCardValidatesAndProducesAnEngineStart()
    {
        var catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, UnitVocabulary.Asl())!.Catalog!;
        var text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "formation-access-test.scenario-card.json"));
        var read = ScenarioCards.Parse("formation-access-test", text, catalog);
        Assert.True(read.IsValid, string.Join("; ", read.Diagnostics));
        var root = Path.Combine(Path.GetTempPath(), "formation-card-" + Guid.NewGuid().ToString("N"));
        try
        {
            var library = new ScenarioCardLibrary(root);
            Assert.Empty(library.Save(read.Card!, catalog));
            var saved = library.Read(read.Card!.Id, catalog);
            Assert.True(saved!.IsValid);
            var start = ScenarioCards.Start(saved.Card!, library.Sha256(saved.Card!.Id)!, saved.Card.Catalog, null, null);
            Assert.Equal("american", start["firstSide"]!.GetValue<string>());
            Assert.Equal("bd04", start["boards"]![0]!.GetValue<string>());
            Assert.Equal(saved.Card.Id, start["scenario"]!["id"]!.GetValue<string>());
            Assert.Equal(3, saved.Card.Sides.Single(s => s.Side == "american").Groups[0].Units.Single(u => u.Definition == "american-squad").Count);
            Assert.NotNull(saved.Card.VictoryConditions.Outcomes);
            var invalid = JsonSerializer.SerializeToNode(JsonSerializer.Deserialize<JsonElement>(text))!.AsObject();
            invalid["sides"]![0]!["groups"]![0]!["units"]![0]!["definition"] = "unresolved-placeholder";
            Assert.False(ScenarioCards.Parse("invalid", invalid.ToJsonString(), catalog).IsValid);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }
}
