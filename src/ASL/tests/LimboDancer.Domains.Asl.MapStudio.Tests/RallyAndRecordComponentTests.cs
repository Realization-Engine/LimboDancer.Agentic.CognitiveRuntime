using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Components.Play;
using LimboDancer.Domains.Asl.MapStudio.Components.Shared;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// The Play components of pass 28b: S08 RuleHelp, the Rally and SW panels' contracts, the ordnance target fields' three ammunition cases (C17), and
/// the records list (R05). The page's use of them is in <see cref="PlayPagePass28bTests"/>.
/// </summary>
public sealed class RallyAndRecordComponentTests : IDisposable
{
    private readonly BunitContext context = new();

    public void Dispose() => context.Dispose();

    // S08: the summary always shows; the full text keeps the paragraph's id inside a closed disclosure.
    [Fact]
    public void RuleHelpShowsTheSummaryAndKeepsTheFullTextCollapsed()
    {
        var help = context.Render<RuleHelp>(parameters => parameters.Add(item => item.Id, "rout-help").Add(item => item.Summary, "Broken units rout (A10.5).")
            .AddChildContent("The whole rule."));
        Assert.Equal("Broken units rout (A10.5).", help.Find("#rout-help-summary").TextContent);
        Assert.Equal("The whole rule.", help.Find("#rout-help").TextContent);
        Assert.False(help.Find("details.rule-help-detail").HasAttribute("open"));
    }

    // Backlog section 23: each SW is a checkbox; checking one asks the page to give it to the second HS.
    [Fact]
    public void TheDeployPanelChecksEachSwForTheSecondHs()
    {
        DeployActionPanel.Toggle? asked = null;
        var panel = context.Render<DeployActionPanel>(parameters => parameters
            .Add(item => item.Squads, [new PlayChoice("g1", "g1 (german)")]).Add(item => item.Leaders, ["gl"]).Add(item => item.Weapons, ["gm1", "gm2"])
            .Add(item => item.Squad, "g1").Add(item => item.Second, new HashSet<string> { "gm1" })
            .Add(item => item.OnWeapon, (DeployActionPanel.Toggle pick) => asked = pick));
        Assert.True(panel.Find(".deploy-weapon[data-weapon='gm1']").HasAttribute("checked"));
        Assert.False(panel.Find(".deploy-weapon[data-weapon='gm2']").HasAttribute("checked"));
        panel.Find(".deploy-weapon[data-weapon='gm2']").Change(true);
        Assert.Equal(new DeployActionPanel.Toggle("gm2", true), asked);

        var none = context.Render<DeployActionPanel>(parameters => parameters
            .Add(item => item.Squads, [new PlayChoice("g1", "g1 (german)")]).Add(item => item.Leaders, []).Add(item => item.Weapons, []));
        Assert.Empty(none.FindAll("#deploy-weapons"));
        Assert.True(none.Find("#propose-deploy").HasAttribute("disabled"));
    }

    [Fact]
    public void DeployPanelRequiresTheBackendRequestedSpotterChoice()
    {
        string? selected = null;
        var panel = context.Render<DeployActionPanel>(parameters => parameters
            .Add(item => item.Squads, [new PlayChoice("g1", "g1")]).Add(item => item.Leaders, []).Add(item => item.Weapons, [])
            .Add(item => item.Squad, "g1").Add(item => item.SpotterChoiceRequired, true)
            .Add(item => item.OnSpotterHalf, (string value) => selected = value));
        Assert.True(panel.Find("#propose-deploy").HasAttribute("disabled"));
        Assert.Contains("remain pending", panel.Markup, StringComparison.Ordinal);
        panel.Find("#deploy-spotter").Change("second");
        Assert.Equal("second", selected);
        panel.Render(parameters => parameters.Add(item => item.SpotterHalf, "second"));
        Assert.False(panel.Find("#propose-deploy").HasAttribute("disabled"));
        panel.Render(parameters => parameters.Add(item => item.SpotterChoiceRequired, false));
        Assert.Empty(panel.FindAll("#deploy-spotter"));
    }

    // A04: a weapon nobody possesses may only be Recovered.
    [Fact]
    public void TheSupportWeaponPanelOffersOnlyRecoveryForALooseWeapon()
    {
        string? proposed = null;
        var panel = context.Render<SupportWeaponActionPanel>(parameters => parameters
            .Add(item => item.Weapons, [new PlayChoice("gm", "gm (left in bd01:E5:0)")]).Add(item => item.Units, [new PlayChoice("g2", "g2 (german)")])
            .Add(item => item.Weapon, "gm").Add(item => item.Unit, "g2").Add(item => item.Held, false).Add(item => item.OnPropose, (string kind) => proposed = kind));
        Assert.True(panel.Find("#propose-transfer").HasAttribute("disabled"));
        Assert.True(panel.Find("#propose-drop").HasAttribute("disabled"));
        Assert.True(panel.Find("#propose-dismantle").HasAttribute("disabled"));
        panel.Find("#propose-recover").Click();
        Assert.Equal(SupportWeaponActionPanel.Recover, proposed);
    }

    // The Play streamlining plan, S4: a Recovery the phase does not allow is held back, with the planner's reason as its tooltip.
    [Fact]
    public void TheSupportWeaponPanelHoldsBackARecoveryThePhaseDoesNotAllow()
    {
        var panel = context.Render<SupportWeaponActionPanel>(parameters => parameters
            .Add(item => item.Weapons, [new PlayChoice("gm", "gm (left in bd01:E5:0)")]).Add(item => item.Units, [new PlayChoice("g2", "g2 (german)")])
            .Add(item => item.Weapon, "gm").Add(item => item.Unit, "g2").Add(item => item.Held, false)
            .Add(item => item.RecoverBar, "A SW is Recovered in the RPh or the MPh (A4.44)."));
        Assert.True(panel.Find("#propose-recover").HasAttribute("disabled"));
        Assert.Equal("A SW is Recovered in the RPh or the MPh (A4.44).", panel.Find("#propose-recover").GetAttribute("title"));
    }

    // The Play streamlining plan, S4: a Rally attempt the gate would refuse, such as a Self-Rally the unit may not make, is held back with the reason.
    [Fact]
    public void TheRallyPanelHoldsBackAnAttemptTheGateWouldRefuse()
    {
        var panel = context.Render<RallyActionPanel>(parameters => parameters.Add(item => item.Units, [new PlayChoice("r1", "r1 (russian)")])
            .Add(item => item.Leaders, []).Add(item => item.Unit, "r1").Add(item => item.Barred, "R1 has no Self-Rally capability; it needs an unbroken leader in its Location."));
        Assert.True(panel.Find("#propose-rally").HasAttribute("disabled"));
        Assert.Contains("needs an unbroken leader", panel.Find("#propose-rally-ready").TextContent, StringComparison.Ordinal);

        panel.Render(parameters => parameters.Add(item => item.Barred, (string?)null));
        Assert.False(panel.Find("#propose-rally").HasAttribute("disabled"));
    }

    // A09 and A10: with nothing to choose, neither renders anything.
    [Fact]
    public void DmRetentionAndShockRecoveryRenderNothingWhenEmpty()
    {
        Assert.Empty(context.Render<DmRetentionChoices>(parameters => parameters.Add(item => item.Units, [])).Markup.Trim());
        Assert.Empty(context.Render<ShockRecoveryAction>(parameters => parameters.Add(item => item.Vehicles, [])).Markup.Trim());
    }

    // C17: a light mortar takes no vehicle (C9.3); a LATW or Panzerfaust has its fixed ammunition; a Gun chooses what it carries (C8.1).
    [Fact]
    public void TheOrdnanceTargetFieldsFollowTheWeapon()
    {
        IRenderedComponent<OrdnanceTargetFields> Fields(OrdnanceTargetFields.AmmunitionKind kind) => context.Render<OrdnanceTargetFields>(parameters => parameters
            .Add(item => item.Targets, ["bd01:E6:0"]).Add(item => item.Vehicles, [new PlayChoice("ge-tank", "ge-tank (attacker-tank)")]).Add(item => item.Ammunition, kind)
            .Add(item => item.Fixed, "HEAT (C13.34, C13.43)").Add(item => item.Ammunitions, [new("ap", "AP", null, null), new("apds", "APDS (Depletion Number 6)", 6, null),
                new("he", "HE (Depletion Number 8)", 8, "it has run out (C8.9)"), new("canister", "Canister", null, "none at the scenario's date (C8.11)")])
            .Add(item => item.Target, "bd01:E6:0"));

        var mortar = Fields(OrdnanceTargetFields.AmmunitionKind.None);
        Assert.Empty(mortar.FindAll("#ordnance-vehicle"));
        Assert.Empty(mortar.FindAll("#ordnance-ammunition"));

        var latw = Fields(OrdnanceTargetFields.AmmunitionKind.Fixed);
        Assert.Single(latw.FindAll("#ordnance-vehicle"));
        Assert.Equal("HEAT (C13.34, C13.43)", latw.Find("#ordnance-pf-heat").TextContent);
        Assert.Empty(latw.FindAll("#ordnance-ammunition"));

        var gun = Fields(OrdnanceTargetFields.AmmunitionKind.Selectable);
        // Pass 55 (C8.9): a Special Ammunition shows its Depletion Number; one the Gun has run out of is not offered, and the note says why.
        Assert.Equal(["ap", "apds"], gun.FindAll("#ordnance-ammunition option").Select(item => item.GetAttribute("value")));
        Assert.Equal(["AP", "APDS (Depletion Number 6)"], gun.FindAll("#ordnance-ammunition option").Select(item => item.TextContent));

        // The table player's review: the note names an ammunition as its choice does ("Canister", not "CANISTER").
        Assert.Equal("HE: it has run out (C8.9); Canister: none at the scenario's date (C8.11)", gun.Find("#ordnance-ammunition-note").TextContent);
        Assert.Empty(gun.FindAll("#ordnance-pf-heat"));
        Assert.Empty(latw.FindAll("#ordnance-ammunition-note"));
    }

    // R05: each record keeps its kind as its class and its event as data-event; an empty list renders no heading.
    [Fact]
    public void TheRecordListKeepsEachKindAndEvent()
    {
        var list = context.Render<ActionRecordList>(parameters => parameters.Add(item => item.Heading, "Rally, Rout, and other actions").Add(item => item.ListId, "play-rallies")
            .Add(item => item.Records, [new ActionRecordList.Entry("a-1", "transfer", "g1 passes gm to g2 (A4.431)"), new ActionRecordList.Entry("b-2", "dm", "r1 comes under DM (A10.62)")]));
        Assert.Equal("Rally, Rout, and other actions", list.Find("h3").TextContent);
        Assert.Equal("a-1", list.Find("#play-rallies .transfer-record").GetAttribute("data-event"));
        Assert.Equal("R1 comes under DM (A10.62)", list.Find("#play-rallies .dm-record").TextContent.Trim());
        Assert.Empty(context.Render<ActionRecordList>(parameters => parameters.Add(item => item.Heading, "Snipers").Add(item => item.ListId, "play-snipers")
            .Add(item => item.Records, [])).Markup.Trim());
    }
}
