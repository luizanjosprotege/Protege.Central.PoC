namespace Protege.Central.PoC.Core;

public enum DeployServer
{
    PedidosApiServer,
    PortalClienteServer,
}

public sealed record ServerInfo(string Host, string Label, string RemotePath, string ComposeSteps);

/// <summary>
/// Runbook de deploy conforme documentado pela equipe. Nenhuma credencial vive aqui -
/// usuario/senha vem sempre do secrets.txt local, nunca hardcoded.
/// </summary>
public static class DeployRunbook
{
    public static readonly Dictionary<DeployServer, ServerInfo> Servers = new()
    {
        [DeployServer.PedidosApiServer] = new(
            "10.2.0.7",
            "Pedidos API (.NET)",
            "/home/pedidos-api",
            "cd /home/pedidos-api\ndocker-compose down\ndocker-compose pull\ndocker-compose up -d"),
        [DeployServer.PortalClienteServer] = new(
            "10.2.0.99",
            "Portal Cliente (Node/Next.js)",
            "/home/user-docker/portal-cliente-nginx",
            "cd /home/user-docker/portal-cliente-nginx\ndocker-compose down\ndocker-compose pull\ndocker-compose up -d\n\n# Se ocorrer erro 502:\n# docker-compose restart proxy"),
    };

    public const string Notes = """
        API FINANCEIRO:
        https://protegeprotecao192464.datasul.cloudtotvs.com.br/api/cufp/v1/Documentos?pageSize=500&page=1

        TOTVS_FINANCEIRO_URL:
        http://172.20.32.69:8080/api/cufp/v1

        BACKEND API DEV002:
        http://10.2.0.99:33052/backend

        Portainer (10.2.0.7):
        http://10.2.0.7:9000/#!/home

        Branches de referencia:
        - RELATORIOS DE COLETA E SALDO: hotfix/DCX-278
        - ZIP NOTAS FINANCEIRO: feat/DCX-380

        Deploy producao / FSM:
        1. Criar branch release: release_[data] a partir da develop
        2. Gerar imagem docker com tag :staging (mesmo fluxo do backend Node)

        codigoEvento = 31
        """;
}
