"""Increment 2g: replace two exploratory Play tests (lines 1599 to 1633) with the test of what they found, LF kept."""
import io

PATH = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/LimboDancer.Domains.Asl.Play.Tests/BacklogPass55Tests.cs"
with io.open(PATH, "r", encoding="utf-8", newline="") as handle:
    text = handle.read()
start = text.index("    [Fact]\n    public async Task AGunDuelsFirstShotThatStopsAtAChoiceStillOwesTheDeclaredShot()")
end = text.index("    [Fact]\n    public async Task AConcealedGunsShotIsNotDueled()")
NEW = '''    [Fact]
    public async Task AGunStackedWithInfantryIsOfferedNoDuelWhileTheTanksShotAtThemIsNotReviewed()
    {
        // The vehicle's potential shot is planned as the shot itself would be: a hit's attack on a Gun's crew stacked with other Infantry is
        // not reviewed, so its MA cannot fire at that Location, and the declared shot resolves at once.
        var entered = await TankComesOn(guns: [.. RussianGun(), Squad(DuelHex)]);
        var before = Revision;
        Committed(await DeclareShot(entered, Once(5, 6)));
        Assert.Equal("ru-gun", Assert.Single(Shots(before)).Gun);
        Assert.Null(Current.Choice);
        NoReplayErrors();
    }

'''
text = text[:start] + NEW + text[end:]
with io.open(PATH, "w", encoding="utf-8", newline="") as handle:
    handle.write(text)
print("ok")
