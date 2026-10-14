"""Pass 40 task 40.4: the words the first games showed missing or stale. Exact blocks, asserted once each."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"


def apply(path, edits):
    text = open(ROOT + path, encoding="utf-8", newline="").read()
    for old, new in edits:
        assert text.count(old) == 1, (path, old[:80], text.count(old))
        text = text.replace(old, new)
    open(ROOT + path, "w", encoding="utf-8", newline="").write(text)


apply("LimboDancer.Domains.Asl.Rules/ScenarioA1MovementCalculator.cs", [
    ("\"play.move-occupied: Infantry may not enter a Location holding a Known enemy unit in the MPh; Infantry OVR outside the reviewed building case is not built (A4.14, A4.15; ruling R10.11)\"",
     "\"play.move-occupied: Infantry may not enter a Location holding a Known enemy unit in the MPh, but by an Infantry OVR of a lone SMC or to recapture Unarmed units (A4.14, A4.15, A20.54; ruling R10.11)\""),
])
apply("LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor", [
    ("        ? [.. choice.Options.Select(option => new PendingChoicePanel.Option(option, ChoiceOptionText(choice, option)))]",
     "        ? [.. choice.Options.Select(option => new PendingChoicePanel.Option(option,\n"
     "            choice.Kind == ChoicePending.OverrunEvade ? $\"into {PlaceOf(option)}\" : ChoiceOptionText(choice, option)))]"),
])
apply("LimboDancer.Domains.Asl.MapStudio/Services/PlayRecords.cs", [
    ("                else if (item.Payload is RepairAttempted repair)\n",
     "                else if (item.Payload is InfantryOverrunNtc ntc && Open(before, ntc.Taker))\n"
     "                {\n"
     "                    // Pass 40 (A4.15): the NTC of an Infantry OVR, with its DR and result.\n"
     "                    var dr = rolls.TryGetValue(ntc.Roll, out var roll) ? roll.Values.Sum().ToString(CultureInfo.InvariantCulture) : \"?\";\n"
     "                    records.Add((item.EventId, \"overrun-ntc\", $\"{ntc.Taker}{In(before, ntc.Taker)} takes the NTC for an Infantry OVR of {ntc.Target}: DR {dr} \"\n"
     "                        + $\"{ntc.Drm:+0;-0;+0} against morale {ntc.Morale}: {(ntc.Passed ? \"passed\" : \"failed\")} (A4.15)\"));\n"
     "                }\n"
     "                else if (item.Payload is RepairAttempted repair)\n"),
])
apply("LimboDancer.Domains.Asl.MapStudio/Services/ReplaySteps.cs", [
    ("        if (First<RecoveryAttempted>(payloads) is { } recovery)\n",
     "        if (First<InfantryOverrunNtc>(payloads) is { } ntc)\n"
     "        {\n"
     "            return (\"move\", SideOf(ntc.Taker), $\"{A(SideOf(ntc.Taker), \"unit\")} takes the NTC for an Infantry OVR: {(ntc.Passed ? \"passed\" : \"failed\")}\", false);\n"
     "        }\n\n"
     "        if (First<RecoveryAttempted>(payloads) is { } recovery)\n"),
])
print("ok")
