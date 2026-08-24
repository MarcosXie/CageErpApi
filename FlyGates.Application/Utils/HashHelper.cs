namespace FlyGates.Application.Utils;

public static class HashHelper
{
    public static string Hash(string value)
    {
        string hash = BCrypt.Net.BCrypt.HashPassword(value);

        return hash.Replace('/', '-');
    }

    public static bool Verify(string value, string hashedValue)
    {
        string parsedHashedValue = hashedValue.Replace('-', '/');

        return BCrypt.Net.BCrypt.Verify(value, parsedHashedValue);
    }
}
