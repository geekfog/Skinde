namespace Skinde.Utility;

public static class StringExtension
{
    public static string GetInitials(this string value)
        => String.Concat(value
            .Split([' '], StringSplitOptions.RemoveEmptyEntries)
            .Where(x => x.Length >= 1 && Char.IsLetter(x[0]))
            .Select(x => Char.ToUpper(x[0])));
}