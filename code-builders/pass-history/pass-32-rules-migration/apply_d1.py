import pathlib

ROOT = pathlib.Path(r"E:\Archive\GitHub\dlandi\LimboDancer.MCP\src\ASL")
EDITS = [
    ("LimboDancer.Domains.Asl.Rules/ScenarioA1OrdnanceEligibility.cs",
     "    /// (FRD) in 1944, and twice as many in 1945, from its half-squads at setup.\n    /// </summary>\n    public static int PanzerfaustAllowance(int halfSquads, int scenarioYear) => scenarioYear switch",
     "    /// (FRD) in 1944, and twice as many in 1945, from its half-squads at setup; a scenario with no year counts as before 1944.\n    /// </summary>\n    public static int PanzerfaustAllowance(int halfSquads, int? scenarioYear) => scenarioYear switch"),
    ("LimboDancer.Domains.Asl.Rules/ScenarioA1OrdnanceEligibility.cs",
     "    /// <summary>Whether a Gun fires at its Bore Sighted Location with its original crew from its setup Location (C6.43); the sightings as texts.</summary>\n    public static bool BoreSighted(IEnumerable<(string Gun, string Location, string Crew, string SetupLocation)> sightings, string gun, string crew, string from, string target)\n    {\n        ArgumentNullException.ThrowIfNull(sightings);\n        return sightings.Any(item => item.Gun == gun && item.Location == target && item.Crew == crew && item.SetupLocation == from);\n    }",
     "    /// <summary>Whether a Gun fires at its Bore Sighted Location with its original crew from its setup Location (C6.43); Locations compare by their own equality.</summary>\n    public static bool BoreSighted<TLocation>(IEnumerable<(string Gun, TLocation Location, string Crew, TLocation SetupLocation)> sightings, string gun, string crew, TLocation from, TLocation target)\n    {\n        ArgumentNullException.ThrowIfNull(sightings);\n        var equality = EqualityComparer<TLocation>.Default;\n        return sightings.Any(item => item.Gun == gun && equality.Equals(item.Location, target) && item.Crew == crew && equality.Equals(item.SetupLocation, from));\n    }"),
    ("LimboDancer.Domains.Asl.Play/LiveOrdnance.cs",
     "        return ScenarioA1OrdnanceEligibility.BoreSighted(state.BoreSights.Select(item => (item.Gun, item.Location.ToString(), item.Crew, item.SetupLocation.ToString())),\n            gun, crew, from.ToString(), target.ToString());",
     "        return ScenarioA1OrdnanceEligibility.BoreSighted(state.BoreSights.Select(item => (item.Gun, item.Location, item.Crew, item.SetupLocation)), gun, crew, from, target);"),
]
for name, old, new in EDITS:
    path = ROOT / name
    raw = path.read_bytes()
    bom = raw.startswith(b"\xef\xbb\xbf")
    text = raw.decode("utf-8-sig")
    crlf = "\r\n" in text
    norm = text.replace("\r\n", "\n")
    count = norm.count(old)
    print(name, "matches", count)
    if count != 1:
        continue
    norm = norm.replace(old, new)
    if crlf:
        norm = norm.replace("\n", "\r\n")
    path.write_bytes((b"\xef\xbb\xbf" if bom else b"") + norm.encode("utf-8"))
