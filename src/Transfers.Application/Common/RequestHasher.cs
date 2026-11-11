using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Transfers.Application.Common;

public static class RequestHasher
{
    public static string Hash(params object?[] parts)
    {
        var normalized = string.Join('|', parts.Select(part => part switch
        {
            null => "",
            decimal value => value.ToString("0.00", CultureInfo.InvariantCulture),
            DateTimeOffset value => value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            _ => Convert.ToString(part, CultureInfo.InvariantCulture)
        }));

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }
}
