using System.Text.RegularExpressions;

namespace Protege.Central.PoC.Core;

public enum ApiTarget
{
    Local,
    Dev002,
    Staging,
}

public static class ApiEnvironment
{
    public const string FilePath = @"C:\proj\portal-cliente\frontend\src\services\api.ts";

    private static readonly Dictionary<ApiTarget, string> Urls = new()
    {
        [ApiTarget.Local] = "http://localhost:3333/backend",
        [ApiTarget.Dev002] = "http://10.2.0.99:33052/backend",
        [ApiTarget.Staging] = "https://portal-hml2.protege.com.br/backend",
    };

    public static bool FileExists() => File.Exists(FilePath);

    public static ApiTarget? GetCurrent()
    {
        if (!FileExists()) return null;
        foreach (var line in File.ReadAllLines(FilePath))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("const baseURL")) continue;
            if (trimmed.StartsWith("//")) continue;

            foreach (var (target, url) in Urls)
            {
                if (trimmed.Contains(url)) return target;
            }
        }
        return null;
    }

    public static void SetTarget(ApiTarget target)
    {
        if (!FileExists())
            throw new FileNotFoundException("api.ts nao encontrado.", FilePath);

        var lines = File.ReadAllLines(FilePath);
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (!Regex.IsMatch(trimmed, @"^(//\s*)?const baseURL\s*=\s*'")) continue;

            var matchedTarget = Urls.FirstOrDefault(kv => trimmed.Contains(kv.Value)).Key;
            var isTargetLine = Urls.TryGetValue(target, out var targetUrl) && trimmed.Contains(targetUrl);

            var indent = lines[i][..(lines[i].Length - lines[i].TrimStart().Length)];
            if (isTargetLine)
                lines[i] = $"{indent}const baseURL = '{targetUrl}'";
            else if (Urls.ContainsValue(ExtractUrl(trimmed) ?? ""))
                lines[i] = $"{indent}// const baseURL = '{ExtractUrl(trimmed)}'";
        }

        File.WriteAllLines(FilePath, lines);
    }

    private static string? ExtractUrl(string line)
    {
        var m = Regex.Match(line, @"'([^']+)'");
        return m.Success ? m.Groups[1].Value : null;
    }
}
