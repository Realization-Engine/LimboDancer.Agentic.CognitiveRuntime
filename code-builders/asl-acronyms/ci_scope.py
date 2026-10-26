"""Select only acronym checks for isolated feature changes; preserve existing CI for mixed changes."""
import re
import subprocess
import sys

APP = "src/ASL/LimboDancer.Domains.Asl.MapStudio/"
DOCS = "src/ASL/docs/"
SOURCE = {DOCS + "ASL Rulebook Acronym Scan.md", DOCS + "ASL Acronym Expanded-Term Search Ledger.csv"}
NOTES = {DOCS + "ASL Acronym Search " + name + ".md" for name in ("Analysis", "Design", "Implementation Plan")}
PREFIXES = ("code-builders/asl-acronyms/", "src/ASL/tests/ASL.Acronyms.Tests/",
            APP + "Services/Acronyms/", APP + "Components/Acronyms/")
FILES = SOURCE | {APP + "Data/acronyms.catalog.json", APP + "Components/Pages/AcronymSearch.razor"}
SHARED = {APP + "Program.cs", APP + "Components/Layout/StudioNavigation.razor",
          APP + "wwwroot/site.css", APP + "LimboDancer.Domains.Asl.MapStudio.csproj",
          "src/ASL/LimboDancer.Domains.Asl.sln"}

def without_feature(path, text):
    if path.endswith("site.css"):
        prefix, marker, tail = text.partition("/* Acronym Search:")
        # Do not hide unrelated rules appended after this feature's section.
        if marker and all(not line.strip() or line.strip().startswith((".acronym-", "@media (max-width: 1023.98px)", "}"))
                          for line in tail.splitlines()[1:]):
            text = prefix
    elif path.endswith("Program.cs"):
        text = re.sub(r"^\s*builder.Services.AddSingleton<Services.Acronyms.AcronymCatalog>\(services =>\n"
                      r"\s*new\(services.GetRequiredService<ILogger<Services.Acronyms.AcronymCatalog>>\(\)\)\);\n", "", text, flags=re.M)
        text = re.sub(r"^\s*builder.Services.AddSingleton<Services.Acronyms.AcronymSearchService>\(\);\n", "", text, flags=re.M)
    elif path.endswith("StudioNavigation.razor"):
        text = re.sub(r'^\s*new\("reference", "Reference", \["acronyms"\], \[new\("Acronym Search", "acronyms"\)\]\),\n', "", text, flags=re.M)
    elif path.endswith(".csproj"):
        text = re.sub(r'^\s*<EmbeddedResource Include="Data/acronyms.catalog.json"[^\n]*\n', "", text, flags=re.M)
    elif path.endswith(".sln"):
        guids = re.findall(r'Project\("[^"]+"\) = "(?:ASL.Acronyms.Tests|AslAcronyms)", "[^"]+", "([^"]+)"', text)
        text = re.sub(r'Project\("[^"]+"\) = "(?:ASL.Acronyms.Tests|AslAcronyms)",[^\n]+\nEndProject\n', "", text)
        text = "\n".join(line for line in text.splitlines() if not any(guid in line for guid in guids))
    return text.strip()

def select(paths, before, after):
    documentation = bool(paths) and all(path in NOTES for path in paths)
    isolated = bool(paths)
    feature = False
    for path in paths:
        if path in NOTES:
            continue
        if path in FILES or path.startswith(PREFIXES):
            feature = True
        elif path in SHARED:
            isolated &= without_feature(path, before(path)) == without_feature(path, after(path))
            feature = True
        elif path == ".github/workflows/asl-authoring-ci.yml":
            # A workflow edit alone must not suppress the existing jobs.
            isolated &= any(p in FILES or p.startswith(PREFIXES) for p in paths)
        else:
            isolated = False
    return not (isolated or documentation), not documentation and (feature or not isolated)

def git_text(revision, path):
    result = subprocess.run(["git", "show", f"{revision}:{path}"], capture_output=True, check=False)
    return result.stdout.decode("utf-8-sig").replace("\r\n", "\n") if result.returncode == 0 else ""

if __name__ == "__main__":
    base, head = sys.argv[1:3]
    paths = subprocess.check_output(["git", "diff", "--name-only", base, head], text=True).splitlines()
    broad, acronyms = select(paths, lambda p: git_text(base, p), lambda p: git_text(head, p))
    print(f"broad={str(broad).lower()}")
    print(f"acronyms={str(acronyms).lower()}")
