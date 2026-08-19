using System.Diagnostics;

namespace Protege.Central.PoC.Core;

public static class AndroidRunner
{
    public static readonly string AppDir = @"C:\proj\app-follow-me";
    public static readonly string AndroidHome = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Android", "Sdk");
    public static readonly string Adb = Path.Combine(AndroidHome, "platform-tools", "adb.exe");
    public static readonly string Emulator = Path.Combine(AndroidHome, "emulator", "emulator.exe");
    public static readonly string AndroidStudio = @"C:\Program Files\Android\Android Studio\bin\studio64.exe";
    public static readonly string AvdDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".android", "avd");

    public static List<string> ListAvds()
    {
        if (!Directory.Exists(AvdDir)) return [];
        return Directory.GetFiles(AvdDir, "*.ini")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!)
            .OrderBy(n => n)
            .ToList();
    }

    public static bool HasOnlineDevice()
    {
        if (!File.Exists(Adb)) return false;
        var psi = new ProcessStartInfo(Adb, "devices")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var p = Process.Start(psi);
        if (p == null) return false;
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit(5000);
        return output.Split('\n')
            .Any(line => line.Contains('\t') && line.Contains("device") && !line.Contains("List of"));
    }

    public static bool StartEmulator(string avdName)
    {
        if (!File.Exists(Emulator)) return false;
        Process.Start(new ProcessStartInfo(Emulator, $"-avd \"{avdName}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = false,
        });
        return true;
    }

    /// <summary>Roda o fluxo completo: usa dispositivo/emulador ja online, ou sobe o emulador e espera antes de rodar run-device.ps1.</summary>
    public static async Task RunAppSmartAsync(string avdNameIfNeeded, Action<string> log)
    {
        log("Verificando dispositivos/emuladores online...");
        if (!HasOnlineDevice())
        {
            log($"Nenhum dispositivo online. Iniciando emulador '{avdNameIfNeeded}'...");
            if (!StartEmulator(avdNameIfNeeded))
            {
                log($"Emulator nao encontrado em: {Emulator}");
                return;
            }

            var waited = 0;
            const int timeoutSeconds = 120;
            while (!HasOnlineDevice() && waited < timeoutSeconds)
            {
                await Task.Delay(2000);
                waited += 2;
                log($"Aguardando o emulador iniciar... ({waited}s)");
            }

            if (!HasOnlineDevice())
            {
                log("Timeout esperando o emulador. Abra o Android Studio e verifique o AVD manualmente.");
                return;
            }
        }

        log("Dispositivo/emulador online. Rodando run-device.ps1 (build se necessario + Metro)...");
        ProcessLauncher.OpenTerminal(
            AppDir,
            "powershell -NoExit -ExecutionPolicy Bypass -File run-device.ps1",
            "app-follow-me: run-device");
    }
}
