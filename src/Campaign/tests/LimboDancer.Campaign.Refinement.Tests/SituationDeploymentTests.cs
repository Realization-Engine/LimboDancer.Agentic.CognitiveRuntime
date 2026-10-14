using System.Text.Json;
using LimboDancer.Campaign.Refinement;
using LimboDancer.Campaign.PilotHost;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Campaign.Refinement.Tests;

public sealed class SituationDeploymentTests
{
    [Fact]
    public async Task CompleteGroundRosterAssemblesDeterministicallyAndRetainsPositionsOnReview()
    {
        var inputs = new PilotInputs(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "goodwood.json")));
        const string seed = "00112233445566778899aabbccddeeff";
        var root = Path.Combine(Path.GetTempPath(), "situation-deployment-" + Guid.NewGuid().ToString("N"));
        try
        {
            var path = SituationPublisher.Publish(inputs, seed, root);
            var map = new SituationMap(inputs, path);
            var service = new SituationDeployment(inputs, map);
            var roster = SituationRoster.Create(inputs);
            // A full, dispersed formation deployment, with unique open parent hexes for both sides.
            var open = inputs.Regions.Where(r => r.Kind == "open").OrderBy(r => r.Key, StringComparer.Ordinal).ToArray();
            var formations = roster.Ground.Select((f, i) => new FormationSelection(f.Id, open[i].Key)).ToArray();
            // Woods are legal ASL setup terrain for vehicles and manned guns. Exercise the
            // real transition, including GamePlanner and Studio, instead of only open fields.
            formations[Array.FindIndex(formations, f => f.UnitId == "de-88mm-04")] = new("de-88mm-04", "A/D-4");
            formations[Array.FindIndex(formations, f => f.UnitId == "de-panther-w-01")] = new("de-panther-w-01", "A/F-6");
            var draft = service.Assemble(formations);
            Assert.True(draft.Ready, string.Join("; ", draft.Review.Select(r => r.UnitId + ": " + r.Reason)));
            Assert.Equal(624, draft.Positions.Length);
            Assert.Equal(624, draft.Positions.Select(p => p.UnitId).Distinct().Count());
            var addresses = new SituationAddresses(inputs, seed);
            Assert.All(draft.Positions, p => Assert.Equal(formations.Single(f => f.UnitId ==
                roster.Ground.Single(f => f.Units.Any(u => u.Id == p.UnitId)).Id).Anchor, addresses.At(p.Column, p.Row).Parent));
            // One ASL board removes internal loading margins without changing accepted center terrain/elevation.
            var fullBoard = SituationAslBoard.Assemble(inputs, map);
            var fullGrid = FeatureCompiler.Compile(fullBoard.Model, inputs.Catalog).Grid;
            foreach (var tile in addresses.Tiles)
            {
                var accepted = map.Tile(tile.Key);
                var dx = (tile.Crop.Column - SituationAslBoard.OriginColumn) * 225 / 4;
                var dy = (tile.Crop.Row - SituationAslBoard.OriginRow) * 129 / 2;
                for (var x = 225; x < 2025 && x + dx < fullGrid.Geometry.GridWidth; x++)
                for (var y = 129; y < 1161 && y + dy < fullGrid.Geometry.GridHeight; y++)
                {
                    if (accepted.Grid.CodeAt(x, y) != fullGrid.CodeAt(x + dx, y + dy) ||
                        accepted.Grid.ElevationAt(x, y) != fullGrid.ElevationAt(x + dx, y + dy))
                        Assert.Fail($"Accepted raster differs at {tile.Key} pixel {x},{y}.");
                }
                foreach (var cell in tile.Cells)
                {
                    var original = accepted.Grid.Geometry.CenterPoint(new HexIndex(cell.Column - tile.Crop.Column, cell.Row - tile.Crop.Row));
                    var translated = fullGrid.Geometry.CenterPoint(new HexIndex(cell.Column - SituationAslBoard.OriginColumn,
                        cell.Row - SituationAslBoard.OriginRow));
                    Assert.Equal(accepted.Grid.CodeAt(original.X, original.Y), fullGrid.CodeAt(translated.X, translated.Y));
                    Assert.Equal(accepted.Grid.ElevationAt(original.X, original.Y), fullGrid.ElevationAt(translated.X, translated.Y));
                }
            }
            Assert.Equal(fullBoard.Version, SituationAslBoard.Assemble(inputs, map).Version);
            var json = JsonSerializer.Serialize(draft);
            var reloaded = JsonSerializer.Deserialize<SituationDeploymentDraft>(json)!;
            Assert.Equal(json, JsonSerializer.Serialize(service.Review(reloaded)));
            Assert.Equal(json, JsonSerializer.Serialize(service.Assemble(formations.Reverse())));
            Assert.Equal(json, JsonSerializer.Serialize(service.Assemble(formations, reloaded.Positions)));
            foreach (var weapon in roster.Ground.SelectMany(f => f.Units).Where(u => u.Holding is not null))
            {
                var p = draft.Positions.Single(p => p.UnitId == weapon.Id);
                var crew = draft.Positions.Single(p => p.UnitId == weapon.Holding!.Holder);
                Assert.Equal((crew.Column, crew.Row), (p.Column, p.Row));
            }
            var handoff = await SituationHandoffs.Prepare(inputs, map, draft, "whole-goodwood-test");
            Assert.Equal(fullBoard.Version, handoff.BoardVersion);
            Assert.Equal(json, JsonSerializer.Serialize(handoff.Setup));
            Assert.Equal(8, handoff.Roster.AirSupport.Aircraft.Length);
            var restoredHandoff = JsonSerializer.Deserialize<SituationHandoff>(JsonSerializer.Serialize(handoff))!;
            await StudioHandoffTests.AssertFullImport(restoredHandoff, Path.Combine(root, "studio"));
            // An anchor change produces review, never an automatic move of existing squads.
            var changed = formations.ToArray();
            changed[0] = changed[0] with { Anchor = open[200].Key };
            var review = service.Assemble(changed, reloaded.Positions);
            Assert.False(review.Ready);
            Assert.Equal(draft.Positions, review.Positions);
            Assert.Contains(review.Review, r => r.Reason.Contains("parent formation", StringComparison.Ordinal));
            Assert.Throws<InvalidDataException>(() => service.Review(draft with { Policy = "future" }));
            Assert.Throws<InvalidDataException>(() => service.Assemble(formations, [draft.Positions[0], draft.Positions[0]]));
            Assert.Throws<OperationCanceledException>(() => service.Assemble(formations, cancellationToken: new CancellationToken(true)));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
