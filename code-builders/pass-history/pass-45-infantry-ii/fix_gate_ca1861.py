"""Pass 45, the gate: CA1861 in two Play test files. The constant arrays become static readonly fields, as the files' others are."""
import sys

T = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/LimboDancer.Domains.Asl.Play.Tests/"
EDITS = {
    "BacklogPass8Tests.cs": [
        ('            ["firers"] = new[] { "r1" },\n            ["weapons"] = new Dictionary<string, string[]> { ["r1"] = ["ru-mmg"] },\n            ["withoutInherent"] = new[] { "r1" },\n',
         '            ["firers"] = R1,\n            ["weapons"] = new Dictionary<string, string[]> { ["r1"] = ["ru-mmg"] },\n            ["withoutInherent"] = R1,\n', 1),
    ],
    "CloseCombatStepsTests.cs": [
        ('    private static readonly string[] R4 = ["r4"];\n', '    private static readonly string[] R4 = ["r4"];\n    private static readonly string[] Rl = ["rl"];\n', 1),
        ('            firers = R4,\n            directors = new[] { "rl" },\n', '            firers = R4,\n            directors = Rl,\n', 2),
    ],
}
texts = {}
for name, edits in EDITS.items():
    text = open(T + name, encoding="utf-8", newline="").read()
    crlf = "\r\n" in text
    text = text.replace("\r\n", "\n")
    for old, new, count in edits:
        if text.count(old) != count:
            sys.exit(f"{name}: found {text.count(old)}, expected {count}: {old[:70]!r}")
        text = text.replace(old, new)
    for field in ("R1 = ", "Rl = "):
        if text.count("string[] " + field) > 1:
            sys.exit(f"{name}: {field} declared twice")
    texts[name] = text.replace("\n", "\r\n") if crlf else text
for name, text in texts.items():
    open(T + name, "w", encoding="utf-8", newline="").write(text)
    print("written", name)
