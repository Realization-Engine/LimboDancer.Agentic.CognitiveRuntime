"""Add DefinitionReference.Catalog to the both-sides skip of the three pass 55 proof scripts."""
import io

OLD = 'PROOF_SKIP="GameState.Catalog${'
NEW = 'PROOF_SKIP="GameState.Catalog,DefinitionReference.Catalog${'
for name in ("base.sh", "replay_now.sh", "dump_state.sh"):
    path = "E:/Archive/GitHub/dlandi/pass32-tools/pass55/" + name
    text = io.open(path, encoding="utf-8", newline="").read()
    assert text.count(OLD) == 1, (name, text.count(OLD))
    io.open(path, "w", encoding="utf-8", newline="").write(text.replace(OLD, NEW))
    print("fixed", name)
