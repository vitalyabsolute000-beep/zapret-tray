namespace ZapretTrayApp;

internal static class StrategyDisplay
{
    public static string FormatName(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        if (name.Equals("general", StringComparison.OrdinalIgnoreCase))
        {
            return "General";
        }

        return name.StartsWith("general ", StringComparison.OrdinalIgnoreCase)
            ? name["general ".Length..].Trim().Trim('(', ')')
            : name;
    }
}
