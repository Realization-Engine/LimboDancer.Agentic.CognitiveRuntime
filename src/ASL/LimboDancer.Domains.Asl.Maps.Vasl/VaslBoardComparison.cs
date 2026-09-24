using System.Xml;
using System.Xml.Linq;

namespace LimboDancer.Domains.Asl.Maps.Vasl;

/// <summary>
/// Compares one board's source directory with its packaged archive (VASL Board Ingestion Design, section 8.2):
/// LOSData must be byte-identical and BoardMetadata.xml semantically equal. Other entries are not compared.
/// </summary>
public static class VaslBoardComparison
{
    public static IReadOnlyList<MapDiagnostic> Compare(VaslBoardSource directory, VaslBoardSource archive)
    {
        ArgumentNullException.ThrowIfNull(directory);
        ArgumentNullException.ThrowIfNull(archive);
        var diagnostics = new List<MapDiagnostic>();
        var directoryLos = directory.ReadEntry(VaslBoardSource.LosDataEntry);
        var archiveLos = archive.ReadEntry(VaslBoardSource.LosDataEntry);
        if (directoryLos is null || archiveLos is null)
        {
            diagnostics.Add(new MapDiagnostic("VASL-SRC-003", MapDiagnosticSeverity.Error,
                $"LOSData is missing from the {(directoryLos is null ? "source directory" : "archive")} of bd{directory.BoardName}."));
        }
        else if (!directoryLos.AsSpan().SequenceEqual(archiveLos))
        {
            diagnostics.Add(new MapDiagnostic("VASL-SRC-001", MapDiagnosticSeverity.Error,
                $"LOSData differs between the source directory and the archive of bd{directory.BoardName}."));
        }

        var directoryXml = directory.ReadEntry(VaslBoardSource.MetadataEntry);
        var archiveXml = archive.ReadEntry(VaslBoardSource.MetadataEntry);
        if (directoryXml is not null && archiveXml is not null && !SemanticallyEqual(directoryXml, archiveXml))
        {
            diagnostics.Add(new MapDiagnostic("VASL-SRC-002", MapDiagnosticSeverity.Warning,
                $"BoardMetadata.xml differs semantically between the source directory and the archive of bd{directory.BoardName}."));
        }

        return diagnostics;
    }

    /// <summary>XML equality ignoring comments, insignificant whitespace, line endings, and attribute order.</summary>
    public static bool SemanticallyEqual(byte[] left, byte[] right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        try
        {
            return XNode.DeepEquals(Normalize(left), Normalize(right));
        }
        catch (XmlException)
        {
            return false;
        }
    }

    private static XElement Normalize(byte[] xml) =>
        NormalizeElement(VaslXml.Load(xml, out _).Root ?? new XElement("empty"));

    private static XElement NormalizeElement(XElement element) =>
        new(
            element.Name,
            element.Attributes().Where(attribute => !attribute.IsNamespaceDeclaration).OrderBy(attribute => attribute.Name.ToString(), StringComparer.Ordinal)
                .Select(attribute => new XAttribute(attribute.Name, attribute.Value)),
            element.Nodes().Select(node => node switch
            {
                XElement child => (object?)NormalizeElement(child),
                XText text when !string.IsNullOrWhiteSpace(text.Value) => new XText(text.Value.Trim()),
                _ => null,
            }).Where(node => node is not null));
}
