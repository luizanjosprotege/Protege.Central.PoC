using System.Diagnostics;

namespace Protege.Central.PoC.Core;

public static class ProcessLauncher
{
    public static event Action? TrackedProcessCountChanged;
    private static int _trackedCount;
    public static int TrackedCount => _trackedCount;

    /// <summary>Abre um terminal cmd visivel executando um comando, numa pasta especifica.</summary>
    public static Process OpenTerminal(string workingDirectory, string command, string title, bool track = true)
    {
        var psi = new ProcessStartInfo("cmd.exe")
        {
            WorkingDirectory = workingDirectory,
            Arguments = $"/k title {title} && {command}",
            UseShellExecute = false,
        };
        return Start(psi, track);
    }

    /// <summary>Abre um terminal cmd com uma variavel de ambiente extra (nao aparece na linha de comando).</summary>
    public static Process OpenTerminalWithEnv(string workingDirectory, string command, string title, (string key, string value) env, bool track = true)
    {
        var psi = new ProcessStartInfo("cmd.exe")
        {
            WorkingDirectory = workingDirectory,
            Arguments = $"/k title {title} && {command}",
            UseShellExecute = false,
        };
        psi.EnvironmentVariables[env.key] = env.value;
        return Start(psi, track);
    }

    public static void OpenPath(string path)
    {
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    public static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private static Process Start(ProcessStartInfo psi, bool track)
    {
        var proc = Process.Start(psi) ?? throw new InvalidOperationException("Falha ao iniciar o processo.");
        if (track)
        {
            proc.EnableRaisingEvents = true;
            Interlocked.Increment(ref _trackedCount);
            TrackedProcessCountChanged?.Invoke();
            proc.Exited += (_, _) =>
            {
                Interlocked.Decrement(ref _trackedCount);
                TrackedProcessCountChanged?.Invoke();
            };
        }
        return proc;
    }
}
