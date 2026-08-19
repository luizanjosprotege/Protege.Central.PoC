using System.Diagnostics;

namespace Protege.Central.PoC.Core;

/// <summary>
/// Garante que o Docker Desktop esteja aberto e o daemon respondendo antes de
/// qualquer build/push, abrindo-o automaticamente quando necessario.
/// </summary>
public static class DockerDesktop
{
    public static readonly string ExePath = @"C:\Program Files\Docker\Docker\Docker Desktop.exe";

    public static bool IsRunning()
    {
        try
        {
            var psi = new ProcessStartInfo("docker", "info")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p == null) return false;
            p.WaitForExit(5000);
            return p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> EnsureRunningAsync(Action<string> log, int timeoutSeconds = 90)
    {
        if (IsRunning())
        {
            log("Docker Desktop ja esta rodando.");
            return true;
        }

        log("Docker Desktop nao esta rodando. Iniciando...");
        if (File.Exists(ExePath))
        {
            Process.Start(new ProcessStartInfo(ExePath) { UseShellExecute = true });
        }
        else
        {
            log($"Docker Desktop nao encontrado em: {ExePath}");
            return false;
        }

        var waited = 0;
        while (!IsRunning() && waited < timeoutSeconds)
        {
            await Task.Delay(3000);
            waited += 3;
            log($"Aguardando o Docker Desktop iniciar... ({waited}s)");
        }

        if (!IsRunning())
        {
            log("Timeout esperando o Docker Desktop iniciar. Verifique manualmente.");
            return false;
        }

        log("Docker Desktop pronto (daemon respondendo).");
        return true;
    }
}
