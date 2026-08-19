namespace Protege.Central.PoC.Core;

public static class SshHelper
{
    public static void OpenSession(string host, string user)
    {
        ProcessLauncher.OpenTerminal(
            AppContext.BaseDirectory,
            $"ssh {user}@{host}",
            $"SSH {user}@{host}",
            track: false);
    }
}
