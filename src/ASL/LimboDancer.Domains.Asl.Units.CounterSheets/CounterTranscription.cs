using System.Text;

namespace LimboDancer.Domains.Asl.Units.CounterSheets;

/// <summary>
/// One printed value read from a counter (format <c>asl-counter-transcription/1</c>): the sheet, the counter, the
/// face it is printed on (<c>counter</c> for the kind and nationality), the attribute or trait, the value as printed,
/// who transcribed it, and who reviewed it. <see cref="Row"/> is the line number in the file, the header being line 1.
/// </summary>
public sealed record TranscriptionRow(
    int Row,
    string Sheet,
    string Counter,
    string Face,
    string Attribute,
    string Value,
    string Transcriber,
    string Reviewer,
    string Note);

public sealed record CounterTranscriptionResult(IReadOnlyList<TranscriptionRow> Rows, IReadOnlyList<UnitDiagnostic> Diagnostics)
{
    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == UnitDiagnosticSeverity.Error);
}

/// <summary>
/// Reads a counter transcription or worksheet: CSV in UTF-8 with the header
/// <c>sheet,counter,face,attribute,value,transcriber,reviewer,note</c>. Fields may be quoted with double quotes, a
/// doubled quote standing for one; a quoted field may not span lines. Blank lines are ignored.
/// </summary>
public static class CounterTranscription
{
    public const string Format = "asl-counter-transcription/1";

    public static readonly IReadOnlyList<string> Header = ["sheet", "counter", "face", "attribute", "value", "transcriber", "reviewer", "note"];

    /// <summary>The value written for an attribute the counter does not print.</summary>
    public const string NotPrinted = "not-printed";

    /// <summary>The face written for facts about the whole counter: its kind and its nationality.</summary>
    public const string CounterFace = "counter";

    public static CounterTranscriptionResult Read(ReadOnlySpan<byte> csv)
    {
        var diagnostics = new List<UnitDiagnostic>();
        var rows = new List<TranscriptionRow>();
        var lines = Encoding.UTF8.GetString(csv).TrimStart('﻿').ReplaceLineEndings("\n").Split('\n');
        if (lines.Length == 0 || !Split(lines[0], out var header) || !header.SequenceEqual(Header, StringComparer.Ordinal))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-001", $"The first line must be the header {string.Join(',', Header)}.", "line 1"));
            return new CounterTranscriptionResult(rows, diagnostics);
        }

        for (var index = 1; index < lines.Length; index++)
        {
            var line = lines[index];
            var number = index + 1;
            if (line.Trim().Length == 0)
            {
                continue;
            }

            if (!Split(line, out var fields) || fields.Count != Header.Count)
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-001", $"A row has {Header.Count} fields, quoted where they contain a comma.", $"line {number}"));
                continue;
            }

            rows.Add(new TranscriptionRow(number, fields[0].Trim(), fields[1].Trim(), fields[2].Trim(), fields[3].Trim(), fields[4].Trim(),
                fields[5].Trim(), fields[6].Trim(), fields[7].Trim()));
        }

        foreach (var group in rows.GroupBy(row => (row.Counter, row.Face, row.Attribute)).Where(group => group.Count() > 1))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-002",
                $"'{group.Key.Attribute}' on the {group.Key.Face} face of '{group.Key.Counter}' is recorded on more than one line.",
                $"lines {string.Join(", ", group.Select(row => row.Row))}"));
        }

        return new CounterTranscriptionResult(rows, diagnostics);
    }

    private static bool Split(string line, out List<string> fields)
    {
        fields = [];
        var field = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (quoted)
            {
                if (character != '"')
                {
                    field.Append(character);
                }
                else if (index + 1 < line.Length && line[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else
                {
                    quoted = false;
                }
            }
            else if (character == '"' && field.Length == 0)
            {
                quoted = true;
            }
            else if (character == ',')
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else
            {
                field.Append(character);
            }
        }

        fields.Add(field.ToString());
        return !quoted;
    }
}
