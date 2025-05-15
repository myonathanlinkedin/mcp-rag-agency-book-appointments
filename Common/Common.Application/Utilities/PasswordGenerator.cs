using System;
using System.Linq;

public static class PasswordGenerator
{
    private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
    private const string Numbers = "0123456789";
    private const string AllChars = Uppercase + Lowercase + Numbers;

    private static readonly Random Random = new();

    public static string Generate(int length)
    {
        if (length < 6 || length > 32)
            throw new ArgumentOutOfRangeException(nameof(length), "Password length must be between 6 and 32.");

        // Ensure required characters
        var required = new[]
        {
            Uppercase[Random.Next(Uppercase.Length)],
            Lowercase[Random.Next(Lowercase.Length)],
            Numbers[Random.Next(Numbers.Length)]
        };

        // Fill the rest randomly
        var remaining = Enumerable.Range(0, length - required.Length)
            .Select(_ => AllChars[Random.Next(AllChars.Length)])
            .ToArray();

        // Combine and shuffle
        var password = required.Concat(remaining)
            .OrderBy(_ => Random.Next())
            .ToArray();

        return new string(password);
    }
}
