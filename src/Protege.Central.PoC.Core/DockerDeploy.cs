namespace Protege.Central.PoC.Core;

public enum DockerImageTarget
{
    PedidosApiDotnet,
    PortalClienteBackendNode,
    PortalClienteFrontendNext,
}

public sealed record DockerImageInfo(string Image, string BuildPath, string Label);

public static class DockerDeploy
{
    public static readonly Dictionary<DockerImageTarget, DockerImageInfo> Images = new()
    {
        [DockerImageTarget.PedidosApiDotnet] = new(
            "grupoprotege/pedidos-api", @"C:\proj\pedidos-api", "Pedidos API (.NET)"),
        [DockerImageTarget.PortalClienteBackendNode] = new(
            "grupoprotege/portalprotege-nodejs-backend", @"C:\proj\portal-cliente\backend", "Portal Cliente Backend (Node)"),
        [DockerImageTarget.PortalClienteFrontendNext] = new(
            "grupoprotege/portalprotege-nextjs-frontend", @"C:\proj\portal-cliente\frontend", "Portal Cliente Frontend (Next.js)"),
    };

    /// <summary>Comando de login sem expor a senha na linha de comando (le de uma env var do processo filho).</summary>
    public static (string command, (string key, string value) env) BuildLoginCommand(string user, string password)
        => ($"(echo %DPW%) | docker login -u {user} --password-stdin & set DPW=", ("DPW", password));

    public static string BuildCommand(DockerImageTarget target, string tag)
        => $"docker build . -t {Images[target].Image}:{tag}";

    public static string PushCommand(DockerImageTarget target, string tag)
        => $"docker push {Images[target].Image}:{tag}";

    public static string BuildAndPushCommand(DockerImageTarget target, string tag)
        => $"{BuildCommand(target, tag)} && {PushCommand(target, tag)}";
}
