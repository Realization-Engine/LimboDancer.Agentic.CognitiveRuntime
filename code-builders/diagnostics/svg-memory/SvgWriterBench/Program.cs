using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Text;
using System.Xml;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Rendering;
using LimboDancer.Domains.Asl.Maps.Vasl;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.Rendering;
using LimboDancer.Domains.Asl.Units.Rendering.Palettes;
using LimboDancer.Domains.Asl.Units.Vocabulary;

// Measures the SVG writer layer (option G of the SVG memory analysis) without changing the product: real renders for the baseline and
// the allocation profile, then a replay of their output through a copy of today's writer and through a span-based prototype.
// Arguments: [repository root] [VASL checkout]; the VASL checkout falls back to AslMaps__VaslRoot, as the tests' does.
var repo = args.Length > 0 ? args[0] : @"E:\Archive\GitHub\dlandi\LimboDancer.MCP";
var vaslRoot = args.Length > 1 ? args[1] : Environment.GetEnvironmentVariable(VaslSource.EnvironmentVariable) ?? @"E:\Archive\GitHub\dlandi\vasl";

var vasl = VaslSource.TryOpen(vaslRoot) ?? throw new InvalidOperationException("No VASL checkout.");
var catalog = vasl.ReadTerrainCatalog().Catalog!;
var board = VaslBoardImporter.Import(vasl, VaslBoardSource.SourceDirectory(vasl, "01"), catalog).Board!;
var facts = HexFactFidelity.Derive(board, catalog);
var input = BoardRenderInput.Create(board.Board, "bd01", board.Grid, catalog, facts);

var vocabulary = UnitVocabulary.Asl();
var sheet = UnitStyles.Load(UnitStyles.Digital);
var renderer = new UnitRenderer(vocabulary, sheet, PaletteSetReader.Embedded(sheet.Palette!));
var target = UnitMapTarget.ForBoard(BoardRef.Parse("bd01"), BoardGeometry.StandardGeomorphic);
UnitPlacementSet Set(string file) =>
    UnitPlacementSetReader.Read(File.ReadAllBytes(Path.Combine(repo, "src", "ASL", "units", "examples", file)), vocabulary).Set!;
var figures = Set("bd01-infantry-figures.units.json");
var demo = Set("bd01-demo.units.json");

var workloads = new (string Name, int Runs, Func<string> Render)[]
{
    ("bd01 exact document", 20, () => BoardRenderer.Document(input, BoardView.Exact)),
    ("bd01 hexfacts document", 20, () => BoardRenderer.Document(input, BoardView.HexFacts)),
    ($"counter layer, figures set ({figures.Units.Count} counters)", 40, () => UnitOverlayBuilder.Build(target, figures.SetId, figures.Units, renderer).Svg),
    ($"counter layer, demo set ({demo.Units.Count} counters)", 40, () => UnitOverlayBuilder.Build(target, demo.SetId, demo.Units, renderer).Svg),
};

using var listener = new AllocationListener();
var report = new StringBuilder();
foreach (var (name, runs, render) in workloads)
{
    var svg = render();
    for (var warm = 0; warm < 5; warm++)
    {
        render();
    }

    GC.Collect();
    listener.Reset();
    listener.On = true;
    var before = GC.GetAllocatedBytesForCurrentThread();
    var timer = Stopwatch.StartNew();
    for (var run = 0; run < runs; run++)
    {
        render();
    }

    timer.Stop();
    listener.On = false;
    var allocated = (GC.GetAllocatedBytesForCurrentThread() - before) / runs;
    report.AppendLine($"## {name}");
    report.AppendLine($"output {svg.Length:N0} chars ({svg.Length * 2:N0} bytes as a string); render {timer.Elapsed.TotalMilliseconds / runs:F2} ms; allocated {allocated:N0} bytes per render ({allocated / (double)(svg.Length * 2):F1}x the output string)");
    report.AppendLine("allocation by type (sampled every ~100 KB):");
    foreach (var (type, bytes) in listener.Top(10))
    {
        report.AppendLine($"  {bytes / runs,14:N0}  {type}");
    }

    var replay = Replay.Parse(svg);
    report.AppendLine($"replay: {replay.Elements:N0} elements, {replay.Attributes:N0} attributes, {replay.Numbers:N0} fixed-point numbers, {replay.PathNumbers:N0} of them in paths");
    foreach (var (writerName, write) in new (string, Func<Replay, string>)[]
             {
                 ("today's writer (copy)", r => r.Write(new OldWriter())),
                 ("today's API, span escaping", r => r.Write(new OldWriter(spanEscaping: true))),
                 ("span writer, StringBuilder", r => r.Write(new SpanWriter(pooled: false))),
                 ("span writer, pooled buffer", r => r.Write(new SpanWriter(pooled: true))),
             })
    {
        var output = write(replay);
        for (var warm = 0; warm < 5; warm++)
        {
            write(replay);
        }

        GC.Collect();
        var b = GC.GetAllocatedBytesForCurrentThread();
        var t = Stopwatch.StartNew();
        for (var run = 0; run < runs; run++)
        {
            write(replay);
        }

        t.Stop();
        var a = (GC.GetAllocatedBytesForCurrentThread() - b) / runs;
        report.AppendLine($"  {writerName,-28} {t.Elapsed.TotalMilliseconds / runs,8:F2} ms {a,14:N0} bytes  identical: {output == svg}");
    }

    report.AppendLine();
}

var numbers = Replay.Parse(BoardRenderer.Document(input, BoardView.Exact)).AllRaw;
report.AppendLine($"## number formatting alone ({numbers.Count:N0} bd01 values)");
foreach (var (label, format) in new (string, Action<StringBuilder, int>)[]
         {
             ("FixedPoint.ToString then Append", (sb, raw) => sb.Append(FixedPoint.FromRaw(raw).ToString())),
             ("TryFormat into a span", (sb, raw) => SpanWriter.AppendFixed(sb, raw)),
         })
{
    var sb = new StringBuilder(1 << 20);
    foreach (var raw in numbers)
    {
        format(sb, raw);
    }

    var text = sb.ToString();
    GC.Collect();
    var b = GC.GetAllocatedBytesForCurrentThread();
    var t = Stopwatch.StartNew();
    for (var run = 0; run < 50; run++)
    {
        sb.Clear();
        foreach (var raw in numbers)
        {
            format(sb, raw);
        }
    }

    t.Stop();
    report.AppendLine($"  {label,-34} {t.Elapsed.TotalMilliseconds / 50,7:F3} ms {(GC.GetAllocatedBytesForCurrentThread() - b) / 50,12:N0} bytes  ({text.Length:N0} chars)");
}

var mismatch = numbers.FirstOrDefault(raw => FixedPoint.FromRaw(raw).ToString() != SpanWriter.FormatFixed(raw));
report.AppendLine($"  formats agree on every value: {numbers.All(raw => FixedPoint.FromRaw(raw).ToString() == SpanWriter.FormatFixed(raw))} (first mismatch raw {mismatch})");
var edge = new[] { 0, 1, -1, 63, -63, 64, -64, 65, 32, -32, int.MaxValue, int.MinValue + 1, 1_000_000, -999_999 };
report.AppendLine($"  edge values agree: {edge.All(raw => FixedPoint.FromRaw(raw).ToString() == SpanWriter.FormatFixed(raw))}");

Console.Write(report);
File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "report.txt"), report.ToString());

/// <summary>GC allocation ticks by type name, from the runtime's own event source.</summary>
internal sealed class AllocationListener : EventListener
{
    private readonly Dictionary<string, long> bytes = new(StringComparer.Ordinal);

    public bool On { get; set; }

    public void Reset() => bytes.Clear();

    public IEnumerable<(string Type, long Bytes)> Top(int count) =>
        bytes.OrderByDescending(pair => pair.Value).Take(count).Select(pair => (pair.Key, pair.Value));

    protected override void OnEventSourceCreated(EventSource source)
    {
        if (source.Name == "Microsoft-Windows-DotNETRuntime")
        {
            EnableEvents(source, EventLevel.Verbose, (EventKeywords)0x1);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs data)
    {
        if (!On || data.EventName is null || !data.EventName.StartsWith("GCAllocationTick", StringComparison.Ordinal) || data.PayloadNames is null)
        {
            return;
        }

        var type = data.Payload![data.PayloadNames.IndexOf("TypeName")] as string ?? "?";
        var amountIndex = data.PayloadNames.IndexOf("AllocationAmount64");
        var amount = amountIndex >= 0 ? Convert.ToInt64(data.Payload[amountIndex], CultureInfo.InvariantCulture) : 100_000;
        lock (bytes)
        {
            bytes[type] = bytes.GetValueOrDefault(type) + amount;
        }
    }
}

/// <summary>The output of a render as the writer calls that produced it, with numbers and path data as the renderer would hand them over.</summary>
internal sealed class Replay
{
    private readonly List<Op> ops = [];

    public int Elements { get; private set; }

    public int Attributes { get; private set; }

    public int Numbers { get; private set; }

    public int PathNumbers { get; private set; }

    public List<int> AllRaw { get; } = [];

    public static Replay Parse(string svg)
    {
        var replay = new Replay();
        using var reader = XmlReader.Create(new StringReader(svg), new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, IgnoreWhitespace = false });
        while (reader.Read())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    var name = reader.Name;
                    var empty = reader.IsEmptyElement;
                    var attributes = new List<Value>();
                    var isFigure = name == "path" && reader.GetAttribute("id")?.StartsWith("fig-", StringComparison.Ordinal) == true;
                    for (var more = reader.MoveToFirstAttribute(); more; more = reader.MoveToNextAttribute())
                    {
                        attributes.Add(replay.ValueOf(reader.Name, reader.Value, isFigure));
                    }

                    reader.MoveToElement();
                    replay.Elements++;
                    replay.Attributes += attributes.Count;
                    replay.ops.Add(new Op(empty ? OpKind.Empty : OpKind.Start, name, [.. attributes], null));
                    break;
                case XmlNodeType.Text or XmlNodeType.Whitespace or XmlNodeType.SignificantWhitespace:
                    replay.ops.Add(new Op(OpKind.Text, null, [], reader.Value));
                    break;
                case XmlNodeType.EndElement:
                    replay.ops.Add(new Op(OpKind.End, null, [], null));
                    break;
            }
        }

        return replay;
    }

    // A value that is an exact 1/64 pixel number is handed over as a number; path data as commands and numbers, except the figure
    // outlines, which come from the figure file as finished strings; everything else as text.
    private Value ValueOf(string name, string text, bool figure)
    {
        if (name == "d" && !figure && PathText.TryParse(text, out var path))
        {
            Numbers += path.Count(part => part.Raw is not null);
            PathNumbers += path.Count(part => part.Raw is not null);
            AllRaw.AddRange(path.Where(part => part.Raw is not null).Select(part => part.Raw!.Value));
            return new Value(name, null, null, path, figure);
        }

        if (TryRaw(text, out var raw))
        {
            Numbers++;
            AllRaw.Add(raw);
            return new Value(name, null, raw, null, false);
        }

        return new Value(name, text, null, null, figure && name == "d");
    }

    public static bool TryRaw(string text, out int raw)
    {
        raw = 0;
        if (text.Length == 0 || text.Length > 16 || !decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        var scaled = value * 64;
        if (scaled != decimal.Truncate(scaled) || scaled > int.MaxValue || scaled < int.MinValue + 1)
        {
            return false;
        }

        raw = (int)scaled;
        return FixedPoint.FromRaw(raw).ToString() == text;
    }

    public string Write(IWriter writer)
    {
        foreach (var op in ops)
        {
            switch (op.Kind)
            {
                case OpKind.Start:
                    writer.Start(op.Name!, op.Attributes);
                    break;
                case OpKind.Empty:
                    writer.Empty(op.Name!, op.Attributes);
                    break;
                case OpKind.Text:
                    writer.Text(op.Text!);
                    break;
                default:
                    writer.End();
                    break;
            }
        }

        return writer.Finish();
    }

    private enum OpKind { Start, Empty, Text, End }

    private sealed record Op(OpKind Kind, string? Name, Value[] Attributes, string? Text);
}

/// <summary>An attribute as the renderer has it: text, a fixed-point number, path parts, or a pre-checked figure outline.</summary>
internal sealed record Value(string Name, string? Text, int? Raw, List<PathPart>? Path, bool Trusted);

internal sealed record PathPart(char? Command, int? Raw);

internal static class PathText
{
    public static bool TryParse(string d, out List<PathPart> parts)
    {
        parts = [];
        var index = 0;
        while (index < d.Length)
        {
            var c = d[index];
            if (char.IsAsciiLetter(c))
            {
                parts.Add(new PathPart(c, null));
                index++;
                continue;
            }

            if (c == ' ')
            {
                parts.Add(new PathPart(' ', null));
                index++;
                continue;
            }

            var start = index;
            while (index < d.Length && (char.IsAsciiDigit(d[index]) || d[index] is '.' or '-') && (index == start || d[index] != '-'))
            {
                index++;
            }

            if (index == start || !Replay.TryRaw(d[start..index], out var raw))
            {
                return false;
            }

            parts.Add(new PathPart(null, raw));
        }

        return true;
    }

    public static string Join(List<PathPart> parts)
    {
        var builder = new StringBuilder();
        foreach (var part in parts)
        {
            if (part.Raw is { } raw)
            {
                builder.Append(FixedPoint.FromRaw(raw).ToString());
            }
            else
            {
                builder.Append(part.Command!.Value);
            }
        }

        return builder.ToString();
    }
}

internal interface IWriter
{
    void Start(string name, Value[] attributes);

    void Empty(string name, Value[] attributes);

    void Text(string text);

    void End();

    string Finish();
}

/// <summary>Today's SvgWriter and PathData, as they are: numbers through FixedPoint.ToString, paths through a PathData builder, escaping by character.</summary>
internal sealed class OldWriter(bool spanEscaping = false) : IWriter
{
    private static readonly SearchValues<char> AttributeSpecials = SearchValues.Create("&<\"");
    private static readonly SearchValues<char> TextSpecials = SearchValues.Create("&<>");
    private readonly StringBuilder builder = new();
    private readonly Stack<string> open = new();

    // The smallest change: the same string API and number text, only the escape loop appends spans between the characters that need it.
    private void Escaped(ReadOnlySpan<char> text, SearchValues<char> specials)
    {
        while (true)
        {
            var at = text.IndexOfAny(specials);
            if (at < 0)
            {
                builder.Append(text);
                return;
            }

            builder.Append(text[..at]).Append(text[at] switch { '&' => "&amp;", '<' => "&lt;", '>' => "&gt;", _ => "&quot;" });
            text = text[(at + 1)..];
        }
    }

    public void Start(string name, Value[] attributes)
    {
        WriteTag(name, Convert(attributes));
        builder.Append('>');
        open.Push(name);
    }

    public void Empty(string name, Value[] attributes)
    {
        WriteTag(name, Convert(attributes));
        builder.Append("/>");
    }

    public void Text(string text)
    {
        if (spanEscaping)
        {
            Escaped(text, TextSpecials);
            return;
        }

        foreach (var character in text)
        {
            builder.Append(character switch { '&' => "&amp;", '<' => "&lt;", '>' => "&gt;", _ => character.ToString() });
        }
    }

    public void End() => builder.Append("</").Append(open.Pop()).Append('>');

    public string Finish() => builder.ToString();

    // The renderer builds the (name, value) tuples first, numbers as strings and path data through PathData, then passes a params array.
    private static (string Name, string Value)[] Convert(Value[] attributes)
    {
        var result = new (string, string)[attributes.Length];
        for (var index = 0; index < attributes.Length; index++)
        {
            var value = attributes[index];
            result[index] = (value.Name, value.Raw is { } raw ? FixedPoint.FromRaw(raw).ToString() : value.Path is { } path ? PathText.Join(path) : value.Text!);
        }

        return result;
    }

    private void WriteTag(string name, (string Name, string Value)[] attributes)
    {
        builder.Append('<').Append(name);
        foreach (var (attributeName, value) in attributes)
        {
            builder.Append(' ').Append(attributeName).Append("=\"");
            if (spanEscaping)
            {
                Escaped(value, AttributeSpecials);
                builder.Append('"');
                continue;
            }

            foreach (var character in value)
            {
                builder.Append(character switch { '&' => "&amp;", '<' => "&lt;", '"' => "&quot;", _ => character.ToString() });
            }

            builder.Append('"');
        }
    }
}

/// <summary>
/// The prototype: escaping by span between the characters that need it (SearchValues), numbers formatted straight into the output,
/// path data written straight into the attribute, trusted outlines copied unescaped, and optionally a pooled char buffer instead of
/// StringBuilder chunks.
/// </summary>
internal sealed class SpanWriter(bool pooled) : IWriter
{
    private static readonly SearchValues<char> AttributeSpecials = SearchValues.Create("&<\"");
    private static readonly SearchValues<char> TextSpecials = SearchValues.Create("&<>");

    private readonly StringBuilder builder = new();
    private readonly Stack<string> open = new();
    private char[] buffer = pooled ? ArrayPool<char>.Shared.Rent(1 << 16) : [];
    private int length;

    public void Start(string name, Value[] attributes)
    {
        WriteTag(name, attributes);
        Append('>');
        open.Push(name);
    }

    public void Empty(string name, Value[] attributes)
    {
        WriteTag(name, attributes);
        Append("/>");
    }

    public void Text(string text) => Escaped(text, TextSpecials);

    public void End()
    {
        Append("</");
        Append(open.Pop());
        Append('>');
    }

    public string Finish()
    {
        if (!pooled)
        {
            return builder.ToString();
        }

        var result = new string(buffer, 0, length);
        ArrayPool<char>.Shared.Return(buffer);
        buffer = [];
        return result;
    }

    private void WriteTag(string name, Value[] attributes)
    {
        Append('<');
        Append(name);
        foreach (var value in attributes)
        {
            Append(' ');
            Append(value.Name);
            Append("=\"");
            if (value.Raw is { } raw)
            {
                Fixed(raw);
            }
            else if (value.Path is { } path)
            {
                foreach (var part in path)
                {
                    if (part.Raw is { } number)
                    {
                        Fixed(number);
                    }
                    else
                    {
                        Append(part.Command!.Value);
                    }
                }
            }
            else if (value.Trusted)
            {
                Append(value.Text!);
            }
            else
            {
                Escaped(value.Text!, AttributeSpecials);
            }

            Append('"');
        }
    }

    private void Escaped(ReadOnlySpan<char> text, SearchValues<char> specials)
    {
        while (true)
        {
            var at = text.IndexOfAny(specials);
            if (at < 0)
            {
                Append(text);
                return;
            }

            Append(text[..at]);
            Append(text[at] switch { '&' => "&amp;", '<' => "&lt;", '>' => "&gt;", _ => "&quot;" });
            text = text[(at + 1)..];
        }
    }

    private void Fixed(int raw)
    {
        Span<char> scratch = stackalloc char[24];
        var written = WriteFixed(scratch, raw);
        Append(scratch[..written]);
    }

    private void Append(char character)
    {
        if (!pooled)
        {
            builder.Append(character);
            return;
        }

        Ensure(1);
        buffer[length++] = character;
    }

    private void Append(ReadOnlySpan<char> text)
    {
        if (!pooled)
        {
            builder.Append(text);
            return;
        }

        Ensure(text.Length);
        text.CopyTo(buffer.AsSpan(length));
        length += text.Length;
    }

    private void Ensure(int more)
    {
        if (length + more <= buffer.Length)
        {
            return;
        }

        var larger = ArrayPool<char>.Shared.Rent(Math.Max(buffer.Length * 2, length + more));
        buffer.AsSpan(0, length).CopyTo(larger);
        ArrayPool<char>.Shared.Return(buffer);
        buffer = larger;
    }

    /// <summary>FixedPoint.ToString's text, written into a span: sign, whole pixels, and the 1/64 remainder as up to six decimals without trailing zeros.</summary>
    public static int WriteFixed(Span<char> destination, int raw)
    {
        var magnitude = Math.Abs((long)raw);
        var whole = magnitude / FixedPoint.UnitsPerPixel;
        var remainder = magnitude % FixedPoint.UnitsPerPixel;
        var at = 0;
        if (raw < 0)
        {
            destination[at++] = '-';
        }

        whole.TryFormat(destination[at..], out var digits, default, CultureInfo.InvariantCulture);
        at += digits;
        if (remainder == 0)
        {
            return at;
        }

        destination[at++] = '.';
        // remainder / 64 = remainder * 15625 / 1000000: six digits, trailing zeros dropped.
        var fraction = remainder * 15625;
        Span<char> six = stackalloc char[6];
        for (var place = 5; place >= 0; place--)
        {
            six[place] = (char)('0' + (fraction % 10));
            fraction /= 10;
        }

        var end = 6;
        while (six[end - 1] == '0')
        {
            end--;
        }

        six[..end].CopyTo(destination[at..]);
        return at + end;
    }

    public static void AppendFixed(StringBuilder builder, int raw)
    {
        Span<char> scratch = stackalloc char[24];
        builder.Append(scratch[..WriteFixed(scratch, raw)]);
    }

    public static string FormatFixed(int raw)
    {
        Span<char> scratch = stackalloc char[24];
        return new string(scratch[..WriteFixed(scratch, raw)]);
    }
}
