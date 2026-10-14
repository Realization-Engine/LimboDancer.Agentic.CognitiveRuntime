import pathlib

ROOT = pathlib.Path(r"E:\Archive\GitHub\dlandi\pass32-tools")
for name in ["proofs.sh", "chart.sh", "suite.sh", "chart_compare.py", "proof-main/Proof.csproj"]:
    path = ROOT / name
    data = path.read_bytes()
    if data.startswith(b"\xef\xbb\xbf"):
        data = data[3:]
    if name.endswith(".sh"):
        data = data.replace(b"\r\n", b"\n")
    path.write_bytes(data)
    print(name, "crlf" if b"\r\n" in data else "lf", len(data))
