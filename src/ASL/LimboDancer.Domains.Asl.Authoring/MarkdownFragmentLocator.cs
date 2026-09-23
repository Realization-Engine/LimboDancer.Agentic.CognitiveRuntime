using System.Text;
using System.Text.RegularExpressions;

namespace LimboDancer.Domains.Asl.Authoring;

public static partial class MarkdownFragmentLocator
{
    public static IReadOnlyList<SourceFragment> Locate(
        string sourceId,
        string sourcePath,
        string sourceSha256,
        string? chapter,
        string content)
    {
        if (string.IsNullOrWhiteSpace(sourceId)
            || string.IsNullOrWhiteSpace(sourcePath)
            || sourceSha256.Length != 64)
        {
            throw new ArgumentException("Source identity, path, and SHA-256 are required.");
        }

        var lines = SplitLinesKeepingEndings(content);
        var fragments = new List<SourceFragment>();
        var headingPath = new List<string>();
        int? currentPage = null;
        (string PublishedId, string NormalizedId)? currentRule = null;
        var index = 0;

        while (index < lines.Count)
        {
            var lineText = RemoveLineEnding(lines[index]);
            var pageMatch = PageRegex().Match(lineText);
            if (pageMatch.Success)
            {
                currentPage = int.Parse(pageMatch.Groups["page"].Value, System.Globalization.CultureInfo.InvariantCulture);
                index++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(lineText))
            {
                index++;
                continue;
            }

            var headingMatch = HeadingRegex().Match(lineText);
            if (headingMatch.Success)
            {
                var level = headingMatch.Groups["marks"].Value.Length;
                var title = headingMatch.Groups["title"].Value.Trim();
                var retainedCount = Math.Min(level - 1, headingPath.Count);
                if (retainedCount < headingPath.Count)
                {
                    headingPath.RemoveRange(retainedCount, headingPath.Count - retainedCount);
                }

                headingPath.Add(title);
                currentRule = null;
                fragments.Add(CreateFragment(
                    sourceId,
                    sourcePath,
                    sourceSha256,
                    SourceFragmentKind.Heading,
                    lines[index],
                    index + 1,
                    index + 1,
                    currentPage,
                    headingPath,
                    null,
                    null));
                index++;
                continue;
            }

            if (lineText.StartsWith("```", StringComparison.Ordinal))
            {
                var end = index + 1;
                while (end < lines.Count && !RemoveLineEnding(lines[end]).StartsWith("```", StringComparison.Ordinal))
                {
                    end++;
                }

                if (end < lines.Count)
                {
                    end++;
                }

                var currentPublishedId = currentRule?.PublishedId;
                var currentNormalizedId = currentRule?.NormalizedId;
                fragments.Add(CreateFragment(
                    sourceId,
                    sourcePath,
                    sourceSha256,
                    SourceFragmentKind.StructuredText,
                    string.Concat(lines.Skip(index).Take(end - index)),
                    index + 1,
                    end,
                    currentPage,
                    headingPath,
                    currentPublishedId,
                    currentNormalizedId));
                index = end;
                continue;
            }

            if (FigureRegex().IsMatch(lineText.Trim()))
            {
                var currentPublishedId = currentRule?.PublishedId;
                var currentNormalizedId = currentRule?.NormalizedId;
                fragments.Add(CreateFragment(
                    sourceId,
                    sourcePath,
                    sourceSha256,
                    SourceFragmentKind.FigureReference,
                    lines[index],
                    index + 1,
                    index + 1,
                    currentPage,
                    headingPath,
                    currentPublishedId,
                    currentNormalizedId));
                index++;
                continue;
            }

            var paragraphEnd = index + 1;
            while (paragraphEnd < lines.Count && !IsBoundary(lines[paragraphEnd]))
            {
                paragraphEnd++;
            }

            var raw = string.Concat(lines.Skip(index).Take(paragraphEnd - index));
            var ruleMatch = chapter is null ? Match.Empty : RuleRegex().Match(lineText);
            SourceFragmentKind kind;
            string? publishedId;
            string? normalizedId;
            if (ruleMatch.Success)
            {
                publishedId = ruleMatch.Groups["id"].Value;
                normalizedId = NormalizeRuleId(publishedId, chapter);
                currentRule = (publishedId, normalizedId);
                kind = SourceFragmentKind.RuleText;
            }
            else if (currentRule is not null)
            {
                (publishedId, normalizedId) = currentRule.Value;
                kind = SourceFragmentKind.RuleContinuation;
            }
            else
            {
                publishedId = null;
                normalizedId = null;
                kind = SourceFragmentKind.Paragraph;
            }

            fragments.Add(CreateFragment(
                sourceId,
                sourcePath,
                sourceSha256,
                kind,
                raw,
                index + 1,
                paragraphEnd,
                currentPage,
                headingPath,
                publishedId,
                normalizedId));
            index = paragraphEnd;
        }

        return fragments;
    }

    public static string NormalizeRuleId(string publishedId, string? chapter)
    {
        var value = publishedId.Trim();
        if (PrefixedRuleRegex().IsMatch(value))
        {
            return value.ToUpperInvariant();
        }

        return chapter is null ? value : $"{chapter.ToUpperInvariant()}{value}";
    }

    private static bool IsBoundary(string line)
    {
        var text = RemoveLineEnding(line);
        return string.IsNullOrWhiteSpace(text)
            || PageRegex().IsMatch(text)
            || HeadingRegex().IsMatch(text)
            || text.StartsWith("```", StringComparison.Ordinal)
            || FigureRegex().IsMatch(text.Trim())
            || RuleRegex().IsMatch(text);
    }

    private static SourceFragment CreateFragment(
        string sourceId,
        string sourcePath,
        string sourceSha256,
        SourceFragmentKind kind,
        string content,
        int startLine,
        int endLine,
        int? page,
        IReadOnlyList<string> headingPath,
        string? publishedId,
        string? normalizedId)
    {
        var contentHash = Hashing.Sha256Text(content);
        var identityMaterial = $"{sourceId}\n{sourceSha256}\n{startLine}\n{endLine}\n{contentHash}";
        var fragmentHash = Hashing.Sha256Text(identityMaterial);
        var dependencies = InlineFigureRegex().Matches(content)
            .Cast<Match>()
            .Select(match => match.Groups["path"].Value)
            .ToArray();

        return new SourceFragment(
            $"asl-fragment:sha256:{fragmentHash}",
            sourceId,
            sourcePath,
            sourceSha256,
            kind,
            contentHash,
            content,
            new SourceLocator(
                startLine,
                endLine,
                page,
                page,
                headingPath.ToArray(),
                publishedId,
                normalizedId),
            dependencies,
            FootnoteRegex().IsMatch(content));
    }

    private static List<string> SplitLinesKeepingEndings(string value)
    {
        var lines = new List<string>();
        var start = 0;
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] != '\n')
            {
                continue;
            }

            lines.Add(value[start..(index + 1)]);
            start = index + 1;
        }

        if (start < value.Length)
        {
            lines.Add(value[start..]);
        }

        return lines;
    }

    private static string RemoveLineEnding(string line)
    {
        return line.TrimEnd('\r', '\n');
    }

    [GeneratedRegex(@"^<!--\s*page\s+(?<page>\d+)\s*-->$", RegexOptions.CultureInvariant)]
    private static partial Regex PageRegex();

    [GeneratedRegex(@"^(?<marks>#{1,6})\s+(?<title>.+?)\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"^!\[[^\]]*\]\((?<path>[^)]+)\)$", RegexOptions.CultureInvariant)]
    private static partial Regex FigureRegex();

    [GeneratedRegex(@"!\[[^\]]*\]\((?<path>[^)]+)\)", RegexOptions.CultureInvariant)]
    private static partial Regex InlineFigureRegex();

    [GeneratedRegex(@"^\*\*(?:\\?\*)?(?<id>(?:[A-Z]\.\d+(?:\.\d+)*|\d+\.\d+(?:\.\d+)*))\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RuleRegex();

    [GeneratedRegex(@"^[A-Z](?:\.|\d)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PrefixedRuleRegex();

    [GeneratedRegex(@"<sup>.+?</sup>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FootnoteRegex();
}
