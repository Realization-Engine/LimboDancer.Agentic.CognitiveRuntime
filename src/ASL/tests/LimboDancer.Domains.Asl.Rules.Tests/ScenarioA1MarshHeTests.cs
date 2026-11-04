using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class ScenarioA1MarshHeTests
{
    private static readonly MarshHeFacts Marsh = new(MarshHeEffect.He, MarshHeTerrain.Marsh, MarshHeSnow.None, false, false, false, MarshHeTarget.Unit, null);
    private static readonly ScenarioA1FireReference Reference = new ScenarioA1FirePackage().Reference;

    [Theory]
    [InlineData(MarshHeEffect.He, 0.5)]
    [InlineData(MarshHeEffect.DemolitionCharge, 0.5)]
    [InlineData(MarshHeEffect.HeEquivalency, 0.5)]
    [InlineData(MarshHeEffect.NonHe, 1)]
    [InlineData(MarshHeEffect.Residual, 1)]
    public void EffectClassificationIncludesDcAndHeEquivalencyButNeverRehalvesResidual(MarshHeEffect effect, double expected)
    {
        AssertFactor((decimal)expected, Marsh with
        {
            Effect = effect
        });
        AssertFactor((decimal)expected, Marsh with
        {
            Effect = effect,
            Terrain = MarshHeTerrain.Mudflat
        });
        AssertFactor(1m, Marsh with
        {
            Effect = effect,
            Terrain = MarshHeTerrain.Other
        });
    }

    [Theory]
    [InlineData(MarshHeSnow.Snow, false, false, "B16.8")]
    [InlineData(MarshHeSnow.DeepSnow, false, false, "B16.8")]
    [InlineData(MarshHeSnow.None, true, false, "B16.8")]
    [InlineData(MarshHeSnow.None, false, true, "B16.6")]
    public void WeatherAndConnectedFloodingRemoveTheModifierIncludingMudflat(MarshHeSnow snow, bool cold, bool flooded, string rule)
    {
        foreach (var terrain in new[] { MarshHeTerrain.Marsh, MarshHeTerrain.Mudflat })
        {
            var result = ScenarioA1MarshHeCalculator.Evaluate(Marsh with
            {
                Terrain = terrain,
                SnowCover = snow,
                SubZero = cold,
                ConnectedToFloodedWater = flooded,
                BridgePresent = true,
                OnBridge = null,
            });
            Assert.Equal(1m, result.Multiplier);
            Assert.Equal(rule, result.Rule);
        }

        // Flooding elsewhere is not a connected flooded-water fact.
        AssertFactor(0.5m, Marsh);
    }

    [Theory]
    [InlineData(MarshHeTarget.Unit, true, true, 1d)]
    [InlineData(MarshHeTarget.Unit, true, false, 0.5)]
    [InlineData(MarshHeTarget.Unit, true, null, null)]
    [InlineData(MarshHeTarget.Unit, false, null, 0.5)]
    [InlineData(MarshHeTarget.Unit, null, false, 0.5)]
    [InlineData(MarshHeTarget.Unit, null, null, null)]
    [InlineData(MarshHeTarget.Bridge, true, null, 1d)]
    [InlineData(MarshHeTarget.Bridge, false, null, null)]
    [InlineData(MarshHeTarget.GroundLocation, true, null, 0.5)]
    [InlineData(MarshHeTarget.GroundLocation, null, null, 0.5)]
    public void BridgePresenceMattersOnlyWhenItCanChangeTheTargetExemption(MarshHeTarget target, bool? bridge, bool? onBridge, double? expected)
    {
        var facts = Marsh with
        {
            Target = target,
            BridgePresent = bridge,
            OnBridge = onBridge
        };
        if (expected is { } value)
        {
            AssertFactor((decimal)value, facts);
        }
        else
        {
            AssertUnknown(facts);
        }
    }

    [Fact]
    public void KnownExemptionsIgnoreIrrelevantUnknownsButSpecialProfilesDoNotMasqueradeAsMarsh()
    {
        AssertFactor(1m, Marsh with
        {
            Effect = MarshHeEffect.Residual,
            Terrain = null,
            SnowCover = null,
            SubZero = null,
            ConnectedToFloodedWater = null
        });
        AssertFactor(1m, Marsh with
        {
            Effect = MarshHeEffect.Residual,
            Terrain = MarshHeTerrain.Swamp
        });
        AssertFactor(1m, Marsh with
        {
            Effect = MarshHeEffect.NonHe,
            Terrain = MarshHeTerrain.Swamp
        });
        AssertFactor(1m, Marsh with
        {
            SnowCover = null,
            SubZero = null,
            ConnectedToFloodedWater = null,
            BridgePresent = true,
            OnBridge = true
        });
        AssertFactor(1m, Marsh with
        {
            SubZero = true,
            BridgePresent = null
        });
        AssertUnknown(Marsh with
        {
            SnowCover = null
        });
        AssertUnknown(Marsh with
        {
            Effect = null
        });
        AssertUnknown(Marsh with
        {
            Terrain = MarshHeTerrain.Swamp
        });
        AssertUnknown(Marsh with
        {
            Effect = MarshHeEffect.UnsupportedVtFfe
        });
        AssertUnknown(Marsh with
        {
            Terrain = (MarshHeTerrain)999
        });
        AssertUnknown(Marsh with
        {
            OnBridge = true
        });
        AssertUnknown(Marsh with
        {
            Target = MarshHeTarget.GroundLocation,
            BridgePresent = true,
            OnBridge = true
        });
    }

    [Theory]
    [InlineData("12", "6")]
    [InlineData("24", "12")]
    [InlineData("6", "3")]
    [InlineData("2", "1")]
    [InlineData("30", "15")]
    [InlineData("15", "7.5")]
    [InlineData("36", "18")]
    public void ExactMultiplierComposesWithAlreadyAppliedModifiersWithoutRounding(string suppliedFp, string expectedFp)
    {
        // CH/Area Fire have already affected the supplied component. Set DC 36 FP proves no legality or TEM permission.
        var multiplier = ScenarioA1MarshHeCalculator.Evaluate(Marsh).Multiplier!.Value;
        Assert.Equal(decimal.Parse(expectedFp, CultureInfo.InvariantCulture), decimal.Parse(suppliedFp, CultureInfo.InvariantCulture) * multiplier);
        // A thrown DC's other effect Location must receive its own answer.
        AssertFactor(1m, Marsh with
        {
            Effect = MarshHeEffect.DemolitionCharge,
            Terrain = MarshHeTerrain.Other
        });
    }

    [Theory]
    [InlineData(12, false, "marsh", "6")]
    [InlineData(12, true, "marsh", "12")]
    [InlineData(1, false, "marsh", "0.5")]
    [InlineData(12, false, "open-ground", "12")]
    public void LegacyOrdnancePreservesSerializedFirepowerIncludingFractionalAndCriticalHits(int fp, bool critical, string terrain, string expectedJson)
    {
        var attack = Attack(terrain) with
        {
            OrdnanceHit = new("gun", fp, critical)
        };
        var arithmetic = ScenarioA1FireCalculator.Preview(attack, Reference);
        Assert.NotNull(arithmetic);
        Assert.Equal(expectedJson, JsonSerializer.Serialize(arithmetic.TotalFirepower));
        // This specifically catches 6 versus 6.0, which numeric decimal equality misses.
        Assert.Equal(expectedJson, JsonSerializer.Serialize(fp * (critical ? 2 : 1) / ScenarioA1MarshHeCalculator.LegacyOrdnanceDivisor(terrain)));
        Assert.Equal("1", JsonSerializer.Serialize(ScenarioA1MarshHeCalculator.LegacyOrdnanceDivisor(null)));
        Assert.Equal("1", JsonSerializer.Serialize(ScenarioA1MarshHeCalculator.LegacyOrdnanceDivisor("Marsh")));
    }

    [Fact]
    public void LegacyDcStillKeepsThirtyFpIntoMarshUntilWiring()
    {
        var attack = Attack("marsh") with
        {
            FirerLocationId = "bd07:O6:0",
            Range = 1,
            FireGroupComplete = null,
            Firers = null,
            Los = null,
            DemolitionCharge = new("dc", "attacker-dc", FireDemolitionCharge.Thrown, "firer", "attacker-squad", false, false, null, null),
        };
        Assert.Equal(30m, ScenarioA1FireCalculator.Preview(attack, Reference)!.TotalFirepower);
        AssertFactor(0.5m, Marsh with
        {
            Effect = MarshHeEffect.DemolitionCharge
        });
    }

    [Fact]
    public void MarshContractRequiresPublicFactsAndKeepsPrivateBridgePositionBehindItsAnchor()
    {
        var contract = ScenarioA1RuleContracts.Find("b16.31-marsh-he")!;
        var facts = JsonNode.Parse("""
            {"effectFamily":"he","terrainKind":"marsh","snowCover":"none","subZero":false,
             "connectedToFloodedWater":false,"bridgePresent":false,"targetKind":"unit"}
            """)!.AsObject();
        Assert.Equal("halved", ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        foreach (var field in contract.Facts.Where(field => !field.MayBeUnknown))
        {
            var missing = facts.DeepClone().AsObject();
            missing.Remove(field.Name);
            Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(contract, missing).Outcome);
        }

        facts["bridgePresent"] = true;
        facts["onBridge"] = true;
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        facts["targetId"] = "target";
        Assert.Equal("unchanged", ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        facts["onBridge"] = false;
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var result = ScenarioA1RuleContracts.Evaluate(contract, facts);
            Assert.Equal("halved", result.Outcome);
            Assert.Contains(result.Reasons, reason => reason.Text.Contains("multiplier: 0.5", StringComparison.Ordinal));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }

        Assert.Empty(contract.Events);
        var position = Assert.Single(contract.Facts, field => field.Name == "onBridge");
        Assert.Equal((ContractDisclosure.Owner, "targetId", true), (position.Disclosure, position.Concerns, position.MayBeUnknown));
        Assert.Equal("targetId", Assert.Single(contract.Facts, field => field.Name == "targetId").Concerns);
    }

    private static FireAttack Attack(string terrain) => new("PFPh", "phasing", null, null, "bd07:P6:0", [], null, null,
        true, new FireLos(false, 0, true, false), 7, terrain,
        [new FireTarget("target", "defender-squad", "bd07:P6:0", false, false, false, false, false, false, false) { KnownEnemyInLos = true, Captors = [] }], 2, null);

    private static void AssertFactor(decimal expected, MarshHeFacts facts)
    {
        var result = ScenarioA1MarshHeCalculator.Evaluate(facts);
        Assert.Equal(MarshHeDisposition.Evaluated, result.Disposition);
        Assert.Equal(expected, result.Multiplier);
    }

    private static void AssertUnknown(MarshHeFacts facts)
    {
        var result = ScenarioA1MarshHeCalculator.Evaluate(facts);
        Assert.Equal(MarshHeDisposition.NotEvaluated, result.Disposition);
        Assert.Null(result.Multiplier);
    }
}
