# MapStudio's lock file moved to the SDK 10.0.402's Microsoft.AspNetCore.App.Internal.Assets 10.0.13: the one entry, LF kept.
import sys

path = sys.argv[1] + "/src/ASL/LimboDancer.Domains.Asl.MapStudio/packages.lock.json"
with open(path, encoding="utf-8", newline="") as f:
    text = f.read()
assert "\r\n" not in text
old = ('        "requested": "[10.0.12, )",\n'
       '        "resolved": "10.0.12",\n'
       '        "contentHash": "+sIBo9AMBZH46qHWHQdfzcGMt+RuBaPYrUMOAM9VjA7MQ9QtIez3lq1C3E05+Yroa94KKt3DSWaxmCI4H28a9Q=="\n')
new = ('        "requested": "[10.0.13, )",\n'
       '        "resolved": "10.0.13",\n'
       '        "contentHash": "Y2cXnMnqht5fvja2H8kkr5WA2fYItIPwXiBVo83+/9oySuKZCrz4T+yyZRFtLpxxDScvtYDt7e+J0eNElJ+AiA=="\n')
assert text.count(old) == 1
with open(path, "w", encoding="utf-8", newline="") as f:
    f.write(text.replace(old, new))
print("written")
