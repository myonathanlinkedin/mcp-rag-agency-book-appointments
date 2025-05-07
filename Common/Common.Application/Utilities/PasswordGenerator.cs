public static class PasswordGenerator
{
    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string Generate(int length)
    {
        var random = new Random();
        var password = new string(Enumerable.Repeat(Chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        return password;
    }
}


