"""The proof tool's digest: properties named in the PROOF_SKIP environment variable (Type.Property, comma-separated) are left out of the dump.

A pass that adds properties to the game state changes every state's digest, which hides whether anything main's state held has changed. With
the pass's new properties left out on the gate's side, the two digests compare what both builds hold. Patches proof/Program.cs only.
"""
path = "E:/Archive/GitHub/dlandi/pass32-tools/proof/Program.cs"
text = open(path, encoding="utf-8", newline="").read()
nl = "\r\n" if "\r\n" in text else "\n"
old = ("        text.Append(type.Name).Append('(');" + nl
       + "        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(item => item.GetIndexParameters().Length == 0 && item.Name != \"EqualityContract\")" + nl)
new = ("        text.Append(type.Name).Append('(');" + nl
       + "        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(item => item.GetIndexParameters().Length == 0 && item.Name != \"EqualityContract\""
       + " && !Skipped.Contains(type.Name + \".\" + item.Name))" + nl)
assert text.count(old) == 1, text.count(old)
text = text.replace(old, new)
old = "internal static class Dump" + nl + "{" + nl
assert text.count(old) == 1
text = text.replace(old, old + "    private static readonly HashSet<string> Skipped = [.. (Environment.GetEnvironmentVariable(\"PROOF_SKIP\") ?? string.Empty)"
                    + ".Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];" + nl + nl)
open(path, "w", encoding="utf-8", newline="").write(text)
print("patched")
