"""Print the failed tests of a trx file: name and the head of the message."""
import os
import sys
import xml.etree.ElementTree as ET

path = sys.argv[1]
if not os.path.exists(path):
    sys.exit("no trx file")
ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
root = ET.parse(path).getroot()
results = root.findall(".//t:UnitTestResult", ns)
failed = [r for r in results if r.get("outcome") != "Passed"]
print(f"results: {len(results)}, not passed: {len(failed)}")
for r in failed[:25]:
    message = r.find(".//t:Message", ns)
    stack = r.find(".//t:StackTrace", ns)
    print("FAILED", r.get("testName"))
    print("   ", (message.text if message is not None else "")[:700].replace("\n", "\n    "))
    if stack is not None and stack.text:
        lines = [line for line in stack.text.split("\n") if "Tests" in line][:2]
        print("   ", " | ".join(line.strip()[-110:] for line in lines))
