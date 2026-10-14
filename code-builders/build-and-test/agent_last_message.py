"""Write the last assistant text of an agent's JSONL transcript to a file: last_message.py <jsonl> <out>"""
import io
import json
import sys

last = None
with io.open(sys.argv[1], encoding="utf-8") as f:
    for line in f:
        try:
            row = json.loads(line)
        except ValueError:
            continue
        message = row.get("message") or {}
        if message.get("role") != "assistant":
            continue
        content = message.get("content")
        if isinstance(content, str):
            text = content
        else:
            text = "\n".join(part.get("text", "") for part in content or [] if isinstance(part, dict) and part.get("type") == "text")
        if text.strip():
            last = text
with io.open(sys.argv[2], "w", encoding="utf-8", newline="\n") as out:
    out.write(last or "")
print(len(last or ""))
