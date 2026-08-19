using System.Text;

namespace Protege.Central.PoC.Core;

/// <summary>
/// No primeiro uso, varre a maquina local para descobrir o que ja esta instalado/disponivel
/// (Docker Desktop, JDK, cliente SSH, keystore de assinatura) e gera o secrets.txt com essas
/// informacoes ja confirmadas. Senhas nunca sao detectaveis por uma varredura - ficam em branco
/// para o usuario preencher.
/// </summary>
public static class MachineScan
{
    public static string BuildSecretsTemplate()
    {
        var dockerPath = FindDockerDesktop();
        var keystorePath = FindKeystore();
        var jdkPath = FindJdk();
        var sshPath = FindSsh();
        var sdkPath = AndroidRunner.AndroidHome;
        var sdkFound = Directory.Exists(sdkPath) && File.Exists(AndroidRunner.Adb);

        var sb = new StringBuilder();
        sb.AppendLine("# ===================================================================");
        sb.AppendLine("#  Protege.Central.PoC - credenciais locais");
        sb.AppendLine("#  Gerado automaticamente no primeiro uso a partir de uma varredura desta");
        sb.AppendLine("#  maquina (ferramentas/paths detectados abaixo como comentario).");
        sb.AppendLine("#  Senhas NAO sao detectaveis - preencha manualmente.");
        sb.AppendLine("#  Este arquivo NAO deve ser commitado nem compartilhado.");
        sb.AppendLine("# ===================================================================");
        sb.AppendLine("#");
        sb.AppendLine($"#  Docker Desktop : {Found(dockerPath)}");
        sb.AppendLine($"#  Android SDK    : {(sdkFound ? sdkPath : "nao encontrado em " + sdkPath)}");
        sb.AppendLine($"#  JDK 17         : {Found(jdkPath)}");
        sb.AppendLine($"#  Cliente SSH    : {Found(sshPath)}");
        sb.AppendLine($"#  Keystore APK   : {Found(keystorePath)}");
        sb.AppendLine("#");
        sb.AppendLine();
        sb.AppendLine("# --- Docker Hub ---");
        sb.AppendLine("DOCKER_USER=dockerprotege");
        sb.AppendLine("DOCKER_PASS=");
        sb.AppendLine();
        sb.AppendLine("# --- Servidor 10.2.0.7 (Pedidos API / Portainer) ---");
        sb.AppendLine("SSH_HOST_1=10.2.0.7");
        sb.AppendLine("SSH_USER_1=");
        sb.AppendLine("SSH_PASS_1=");
        sb.AppendLine("PORTAINER_USER=admin");
        sb.AppendLine("PORTAINER_PASS=");
        sb.AppendLine();
        sb.AppendLine("# --- Servidor 10.2.0.99 (Portal Cliente / nginx) ---");
        sb.AppendLine("SSH_HOST_2=10.2.0.99");
        sb.AppendLine("SSH_USER_2=welcome");
        sb.AppendLine("SSH_PASS_2=");
        sb.AppendLine();
        sb.AppendLine("# --- Senha Segura / PAM Core ---");
        sb.AppendLine("SENHASEGURA_USER=");
        sb.AppendLine("SENHASEGURA_PASS=");
        sb.AppendLine();
        sb.AppendLine("# --- Keystore de assinatura do APK (app-follow-me) ---");
        sb.AppendLine($"KEYSTORE_PATH={keystorePath ?? @"C:\Users\luiz.belago\build_android.jks"}");
        sb.AppendLine("KEYSTORE_PASS=");
        sb.AppendLine("KEYSTORE_ALIAS=key0");
        return sb.ToString();
    }

    private static string Found(string? value) => value ?? "nao encontrado";

    private static string? FindDockerDesktop()
    {
        var path = @"C:\Program Files\Docker\Docker\Docker Desktop.exe";
        return File.Exists(path) ? path : null;
    }

    private static string? FindKeystore()
    {
        const string hintedName = "build_android.jks";
        var hinted = Path.Combine(@"C:\Users\luiz.belago", hintedName);
        if (File.Exists(hinted)) return hinted;

        try
        {
            foreach (var userDir in Directory.EnumerateDirectories(@"C:\Users"))
            {
                var candidate = Path.Combine(userDir, hintedName);
                if (File.Exists(candidate)) return candidate;
            }
        }
        catch { /* sem permissao em alguma pasta de usuario - ignora */ }

        return null;
    }

    private static string? FindJdk()
    {
        var searchRoots = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Local", "Programs", "Microsoft"),
            @"C:\Program Files\Microsoft",
            @"C:\Program Files\Eclipse Adoptium",
            @"C:\Program Files\Java",
        };
        foreach (var root in searchRoots)
        {
            if (!Directory.Exists(root)) continue;
            var match = Directory.GetDirectories(root, "jdk-17*").FirstOrDefault();
            if (match != null && File.Exists(Path.Combine(match, "bin", "java.exe"))) return match;
        }
        return null;
    }

    private static string? FindSsh()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "OpenSSH", "ssh.exe");
        return File.Exists(path) ? path : null;
    }
}
