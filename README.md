# Protege.Central.PoC

Central de controle desktop para os projetos internos da Protege — Android (`app-follow-me`), Portal Cliente (backend/frontend) e Docker/Deploy — com o visual copiado do app mobile "Protege Follow Me".

Ver [RECREATE_PROMPT.md](RECREATE_PROMPT.md) para a especificação completa de arquitetura/funcionalidades/visual.

## Pré-requisitos

- Windows
- .NET SDK 10 (`dotnet --version`)
- Para as funções de Android: Android SDK + Android Studio já instalados
- Para as funções de Portal Cliente: Node.js/npm e os repositórios `C:\proj\portal-cliente` e `C:\proj\app-follow-me` presentes
- Para as funções de Docker: Docker Desktop instalado

## Build

```powershell
# build simples (debug, para desenvolver)
dotnet build src\Protege.Central.PoC.App\Protege.Central.PoC.App.csproj

# build seguro/ofuscado (release, self-contained win-x64)
.\build-secure.ps1
```

O executável final fica em `publish\Protege.Central.PoC.exe`.

## Primeira execução

Ao abrir o app pela primeira vez em uma máquina, ele varre o ambiente local (Docker Desktop, JDK 17, Android SDK, cliente SSH, keystore de assinatura) e gera automaticamente um `secrets.txt` ao lado do executável, com o que foi detectado já preenchido e os campos de senha em branco. Abra esse arquivo (botão "Editar credenciais" no app, ou direto pelo Notepad) e preencha as senhas antes de usar os botões de login/deploy. Esse arquivo nunca deve ser commitado — já está no `.gitignore`.

## Prompt de primeiro uso (para quem clonar o repositório)

Ao clonar este repositório em uma máquina nova, cole o prompt abaixo no seu assistente de codificação (ex.: Claude Code) para deixar o projeto pronto para uso:

```
Acabei de clonar o repositório Protege.Central.PoC. Por favor:

1. Verifique se o .NET SDK 10 está instalado (`dotnet --version`).
2. Restaure e compile a solução (`dotnet build` a partir de
   src\Protege.Central.PoC.App\Protege.Central.PoC.App.csproj).
3. Rode o executável de debug uma vez (bin\Debug\net10.0-windows\Protege.Central.PoC.exe)
   só para disparar a primeira execução — isso vai gerar automaticamente um secrets.txt
   ao lado do executável, com uma varredura desta máquina (Docker Desktop, JDK 17,
   Android SDK, cliente SSH, keystore .jks) já preenchida como comentário.
4. Feche o app, abra o secrets.txt gerado e me avise quais campos de senha
   (DOCKER_PASS, SSH_PASS_1, SSH_PASS_2, PORTAINER_PASS, SENHASEGURA_PASS,
   KEYSTORE_PASS) estão faltando — vou te passar os valores para preencher.
   NÃO tente adivinhar ou inventar nenhuma senha.
5. Confirme se os caminhos assumidos pelo app ainda existem nesta máquina:
   C:\proj\app-follow-me, C:\proj\portal-cliente, C:\proj\pedidos-api,
   e o Android SDK em %USERPROFILE%\Android\Sdk. Se algum caminho for
   diferente aqui, me avise antes de mexer no código.
6. Quando as credenciais estiverem preenchidas, rode .\build-secure.ps1 para gerar
   o build final (self-contained, ofuscado) em publish\Protege.Central.PoC.exe.

Não commite o secrets.txt nem o Mapping.txt de ofuscação em nenhuma hipótese.
```

## Estrutura

```
src/
  Protege.Central.PoC.Core/   # logica de integracao, sem UI
  Protege.Central.PoC.App/    # WinForms (dashboard, tema, Program.cs)
tools/obfuscar/                # ofuscador usado pelo build-secure.ps1
build-secure.ps1               # build release self-contained + ofuscacao
RECREATE_PROMPT.md             # especificacao completa do projeto
```
