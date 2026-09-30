using System.Security.Cryptography;
using System.Text;
using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.Catalog;
using Microsoft.Extensions.DependencyInjection;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Pass 22 (ruling R22.4): every game starts from a card, so a page test's game starts from a minimal card holding what the retired new-game form
/// held: the boards (<c>board</c>), the sides (<c>first</c>, <c>second</c>), their ELR, SAN, and edges (<c>first-elr</c>, <c>second-san</c>,
/// <c>first-edge</c>, ...), the month, year, SSR tokens (<c>rules</c>), Scenario Defender, and label. The card is saved as a user card and chosen.
/// </summary>
internal static class MinimalCards
{
    public static void Choose(IRenderedComponent<PlayPage> page, IReadOnlyDictionary<string, string> fields)
    {
        var live = page.Services.GetRequiredService<LivePlay>();
        var catalog = live.Catalogs.First(item => item.Publication == CatalogPublication.Published);
        string Field(string key, string fallback = "") => fields.TryGetValue(key, out var value) ? value : fallback;
        int Number(string key) => int.TryParse(Field(key), out var value) ? value : 0;
        int? Optional(string key) => int.TryParse(Field(key), out var value) ? value : null;

        var boards = Field("board", "bd01").Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(token =>
        {
            var at = token.IndexOf('@', StringComparison.Ordinal);
            if (at < 0)
            {
                return new ScenarioCardBoard(token, 0, 0, false);
            }

            var slot = token[(at + 1)..];
            var reversed = slot.EndsWith("/r", StringComparison.Ordinal);
            var parts = (reversed ? slot[..^2] : slot).Split(',');
            return new ScenarioCardBoard(token[..at], int.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
                int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture), reversed);
        }).ToArray();

        var first = Field("first", "german");
        var second = Field("second", "russian");
        ScenarioCardSide Side(string side, string prefix) => new(side, Number(prefix + "-san"), null,
            new ScenarioCardEdge(Field(prefix + "-edge"), Field(prefix + "-edge").Length > 0 ? "manufactured" : "none", null), string.Empty, [], null, Optional(prefix + "-elr"));
        string[] tokens = Field("rules").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var key = string.Join(";", fields.OrderBy(item => item.Key, StringComparer.Ordinal).Select(item => $"{item.Key}={item.Value}"));
        var id = "minimal-" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key)))[..12];
        var card = new ScenarioCard(ScenarioCards.Format, id, Field("label", "A minimal game"), $"{catalog.Identity.Catalog}@{catalog.Identity.Version}",
            new ScenarioCardSource("A user card for a page test (ruling R22.4).", "none", []), string.Empty, new ScenarioCardDate(0, Number("month"), Number("year")),
            string.Empty, boards, "top", null, new ScenarioCardTurns(30, false, first, first), Field("defender").Length > 0 ? Field("defender") : null,
            [Side(first, "first"), Side(second, "second")],
            tokens.Length > 0 ? [new ScenarioCardRule(1, "The special rules by token.", "token", tokens, [], null)] : [],
            new ScenarioCardVictory("other", "The players judge the result.", []), null);
        var refused = live.Cards.Save(card, catalog);
        if (refused.Count > 0)
        {
            throw new InvalidOperationException("The minimal card is refused: " + string.Join("; ", refused));
        }

        page.Find("#new-card").Change(id);
    }
}
