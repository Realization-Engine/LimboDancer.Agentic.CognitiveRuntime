using System.Text.Json.Nodes;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class ScenarioA1AmmunitionDefaultTests
{
    private static readonly AmmunitionStockFacts Ordinary = new(false, false, false);
    private static readonly AmmunitionDefaultFacts Truck = new(false, AmmunitionDefaultWeapon.Gun,
        AmmunitionTargetProtection.Unarmored, AmmunitionDefaultTargetType.Vehicle, Ordinary, Ordinary);
    private static readonly Lazy<ScenarioA1OrdnanceReference> Reference = new(() => new ScenarioA1OrdnancePackage().Reference);
    private const string Contract = "c2.21-ammunition-default";

    [Theory]
    [InlineData(AmmunitionTargetProtection.Unarmored, AmmunitionDefaultTargetType.Vehicle, DefaultAmmunition.He)]
    [InlineData(AmmunitionTargetProtection.Unarmored, AmmunitionDefaultTargetType.Infantry, DefaultAmmunition.He)]
    [InlineData(AmmunitionTargetProtection.Unarmored, AmmunitionDefaultTargetType.Area, DefaultAmmunition.He)]
    [InlineData(AmmunitionTargetProtection.Armored, AmmunitionDefaultTargetType.Vehicle, DefaultAmmunition.Ap)]
    public void DefaultsFollowProtectionRatherThanVehicleIdentity(AmmunitionTargetProtection protection, AmmunitionDefaultTargetType targetType, DefaultAmmunition expected)
    {
        var facts = Truck with
        {
            TargetProtection = protection,
            TargetType = targetType
        };
        var before = facts;
        var verdict = Decide(facts);
        Assert.Equal(AmmunitionDefaultDisposition.DefaultSelected, verdict.Disposition);
        Assert.Equal(expected, verdict.Ammunition);
        Assert.Equal(("C2.21", 167), (verdict.Rule, verdict.Page));
        Assert.Equal(before, facts);
        Assert.Equal(verdict, Decide(facts with
        {
            Ap = expected == DefaultAmmunition.He ? null : Ordinary,
            He = expected == DefaultAmmunition.Ap ? null : Ordinary
        }));
    }

    [Fact]
    public void TargetTypeRestrictionsNeverBecomeSubstituteAmmunition()
    {
        AssertOutcome(AmmunitionDefaultDisposition.DeclarationRequired, Truck with
        {
            TargetProtection = AmmunitionTargetProtection.Armored,
            TargetType = AmmunitionDefaultTargetType.Area
        });
        AssertOutcome(AmmunitionDefaultDisposition.NotEvaluated, Truck with
        {
            TargetProtection = AmmunitionTargetProtection.Armored,
            TargetType = AmmunitionDefaultTargetType.Infantry
        });
        AssertOutcome(AmmunitionDefaultDisposition.NotEvaluated, Truck with
        {
            WeaponKind = AmmunitionDefaultWeapon.Mortar
        });
        Assert.Equal(DefaultAmmunition.He, Decide(Truck with
        {
            WeaponKind = AmmunitionDefaultWeapon.Mortar,
            TargetType = AmmunitionDefaultTargetType.Area
        }).Ammunition);
    }

    [Fact]
    public void KnownStockObstaclesRequireDeclarationWithoutFallbackOrIrrelevantFacts()
    {
        AmmunitionStockFacts[] obstacles = [new(true, false, false), new(false, true, false), new(false, false, true),
            new(null, true, null), new(true, null, null), new(null, null, true)];
        foreach (var obstacle in obstacles)
        {
            AssertOutcome(AmmunitionDefaultDisposition.DeclarationRequired, Truck with
            {
                He = obstacle
            });
            AssertOutcome(AmmunitionDefaultDisposition.DeclarationRequired, Truck with
            {
                TargetProtection = AmmunitionTargetProtection.Armored,
                Ap = obstacle
            });
        }

        Assert.Equal("C8.8", Decide(Truck with
        {
            He = new(true, true, true)
        }).Rule);
        Assert.Contains("counter prohibits", Decide(Truck with
        {
            He = new(true, null, true)
        }).Reason);
    }

    [Fact]
    public void UnknownAndUnsupportedInputsNeverBecomeOrdinaryDefaults()
    {
        AmmunitionDefaultFacts[] unknowns = [Truck with { Declared = null }, Truck with { WeaponKind = null },
            Truck with { WeaponKind = AmmunitionDefaultWeapon.Unsupported }, Truck with { WeaponKind = (AmmunitionDefaultWeapon)99 },
            Truck with { TargetProtection = null }, Truck with { TargetProtection = (AmmunitionTargetProtection)99 },
            Truck with { TargetType = null }, Truck with { TargetType = (AmmunitionDefaultTargetType)99 }, Truck with { He = null },
            Truck with { He = new(null, false, false) }, Truck with { He = new(false, null, false) }, Truck with { He = new(false, false, null) }];
        foreach (var unknown in unknowns)
        {
            AssertOutcome(AmmunitionDefaultDisposition.NotEvaluated, unknown);
        }

        AssertOutcome(AmmunitionDefaultDisposition.NotApplicable, new(Declared: true));
        AssertOutcome(AmmunitionDefaultDisposition.NotApplicable, Truck with
        {
            Declared = true,
            He = new(true, true, true)
        });
    }

    [Fact]
    public void CounterAdapterDistinguishesListedNumbersFromDatedAvailability()
    {
        var gun = Reference.Value.Guns["british-at-gun"];
        var stock = ScenarioA1AmmunitionDefaultCalculator.StockFacts(gun, null, false);
        Assert.True(stock.He.ListsDepletionNumber);
        Assert.True(stock.He.CounterProhibits);
        Assert.Null(stock.Ap.Exhausted);
        Assert.Null(ScenarioA1Definitions.DepletionNumber(gun, "he", 1942, 7));
        Assert.NotNull(ScenarioA1Definitions.DepletionNumber(gun, "he", 1944, 7));
        AssertOutcome(AmmunitionDefaultDisposition.DeclarationRequired, Truck with
        {
            He = stock.He
        });
        var apGun = gun with
        {
            NoAp = false,
            SpecialAmmo = ["AP12^5"]
        };
        var apStock = ScenarioA1AmmunitionDefaultCalculator.StockFacts(apGun, false, null);
        Assert.True(apStock.Ap.ListsDepletionNumber);
        AssertOutcome(AmmunitionDefaultDisposition.DeclarationRequired, Truck with
        {
            TargetProtection = AmmunitionTargetProtection.Armored,
            Ap = apStock.Ap
        });
    }

    [Fact]
    public void ContractPreservesUnknownsExplicitDeclarationsAndPrivateAnchors()
    {
        var facts = new JsonObject
        {
            ["declared"] = false,
            ["firerId"] = "crew",
            ["weaponKind"] = "gun",
            ["targetType"] = "vehicle",
            ["targetProtection"] = "unarmored",
            ["heProhibited"] = false,
            ["heListedDepletion"] = false,
            ["heExhausted"] = false,
        };
        Assert.Equal("default-he", ContractOutcome(facts));
        facts["targetProtection"] = "armored";
        Assert.Equal("not-evaluated", ContractOutcome(facts));
        facts["apProhibited"] = false;
        facts["apListedDepletion"] = false;
        facts["apExhausted"] = false;
        Assert.Equal("default-ap", ContractOutcome(facts));
        facts["firerId"] = null;
        Assert.Equal("not-evaluated", ContractOutcome(facts));
        facts["apListedDepletion"] = true;
        Assert.Equal("not-evaluated", ContractOutcome(facts));
        Assert.Equal("not-applicable", ContractOutcome(new JsonObject { ["declared"] = true }));
        Assert.Equal("not-evaluated", ContractOutcome(new JsonObject { ["declared"] = true, ["apExhausted"] = "invalid" }));
        var contract = ScenarioA1RuleContracts.Find(Contract)!;
        Assert.All(contract.Facts, fact => Assert.True(fact.MayBeUnknown));
        Assert.All(contract.Facts.Where(fact => fact.Disclosure == ContractDisclosure.Owner), fact => Assert.Equal("firerId", fact.Concerns));
        Assert.Empty(contract.Events);
    }

    [Fact]
    public void LegacyReadingsKeepTheirIntentionalDifferencesAndExactStrings()
    {
        Assert.True(ScenarioA1Definitions.HeAssumed("defender-at-gun"));
        Assert.False(ScenarioA1Definitions.HeAssumed("british-at-gun"));
        Assert.False(ScenarioA1Definitions.HeAssumed("unknown-gun"));
        AssertOutcome(AmmunitionDefaultDisposition.DeclarationRequired, Truck with
        {
            He = Ordinary with
            {
                Exhausted = true
            }
        });
        Assert.True(ScenarioA1AmmunitionDefaultCalculator.LegacyDuelAmmunitionMatches(true, "ap"));
        Assert.False(ScenarioA1AmmunitionDefaultCalculator.LegacyDuelAmmunitionMatches(true, "he"));
        Assert.Equal(DefaultAmmunition.He, Decide(Truck).Ammunition);
        foreach (var text in new string?[] { null, "", "AP", "HE", "he" })
        {
            Assert.False(ScenarioA1AmmunitionDefaultCalculator.LegacyDuelAmmunitionMatches(true, text));
        }

        Assert.True(ScenarioA1AmmunitionDefaultCalculator.LegacyDuelAmmunitionMatches(false, null));
        Assert.False(ScenarioA1AmmunitionDefaultCalculator.LegacyDuelAmmunitionMatches(false, "he"));
        Assert.True(ScenarioA1AmmunitionDefaultCalculator.LegacyHeNeedsDeclaration(null));
        Assert.False(ScenarioA1AmmunitionDefaultCalculator.LegacyHeNeedsDeclaration(string.Empty));
    }

    private static AmmunitionDefaultVerdict Decide(AmmunitionDefaultFacts facts) => ScenarioA1AmmunitionDefaultCalculator.SelectDefault(facts);

    private static void AssertOutcome(AmmunitionDefaultDisposition expected, AmmunitionDefaultFacts facts)
    {
        var answer = Decide(facts);
        Assert.Equal(expected, answer.Disposition);
        Assert.Null(answer.Ammunition);
    }

    private static string ContractOutcome(JsonObject facts) => JsonNode.Parse(ScenarioA1RuleContracts.Evaluate(Contract, facts.ToJsonString()))!["outcome"]!.GetValue<string>();
}
