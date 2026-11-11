using System.Security.Cryptography;

namespace DevStackStudio.Core.Security;

public static class CredentialGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%^&*-_";

    public static string GeneratePassword(int length = 24)
    {
        if (length < 16)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Generated passwords must be at least 16 characters.");
        }

        Span<byte> bytes = stackalloc byte[length];
        RandomNumberGenerator.Fill(bytes);
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = Alphabet[bytes[i] % Alphabet.Length];
        }

        return new string(chars);
    }
}
