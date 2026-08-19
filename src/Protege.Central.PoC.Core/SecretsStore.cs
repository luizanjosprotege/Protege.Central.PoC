namespace Protege.Central.PoC.Core;

public static class SecretsStore
{
    public static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "secrets.txt");

    public static Dictionary<string, string> Load()
    {
        EnsureExists();
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in File.ReadAllLines(FilePath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var idx = line.IndexOf('=');
            if (idx <= 0) continue;
            dict[line[..idx].Trim()] = line[(idx + 1)..].Trim();
        }
        return dict;
    }

    public static bool EnsureExists()
    {
        if (File.Exists(FilePath)) return false;
        File.WriteAllText(FilePath, MachineScan.BuildSecretsTemplate());
        return true;
    }

    public static string Get(this Dictionary<string, string> secrets, string key, string fallback = "")
        => secrets.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : fallback;
}
