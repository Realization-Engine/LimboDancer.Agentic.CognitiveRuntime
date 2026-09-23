using System.Security.Cryptography;
using System.Text;

namespace LimboDancer.Domains.Asl.Authoring;

public static class Hashing
{
    public static string Sha256File(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    public static string Sha256Text(string value)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
