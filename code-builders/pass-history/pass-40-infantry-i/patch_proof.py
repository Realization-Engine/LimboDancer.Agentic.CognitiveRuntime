"""The proof tool's replay: a game the build cannot read (one that holds an event newer than the build) gets one line and is skipped.

Pass 40's games hold the event infantry-overrun-ntc, which main at 3c660afc does not know; its replay stopped on the first of them.
"""
for name in ("proof", "proof-main"):
    path = f"E:/Archive/GitHub/dlandi/pass32-tools/{name}/Program.cs"
    text = open(path, encoding="utf-8", newline="").read()
    nl = "\r\n" if "\r\n" in text else "\n"
    old = ("if (mode == \"replay\")" + nl + "{" + nl + "    foreach (var scope in games)" + nl + "    {" + nl + "        var record = store.Read(scope)!;" + nl)
    new = ("if (mode == \"replay\")" + nl + "{" + nl + "    foreach (var scope in games)" + nl + "    {" + nl
           + "        var record = store.Read(scope);" + nl
           + "        if (record is null)" + nl + "        {" + nl + "            lines.Add($\"{scope.Game}\\tUNREADABLE\");" + nl + "            continue;" + nl + "        }" + nl + nl)
    assert text.count(old) == 1, (name, text.count(old))
    open(path, "w", encoding="utf-8", newline="").write(text.replace(old, new))
    print("patched", name)
