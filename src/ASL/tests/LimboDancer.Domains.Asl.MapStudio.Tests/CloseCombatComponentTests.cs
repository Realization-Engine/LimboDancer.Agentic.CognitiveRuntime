using Bunit;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.MapStudio.Components.Play;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// The Close Combat, required choice, prisoner, and fire components extracted in pass 27 (plan task 27.5), each on its own: they hold no draft, so
/// each renders what the page gives it, under the element ids the page tests use, and raises what the player does.
/// </summary>
public sealed class CloseCombatComponentTests : IDisposable
{
    private readonly BunitContext context = new();

    public void Dispose() => context.Dispose();

    // C01 (A11.31): one block per open Location with its own units; each proposal names the Location it was made in.
    [Fact]
    public void VehicleCloseCombatOffersEachLocationItsOwnUnits()
    {
        string? attacked = null;
        string? passed = null;
        string? byVehicle = null;
        UnitSelectionList.Toggle? defender = null;
        var panel = context.Render<VehicleCloseCombatPanel>(parameters => parameters
            .Add(item => item.Locations, [
                new VehicleCloseCombatPanel.CcLocation("bd04:E5:0", "russian", ["r1", "r2"], ["rl"], [new VehicleCloseCombatPanel.VehicleChoice("gt", "german")], []),
                new VehicleCloseCombatPanel.CcLocation("bd04:F5:0", "german", [], [], [new VehicleCloseCombatPanel.VehicleChoice("gt2", "german")], ["r3"])])
            .Add(item => item.Defenders, new HashSet<string>())
            .Add(item => item.OnProposeAttack, at => attacked = at).Add(item => item.OnPass, at => passed = at).Add(item => item.OnProposeVehicle, at => byVehicle = at)
            .Add(item => item.OnDefender, toggle => defender = toggle));
        var first = panel.Find(".vehicle-cc[data-location='bd04:E5:0']");
        Assert.Contains("the russian side attacks next", first.TextContent, StringComparison.Ordinal);
        Assert.Equal(["", "r1", "r2"], first.QuerySelectorAll(".vehicle-cc-attacker option").Select(option => option.GetAttribute("value")));
        Assert.Equal("gt (german)", first.QuerySelectorAll(".vehicle-cc-vehicle option")[1].TextContent);
        Assert.Empty(first.QuerySelectorAll(".vehicle-cc-defender"));
        var second = panel.Find(".vehicle-cc[data-location='bd04:F5:0']");
        Assert.Single(second.QuerySelectorAll(".vehicle-cc-attacker option"));
        Assert.Contains("r3", second.TextContent, StringComparison.Ordinal);

        // The attack waits for an attacker and a vehicle, the vehicle's attack for a checked defender; a pass needs neither.
        Assert.True(panel.Find(".vehicle-cc[data-location='bd04:E5:0'] .propose-vehicle-cc-attack").HasAttribute("disabled"));
        Assert.True(panel.Find(".vehicle-cc[data-location='bd04:F5:0'] .propose-vehicle-cc-vehicle").HasAttribute("disabled"));
        panel.Find(".vehicle-cc[data-location='bd04:F5:0'] .propose-vehicle-cc-pass").Click();
        Assert.Equal("bd04:F5:0", passed);
        panel.Find(".vehicle-cc[data-location='bd04:F5:0'] .vehicle-cc-defender").Change(true);
        Assert.Equal(new UnitSelectionList.Toggle("r3", true), defender);

        panel.Render(parameters => parameters.Add(item => item.Attacker, "r1").Add(item => item.Vehicle, "gt").Add(item => item.Defenders, new HashSet<string> { "r3" }));
        panel.Find(".vehicle-cc[data-location='bd04:E5:0'] .propose-vehicle-cc-attack").Click();
        Assert.Equal("bd04:E5:0", attacked);
        panel.Find(".vehicle-cc[data-location='bd04:F5:0'] .propose-vehicle-cc-vehicle").Click();
        Assert.Equal("bd04:F5:0", byVehicle);
    }

    // C02 with C03/N05 kept inside: no declarations before a Location; the Ambush comes before any attack (A11.4).
    [Fact]
    public void TheCloseCombatPanelHoldsAttacksUntilTheAmbushIsRolled()
    {
        var ambush = false;
        string? round = "unset";
        var panel = context.Render<CloseCombatPanel>(parameters => parameters.Add(item => item.Locations, ["bd04:E5:0"]).Add(item => item.Due, ["bd04:E5:0: CC is due"])
            .Add(item => item.OnProposeAmbush, () => ambush = true).Add(item => item.OnRound, value => round = value));
        Assert.Contains("CC is due", panel.Find("#cc-due").TextContent, StringComparison.Ordinal);
        Assert.Empty(panel.FindAll("#cc-stacking"));
        Assert.Empty(panel.FindAll("#cc-prisoners-round"));
        Assert.True(panel.Find("#propose-ambush").HasAttribute("disabled"));

        panel.Render(parameters => parameters.Add(item => item.Location, "bd04:E5:0").Add(item => item.AmbushDue, true));
        Assert.NotNull(panel.Find("#cc-ambush-first"));
        Assert.Empty(panel.FindAll("#cc-attack"));
        Assert.Empty(panel.FindAll("#propose-cc"));
        Assert.Empty(panel.FindAll("#cc-hand-to-hand"));
        panel.Find("#propose-ambush").Click();
        Assert.True(ambush);
        panel.Find("#cc-prisoners-round").Change(true);
        Assert.Equal(CloseCombatResolved.PrisonersRound, round);

        // A rolled Ambush with a round taken: the round choice replaces the prisoners' round, and the status reads the ambusher.
        var at = BoardLocation.Parse("bd04:E5:0");
        panel.Render(parameters => parameters.Add(item => item.AmbushDue, false).Add(item => item.HandToHandAllowed, true)
            .Add(item => item.Entry, new CloseCombatLocation(at, true, "russian", [CloseCombatResolved.AmbusherRound], false)));
        Assert.Contains("The russian side ambushes.", panel.Find("#cc-status").TextContent, StringComparison.Ordinal);
        Assert.NotNull(panel.Find("#cc-round"));
        Assert.Empty(panel.FindAll("#cc-prisoners-round"));
        Assert.Equal("Propose: resolve CC with no attacks", panel.Find("#propose-cc").TextContent);
    }

    // C02 with C07 kept inside, N06 and N08 composed: the declared attacks read as declared, each removable; the withdrawal and infiltration propose their own.
    [Fact]
    public void TheCloseCombatPanelListsItsAttacksAndChoices()
    {
        CloseCombatPanel.DeclaredAttack? removed = null;
        AmbushWithdrawalActions.Withdrawal? withdrawn = null;
        CloseCombatInfiltrationChoices.Choice? infiltrated = null;
        var proposed = false;
        var attack = new CloseCombatPanel.DeclaredAttack(["r1", "r2"], ["g1"], "rl", true, ["g1"], "r2");
        var panel = context.Render<CloseCombatPanel>(parameters => parameters.Add(item => item.Locations, ["bd04:E5:0"]).Add(item => item.Location, "bd04:E5:0")
            .Add(item => item.Attacks, [attack]).Add(item => item.AmbushWithdrawals, [new AmbushWithdrawalActions.Withdrawal("r1", "bd04:E6:0")])
            .Add(item => item.Infiltrators, [new CloseCombatInfiltrationChoices.Infiltrator("r2", ["bd04:F5:0"])])
            .Add(item => item.OnRemoveAttack, value => removed = value).Add(item => item.OnAmbushWithdraw, value => withdrawn = value)
            .Add(item => item.OnInfiltration, value => infiltrated = value).Add(item => item.OnPropose, () => proposed = true));
        Assert.StartsWith("r1, r2 attempt to capture g1, directed by rl (the defender gives up g1)", panel.Find("#cc-attacks li").TextContent, StringComparison.Ordinal);
        Assert.Equal("Propose: resolve CC", panel.Find("#propose-cc").TextContent);
        panel.Find("#cc-attacks button").Click();
        Assert.Same(attack, removed);
        panel.Find(".ambush-withdraw[data-unit='r1'][data-to='bd04:E6:0']").Click();
        Assert.Equal(new AmbushWithdrawalActions.Withdrawal("r1", "bd04:E6:0"), withdrawn);
        Assert.Equal("to bd04:F5:0", panel.FindAll(".cc-infiltrate[data-unit='r2'] option")[1].TextContent);
        panel.Find(".cc-infiltrate[data-unit='r2']").Change("bd04:F5:0");
        Assert.Equal(new CloseCombatInfiltrationChoices.Choice("r2", "bd04:F5:0"), infiltrated);
        panel.Find("#propose-cc").Click();
        Assert.True(proposed);

        // No Location read: no infiltrations; no withdrawals offered: no withdrawal toolbar.
        panel.Render(parameters => parameters.Add(item => item.Infiltrators, null).Add(item => item.AmbushWithdrawals, null));
        Assert.Empty(panel.FindAll("#cc-infiltrations"));
        Assert.Empty(panel.FindAll("#cc-ambush-withdrawal"));
    }

    // C06 with N07 kept inside: a broken unit may not attack; a capture attempt offers the attackers as Guard, in order; the attack waits for both sides.
    [Fact]
    public void TheAttackBuilderOffersCaptureFieldsOnlyForACapture()
    {
        UnitSelectionList.Toggle? attacker = null;
        bool? capture = null;
        var added = false;
        var builder = context.Render<CloseCombatAttackBuilder>(parameters => parameters.Add(item => item.Declaring, true)
            .Add(item => item.Sides, [new CloseCombatAttackBuilder.DeclaringSide("russian", true,
                [new CloseCombatAttackBuilder.Participant("r2", "r2 (4-4-7)", false), new CloseCombatAttackBuilder.Participant("r1", "r1 (4-4-7, broken)", true)],
                [new CloseCombatAttackBuilder.Participant("g1", "g1 (4-6-7)", false)])])
            .Add(item => item.Attackers, new HashSet<string>()).Add(item => item.Defenders, new HashSet<string>())
            .Add(item => item.OnAttacker, value => attacker = value).Add(item => item.OnCapture, value => capture = value).Add(item => item.OnAdd, () => added = true));
        Assert.Contains("russian (ATTACKER)", builder.Find(".cc-side[data-side='russian']").TextContent, StringComparison.Ordinal);
        Assert.True(builder.Find(".cc-attacker[data-unit='r1']").HasAttribute("disabled"));
        Assert.False(builder.Find(".cc-attacker[data-unit='r2']").HasAttribute("disabled"));
        Assert.Equal("russian", builder.Find(".cc-defender[data-unit='g1']").GetAttribute("data-by"));
        Assert.Empty(builder.FindAll("#cc-guard"));
        Assert.True(builder.Find("#cc-add-attack").HasAttribute("disabled"));
        builder.Find(".cc-attacker[data-unit='r2']").Change(true);
        Assert.Equal(new UnitSelectionList.Toggle("r2", true), attacker);
        builder.Find("#cc-capture").Change(true);
        Assert.True(capture);

        builder.Render(parameters => parameters.Add(item => item.Capture, true).Add(item => item.Attackers, new HashSet<string> { "r2", "r1" })
            .Add(item => item.Defenders, new HashSet<string> { "g1" }));
        Assert.Equal(["", "r1", "r2"], builder.FindAll("#cc-guard option").Select(option => option.GetAttribute("value")));
        Assert.NotNull(builder.Find("#cc-yield"));
        builder.Find("#cc-add-attack").Click();
        Assert.True(added);

        // While the Ambush is to be rolled the sides show, but no attack is declared.
        builder.Render(parameters => parameters.Add(item => item.Declaring, false));
        Assert.Empty(builder.FindAll("#cc-attack"));
        Assert.NotEmpty(builder.FindAll(".cc-side"));
    }

    // C04 and C05: each declaration raised for its unit; a broken unit reads that it must try (A11.16).
    [Fact]
    public void StackingAndWithdrawalsRaiseEachUnitsChoice()
    {
        CloseCombatStacking.Choice? stacked = null;
        var stacking = context.Render<CloseCombatStacking>(parameters => parameters.Add(item => item.Smcs, [new CloseCombatStacking.Smc("rl", ["r1", "r2"])])
            .Add(item => item.Stacking, new Dictionary<string, string> { ["rl"] = "r2" }).Add(item => item.OnStacking, value => stacked = value));
        Assert.Equal("r2", stacking.Find(".cc-stack[data-unit='rl']").GetAttribute("value"));
        stacking.Find(".cc-stack[data-unit='rl']").Change(string.Empty);
        Assert.Equal(new CloseCombatStacking.Choice("rl", string.Empty), stacked);

        CloseCombatWithdrawals.Choice? withdrawn = null;
        var withdrawals = context.Render<CloseCombatWithdrawals>(parameters => parameters
            .Add(item => item.Units, [new CloseCombatWithdrawals.Withdrawer("r1", "r1 (4-4-7, broken, melee)", true, ["bd04:E6:0"]),
                new CloseCombatWithdrawals.Withdrawer("r2", "r2 (4-4-7, melee)", false, ["bd04:E6:0"])])
            .Add(item => item.OnWithdrawal, value => withdrawn = value));
        Assert.Contains("(broken: must try to withdraw)", withdrawals.FindAll("#cc-withdrawals label")[0].TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("must try", withdrawals.FindAll("#cc-withdrawals label")[1].TextContent, StringComparison.Ordinal);
        withdrawals.Find(".cc-withdraw[data-unit='r2']").Change("bd04:E6:0");
        Assert.Equal(new CloseCombatWithdrawals.Choice("r2", "bd04:E6:0"), withdrawn);
    }

    // C08 (ruling R5.8): the options only for a view that may answer; any other reads that it waits.
    [Fact]
    public void APendingChoiceShowsOptionsOnlyToItsSide()
    {
        string? chosen = null;
        var panel = context.Render<PendingChoicePanel>(parameters => parameters.Add(item => item.Key, "k1").Add(item => item.Side, "german")
            .Add(item => item.Description, "a PAATC").Add(item => item.MayAnswer, true)
            .Add(item => item.Options, [new PendingChoicePanel.Option("take", "take it"), new PendingChoicePanel.Option("decline", "decline it")])
            .Add(item => item.OnChoose, value => chosen = value));
        Assert.Equal("k1", panel.Find("#play-choice").GetAttribute("data-key"));
        Assert.Equal("Propose: decline it", panel.Find(".choose[data-option='decline']").TextContent);
        panel.Find(".choose[data-option='take']").Click();
        Assert.Equal("take", chosen);
        panel.Render(parameters => parameters.Add(item => item.MayAnswer, false).Add(item => item.Options, []));
        Assert.Empty(panel.FindAll(".choose"));
        Assert.Contains("Waiting for the german side to answer.", panel.Markup, StringComparison.Ordinal);
    }

    // C09 (A20.3, A20.21): take, reject, or free, only for the captor's view; freeing is its own proposal.
    [Fact]
    public void APendingSurrenderOffersItsThreeAnswers()
    {
        string? taken = null;
        var rejected = false;
        var freed = false;
        var panel = context.Render<PendingSurrenderPanel>(parameters => parameters.Add(item => item.Unit, "g1").Add(item => item.MayAnswer, true)
            .Add(item => item.Captors, ["r1", "r2"]).Add(item => item.OnTake, value => taken = value)
            .Add(item => item.OnReject, () => rejected = true).Add(item => item.OnFree, () => freed = true));
        Assert.Equal(2, panel.FindAll(".take-prisoner").Count);
        panel.Find(".take-prisoner[data-captor='r2']").Click();
        Assert.Equal("r2", taken);
        panel.Find(".free-surrender[data-unit='g1']").Click();
        Assert.True(freed);
        Assert.False(rejected);
        panel.Find(".reject-surrender[data-unit='g1']").Click();
        Assert.True(rejected);
        panel.Render(parameters => parameters.Add(item => item.MayAnswer, false).Add(item => item.Captors, []));
        Assert.Empty(panel.FindAll("button"));
        Assert.Contains("g1 has surrendered", panel.Find("[data-surrender='g1']").TextContent, StringComparison.Ordinal);
    }

    // N09 (A20.5): a transfer to each recipient and an abandonment for each Guard, each its own proposal.
    [Fact]
    public void PrisonerCustodyTransfersOrAbandons()
    {
        PrisonerCustodyActions.Transfer? transferred = null;
        string? abandoned = null;
        var custody = context.Render<PrisonerCustodyActions>(parameters => parameters
            .Add(item => item.Guards, [new PrisonerCustodyActions.Guard("r1", ["r2", "rl"]), new PrisonerCustodyActions.Guard("r3", [])])
            .Add(item => item.OnTransfer, value => transferred = value).Add(item => item.OnAbandon, value => abandoned = value));
        Assert.Equal(2, custody.FindAll(".transfer-prisoners[data-guard='r1']").Count);
        Assert.Empty(custody.FindAll(".transfer-prisoners[data-guard='r3']"));
        Assert.Equal("Propose: r1 hands its prisoners to rl", custody.Find(".transfer-prisoners[data-to='rl']").TextContent);
        custody.Find(".transfer-prisoners[data-to='r2']").Click();
        Assert.Equal(new PrisonerCustodyActions.Transfer("r1", "r2"), transferred);
        Assert.Null(abandoned);
        custody.Find(".abandon-prisoners[data-guard='r3']").Click();
        Assert.Equal("r3", abandoned);
    }

    // N12 (A23.4): each operable charge reads its target and modifiers, and proposes its own detonation.
    [Fact]
    public void PlacedChargesProposeTheirOwnDetonation()
    {
        string? detonated = null;
        var placed = context.Render<PlacedChargeDetonationActions>(parameters => parameters
            .Add(item => item.Charges, [new PlacedChargeDetonationActions.Placed("dc1", "bd04:E5:0", true, true), new PlacedChargeDetonationActions.Placed("dc2", "bd04:F5:0", false, false)])
            .Add(item => item.OnDetonate, value => detonated = value));
        Assert.Equal("Propose: detonate dc1 in bd04:E5:0 (30 FP, halved, +1 CX; A23.4)", placed.Find(".detonate-dc[data-dc='dc1']").TextContent);
        Assert.Equal("Propose: detonate dc2 in bd04:F5:0 (30 FP; A23.4)", placed.Find(".detonate-dc[data-dc='dc2']").TextContent);
        placed.Find(".detonate-dc[data-dc='dc2']").Click();
        Assert.Equal("dc2", detonated);
    }

    // N11 (A23.6): the DC and its holder as one choice, and the throw waits for it and a Location.
    [Fact]
    public void TheThrowWaitsForADcAndALocation()
    {
        string? charge = null;
        var proposed = false;
        var thrown = context.Render<ThrowDemolitionChargeAction>(parameters => parameters
            .Add(item => item.Charges, [new ThrowDemolitionChargeAction.HeldCharge("g1", "dc1")]).Add(item => item.OnCharge, value => charge = value)
            .Add(item => item.OnPropose, () => proposed = true));
        Assert.Equal("dc1 (g1)", thrown.FindAll("#throw-dc option")[1].TextContent);
        Assert.True(thrown.Find("#propose-throw-dc").HasAttribute("disabled"));
        thrown.Find("#throw-dc").Change("g1|dc1");
        Assert.Equal("g1|dc1", charge);
        thrown.Render(parameters => parameters.Add(item => item.Charge, "g1|dc1").Add(item => item.At, "  "));
        Assert.True(thrown.Find("#propose-throw-dc").HasAttribute("disabled"));
        thrown.Render(parameters => parameters.Add(item => item.At, "bd04:E5:0"));
        thrown.Find("#propose-throw-dc").Click();
        Assert.True(proposed);
    }

    // C12 (A7.25): the units the page offers, and the proposal waits for one.
    [Fact]
    public void OpportunityFireWaitsForAUnit()
    {
        UnitSelectionList.Toggle? toggled = null;
        var action = context.Render<OpportunityFireAction>(parameters => parameters.Add(item => item.Units, ["r1", "r2"])
            .Add(item => item.Selected, new HashSet<string>()).Add(item => item.OnToggle, value => toggled = value));
        Assert.True(action.Find("#propose-opportunity").HasAttribute("disabled"));
        action.Find(".opportunity-unit[data-unit='r2']").Change(true);
        Assert.Equal(new UnitSelectionList.Toggle("r2", true), toggled);
        action.Render(parameters => parameters.Add(item => item.Selected, new HashSet<string> { "r2" }));
        Assert.False(action.Find("#propose-opportunity").HasAttribute("disabled"));
    }

    // C13 with C14, C15, and N10 kept inside: the MGs-alone and MOL choices follow the selection; the phase decides Snap Shot, Spraying Fire and the Fire Lane.
    [Fact]
    public void TheFirePanelFollowsTheSelectionAndThePhase()
    {
        string? from = null;
        var panel = context.Render<SmallArmsFirePanel>(parameters => parameters.Add(item => item.Side, "german").Add(item => item.Phase, "pfph")
            .Add(item => item.PhaseLabel, "Prep Fire Phase").Add(item => item.Locations, ["bd04:E5:0"]).Add(item => item.Firers, ["g1", "g2"])
            .Add(item => item.SelectedFirers, new HashSet<string> { "g2", "g1" }).Add(item => item.Weapons, [new SmallArmsFirePanel.Weapon("lmg1", "g1")])
            .Add(item => item.SelectedWeapons, new HashSet<string> { "lmg1" }).Add(item => item.Targets, ["bd04:E7:0"]).Add(item => item.MolAllowed, true)
            .Add(item => item.OnFrom, value => from = value));
        Assert.Contains("The german side may fire in the Prep Fire Phase", panel.Find("#fire-side").TextContent, StringComparison.Ordinal);
        Assert.Equal("lmg1 (g1)", panel.Find(".fire-weapon[data-weapon='lmg1']").Parent!.TextContent.Trim());
        Assert.Equal("g1", panel.Find(".fire-alone").GetAttribute("data-unit"));
        Assert.Single(panel.FindAll(".fire-alone"));
        Assert.Equal(["", "g1", "g2"], panel.FindAll("#fire-mol option").Select(option => option.GetAttribute("value")));
        Assert.NotNull(panel.Find("#fire-spray"));
        Assert.Empty(panel.FindAll("#fire-snap-shot"));
        Assert.Empty(panel.FindAll("#fire-lane-weapon"));
        Assert.Empty(panel.FindAll("#fire-partner"));
        Assert.True(panel.Find("#propose-fire").HasAttribute("disabled"));
        panel.Find("#fire-from").Change("bd04:E5:0");
        Assert.Equal("bd04:E5:0", from);

        // A typed Location is a target; the MPh brings Snap Shot and the Fire Lane of a chosen MG; a firing leader his partner.
        panel.Render(parameters => parameters.Add(item => item.FreeTarget, "bd04:E8:0").Add(item => item.Phase, "mph").Add(item => item.MolAllowed, false)
            .Add(item => item.Partners, ["l2"]));
        Assert.False(panel.Find("#propose-fire").HasAttribute("disabled"));
        Assert.NotNull(panel.Find("#fire-snap-shot"));
        Assert.Empty(panel.FindAll("#fire-spray"));
        Assert.Empty(panel.FindAll("#fire-mol"));
        Assert.Equal(["", "lmg1"], panel.FindAll("#fire-lane-weapon option").Select(option => option.GetAttribute("value")));
        Assert.Equal(["", "l2"], panel.FindAll("#fire-partner option").Select(option => option.GetAttribute("value")));
    }

    // Ruling R27.3 (UI and table player review, pass 27): every view reads the OVR's CC as a status; only a view that may act proposes it.
    [Fact]
    public void TheBerserkOverrunsCcIsAnnouncedToEveryViewAndProposedByTheActingOne()
    {
        var proposed = 0;
        var panel = context.Render<BerserkOverrunCloseCombat>(parameters => parameters.Add(item => item.Location, "bd01:A2:0").Add(item => item.Attackers, ["g1"])
            .Add(item => item.Smc, "rl").Add(item => item.SmcAttacks, true).Add(item => item.Ready, false).Add(item => item.OnPropose, () => proposed++));
        Assert.Equal("status", panel.Find("#overrun-cc").GetAttribute("role"));
        Assert.Contains("g1 overran the lone rl in bd01:A2:0", panel.Find("#overrun-cc").TextContent, StringComparison.Ordinal);
        Assert.Contains("rl attacks back", panel.Find("#overrun-cc").TextContent, StringComparison.Ordinal);
        Assert.Empty(panel.FindAll("#propose-overrun-cc"));
        panel.Render(parameters => parameters.Add(item => item.Ready, true).Add(item => item.SmcAttacks, false));
        Assert.Contains("rl cannot attack back", panel.Find("#overrun-cc").TextContent, StringComparison.Ordinal);
        panel.Find("#propose-overrun-cc").Click();
        Assert.Equal(1, proposed);
    }
}
