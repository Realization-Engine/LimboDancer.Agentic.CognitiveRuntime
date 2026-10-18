# svg-memory

The measurements behind `src/ASL/docs/Designs/ASL Map Studio SVG Memory Analysis.md`.

| File | What it does |
|---|---|
| `SvgWriterBench/` | Renders bd01 (from the VASL checkout) and the example counter sets, reports time, allocation, and allocation by type per render, then replays each output through a copy of today's `SvgWriter` and three span-based prototypes and checks that every output is byte-identical. Writes `report.txt` beside the binary. |
| `profile_svg.py` | Splits an SVG file into numbers, attribute names, and attribute values, with its gzip size. |
| `heap_by_type.py` | Totals a `dotnet-gcdump report` by type, folding the size buckets together. |

Build the benchmark with its own output path, so a running Studio is never touched, then run it:

```bash
dotnet build code-builders/diagnostics/svg-memory/SvgWriterBench -c Release -p:BaseOutputPath=<scratch>/svg-bench-out/
```

```bash
dotnet <scratch>/svg-bench-out/Release/net10.0/SvgWriterBench.dll <repository root> <VASL checkout>
```
