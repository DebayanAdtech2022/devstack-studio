using System.Security.Cryptography;
using System.Text;

namespace DevStackStudio.Generators.Support;

public static class Hashing
{
    public static string Sha256(string contents)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(contents.Replace("\r\n", "\n", StringComparison.Ordinal)));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
