"""Print the failed tests of a trx file with the start of each message. Usage: python trx_failed.py <file.trx>"""
import sys
import xml.etree.ElementTree as ET

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
root = ET.parse(sys.argv[1]).getroot()
for result in root.iterfind(".//t:UnitTestResult", NS):
    if result.get("outcome") != "Passed":
        message = result.find(".//t:Message", NS)
        stack = result.find(".//t:StackTrace", NS)
        print(result.get("testName"), "|", result.get("outcome"))
        print("  ", (message.text if message is not None else "")[:900])
        print("  ", (stack.text if stack is not None else "")[:400])
