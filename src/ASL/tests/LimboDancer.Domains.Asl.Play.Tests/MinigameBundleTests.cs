using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// The shared minigame test class (the Rules-first plan, section 7.2; the proving slice, ruling RPS.13): every bundle's manifest is sound (its rules are
/// rows of the inventory, its contracts exist, its card loads and names the bundle), every check gives its expected verdict, and a game's record
/// carries its minigame in its header and no other game's record changes. A claim writes no test code for its position checks.
/// </summary>
public sealed class MinigameBundleTests : IDisposable
{
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, UnitVocabulary.Asl())!.Catalog!;

    private static readonly Lazy<string> Inventory = new(() =>
        File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "ASL", "docs", "ASL Rule Inventory A to E.md")));

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-minigames-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string RepositoryRoot()
    {
        var configured = Environment.GetEnvironmentVariable("ASL_REPO");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "ASL", "LimboDancer.Domains.Asl.sln")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("The repository root was not found.");
    }

    public static TheoryData<string, string> Checks()
    {
        var data = new TheoryData<string, string>();
        foreach (var bundle in Minigames.All)
        {
            foreach (var check in bundle.Checks)
            {
                data.Add(bundle.Id, check.Name);
            }
        }

        return data;
    }

    [Fact]
    public void EveryBundleIsSound()
    {
        // Every bundle folder of the repository is embedded and read (the C9.3 claim: no claim edits this count when it adds its bundle).
        Assert.Equal(Directory.GetDirectories(Path.Combine(RepositoryRoot(), "src", "ASL", "units", "minigames")).Length, Minigames.All.Count);
        Assert.Equal(Minigames.All.Count, Minigames.All.Select(bundle => bundle.Id).Distinct().Count());
        foreach (var bundle in Minigames.All)
        {
            var read = ScenarioCards.Read(bundle.Card, Catalog)!;
            Assert.True(read.IsValid, $"{bundle.Id}: {string.Join("; ", read.Diagnostics)}");
            var card = read.Card!;
            Assert.Equal(bundle.Id, card.Minigame);
            Assert.Equal(bundle.Id, Minigames.BundleOfCard(card.Id));
            Assert.NotEmpty(bundle.Checks);
            foreach (var rule in bundle.Rules)
            {
                Assert.True(Inventory.Value.Contains($"\n| {rule} |", StringComparison.Ordinal), $"{bundle.Id}: {rule} is not a row of the inventory");
                Assert.Contains(bundle, Minigames.ForRule(rule));
            }

            foreach (var contract in bundle.Contracts)
            {
                Assert.NotNull(ScenarioA1RuleContracts.Find(contract));
                Assert.Contains(bundle, Minigames.ForContract(contract));
            }

            Assert.All(bundle.Checks, check => Assert.Contains(check.Contract, bundle.Contracts));

            // A check is seen by one of the card's sides or by the adjudicator, and each unit it names has one of the card's sides.
            var sides = card.Sides.Select(side => side.Side).ToHashSet(StringComparer.Ordinal);
            Assert.All(bundle.Checks, check => Assert.True(check.Viewer == Adjudicator || sides.Contains(check.Viewer), $"{bundle.Id}: viewer '{check.Viewer}'"));
            Assert.All(bundle.Units ?? new Dictionary<string, string>(), unit => Assert.True(sides.Contains(unit.Value), $"{bundle.Id}: {unit.Key} is '{unit.Value}'"));

            // A "?" stack is picked for a Unit fact of the contract that the check leaves unknown, and is the other side's.
            foreach (var check in bundle.Checks.Where(check => check.Sealed is not null))
            {
                var contract = ScenarioA1RuleContracts.Find(check.Contract)!;
                Assert.All(check.Sealed!, pick =>
                {
                    Assert.Contains(contract.Facts, fact => fact.Name == pick.Key && fact.Type == ContractFactType.Unit);
                    Assert.Null(check.Facts[pick.Key]);
                    Assert.True(sides.Contains(pick.Value) && pick.Value != check.Viewer, $"{bundle.Id}: '{check.Name}' seals a {pick.Value} stack");
                });
            }
        }

        // No built-in card is a minigame's, and a card that is not one maps to no bundle.
        Assert.False(Minigames.IsMinigameCard("armor-test"));
        Assert.Null(Minigames.BundleOfCard(null));
    }

    [Theory]
    [MemberData(nameof(Checks))]
    public void EveryCheckGivesItsExpectedVerdict(string bundleId, string checkName)
    {
        var bundle = Minigames.Find(bundleId)!;
        var check = bundle.Checks.Single(item => item.Name == checkName);
        var contract = ScenarioA1RuleContracts.Find(check.Contract)!;
        Assert.Null(HiddenFrom(bundle, check, contract));
        var verdict = ScenarioA1RuleContracts.Evaluate(contract, check.Facts);
        Assert.True(check.Expect.Outcome == verdict.Outcome, $"{checkName}: {verdict.Outcome}, {string.Join("; ", verdict.Reasons.Select(reason => reason.Text))}");
        foreach (var text in check.Expect.Reasons ?? [])
        {
            Assert.Contains(verdict.Reasons, reason => reason.Text.Contains(text, StringComparison.Ordinal));
        }
    }

    private const string Adjudicator = "adjudicator";

    /// <summary>
    /// The first fact a check gives that its viewer may not know, as the Rule Workbench reads a contract's disclosure, or null: a fact disclosed to the
    /// owner (or the opponent) is known only to the side (or the other side) of the unit it concerns, and the adjudicator knows every fact. Without it,
    /// a check that claims one side's view proves only the verdict on its facts (the review of the C9.3 replacement, finding 3).
    /// </summary>
    private static string? HiddenFrom(MinigameManifest bundle, MinigameCheck check, RuleContract contract)
    {
        if (check.Viewer == Adjudicator)
        {
            return null;
        }

        foreach (var fact in contract.Facts.Where(fact => fact.Disclosure != ContractDisclosure.All && check.Facts[fact.Name] is not null))
        {
            // The unit is named, or picked as a "?" stack whose handle gives only its side (PS4).
            var unit = fact.Concerns is null ? null : check.Facts[fact.Concerns]?.GetValue<string>();
            var sealedSide = fact.Concerns is null ? null : check.Sealed?.GetValueOrDefault(fact.Concerns);
            if (unit is null && sealedSide is null)
            {
                return $"'{fact.Name}' concerns a unit the check does not name, so no side may know it";
            }

            if ((unit is null ? sealedSide : bundle.Units?.GetValueOrDefault(unit)) is not { } side)
            {
                return $"'{unit}' has no side in the manifest's units";
            }

            unit ??= $"the {side} \"?\" stack";

            if ((fact.Disclosure == ContractDisclosure.Owner) != (side == check.Viewer))
            {
                return $"the {check.Viewer} viewer may not know '{fact.Name}' of {unit}, a {side} unit";
            }
        }

        return null;
    }

    [Fact]
    public void OnlyAMinigamesRecordCarriesItsBundleAndTheStoreReadsItFromTheHeader()
    {
        var scope = new GameScope(Guid.Parse("7b1d2c3e-0000-4000-8000-0000000004a1"), "mg");
        var plain = new GameRecord("A game", false, []);
        var text = GameEventWriter.Write(scope, plain);
        Assert.DoesNotContain("minigame", text, StringComparison.Ordinal);
        Assert.Null(GameEventReader.Read(System.Text.Encoding.UTF8.GetBytes(text)).Record!.Minigame);

        var minigame = GameEventWriter.Write(scope, plain with
        {
            Minigame = "a10-rally"
        });
        Assert.Equal("a10-rally", GameEventReader.Read(System.Text.Encoding.UTF8.GetBytes(minigame)).Record!.Minigame);

        var store = new FileGameStore(root);
        var path = Path.Combine(root, scope.Tenant.ToString("N"), scope.Game + ".game.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, minigame);
        Assert.Equal("a10-rally", store.MinigameOf(scope));
        File.WriteAllText(path, text);
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(1));
        Assert.Null(store.MinigameOf(scope));
        Assert.Null(store.MinigameOf(new GameScope(scope.Tenant, "absent")));
    }
}
