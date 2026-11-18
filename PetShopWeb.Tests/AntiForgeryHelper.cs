using System.Text.RegularExpressions;

namespace PetShopWeb.Tests;

internal static class AntiForgeryHelper
{
    private static readonly Regex TokenRegex = new(
        @"<input[^>]+name=""__RequestVerificationToken""[^>]+value=""([^""]+)""",
        RegexOptions.IgnoreCase);

    private static readonly Regex TokenRegexAltOrder = new(
        @"<input[^>]+value=""([^""]+)""[^>]+name=""__RequestVerificationToken""",
        RegexOptions.IgnoreCase);

    public static string? Extract(string html)
    {
        var match = TokenRegex.Match(html);
        if (!match.Success)
            match = TokenRegexAltOrder.Match(html);
        return match.Success ? match.Groups[1].Value : null;
    }
}
