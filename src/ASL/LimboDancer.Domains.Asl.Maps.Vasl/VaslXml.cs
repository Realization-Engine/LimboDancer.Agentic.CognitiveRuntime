using System.Text;
using System.Xml.Linq;

namespace LimboDancer.Domains.Asl.Maps.Vasl;

/// <summary>
/// Loads VASL XML files. VASL's parser accepts an XML 1.1 declaration (board 23 has one), which .NET's does not;
/// such a declaration is read as XML 1.0. Content that XML 1.0 genuinely forbids still fails to parse.
/// </summary>
internal static class VaslXml
{
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    public static XDocument Load(byte[] bytes, out bool declaredXml11)
    {
        var start = bytes.AsSpan().StartsWith(Utf8Bom) ? Utf8Bom.Length : 0;
        var content = bytes;
        declaredXml11 = false;
        foreach (var declaration in new[] { "<?xml version=\"1.1\"", "<?xml version='1.1'" })
        {
            var prefix = Encoding.ASCII.GetBytes(declaration);
            if (bytes.AsSpan(start).StartsWith(prefix))
            {
                content = (byte[])bytes.Clone();
                content[start + prefix.Length - 2] = (byte)'0';
                declaredXml11 = true;
                break;
            }
        }

        using var stream = new MemoryStream(content);
        return XDocument.Load(stream, LoadOptions.None);
    }
}
