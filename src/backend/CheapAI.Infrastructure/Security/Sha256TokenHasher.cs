using System.Security.Cryptography;
using System.Text;

namespace CheapAI.Infrastructure.Security;

public static class Sha256TokenHasher
{
    public static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
