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
        Assert.Equal(3, Minigames.All.Count);
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
        }

        // No built-in card is a minigame's, and a card that is not one maps to no bundle.
        Assert.False(Minigames.IsMinigameCard("armor-test"));
        Assert.Null(Minigames.BundleOfCard(null));
    }

    [Theory]
    [MemberData(nameof(Checks))]
    public void EveryCheckGivesItsExpectedVerdict(string bundleId, string checkName)
    {
        var check = Minigames.Find(bundleId)!.Checks.Single(item => item.Name == checkName);
        var verdict = ScenarioA1RuleContracts.Evaluate(ScenarioA1RuleContracts.Find(check.Contract)!, check.Facts);
        Assert.True(check.Expect.Outcome == verdict.Outcome, $"{checkName}: {verdict.Outcome}, {string.Join("; ", verdict.Reasons.Select(reason => reason.Text))}");
        foreach (var text in check.Expect.Reasons ?? [])
        {
            Assert.Contains(verdict.Reasons, reason => reason.Text.Contains(text, StringComparison.Ordinal));
        }
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
