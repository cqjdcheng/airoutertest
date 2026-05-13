using System.Security.Cryptography;
using System.Text;

namespace CheapAI.Application.Common.Security;

public static class TokenHashing
{
    public static string Sha256(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
