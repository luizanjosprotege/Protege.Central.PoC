# Prompt para recriar o projeto "Protege.Central.PoC"

Copie e cole o prompt abaixo para recriar este projeto do zero, com o mesmo nome, arquitetura e funcionalidades.

---

Crie uma aplicação C# WinForms (.NET, `net10.0-windows`) chamada **Protege.Central.PoC**: uma central de controle desktop para os projetos internos da Protege (Android app-follow-me, Portal Cliente, Docker/Deploy), com o visual copiado do app mobile "Protege Follow Me".

## 1. Arquitetura (2 camadas, projetos separados)

- **`Protege.Central.PoC.Core`** (class library, sem dependência de UI): toda a lógica de integração —
  - `SecretsStore` — lê/grava `secrets.txt` (arquivo local de credenciais, nunca commitado).
  - `MachineScan` — no primeiro uso, varre a máquina (Docker Desktop, JDK 17, Android SDK, cliente SSH, keystore `.jks`) e gera o `secrets.txt` já com os caminhos detectados como comentário; senhas ficam em branco (não são detectáveis).
  - `ProcessLauncher` — abre terminais rastreados (contagem de processos ativos), atalhos para abrir pasta/URL.
  - `AndroidRunner` — localiza Android SDK/emulator/Android Studio, lista AVDs, detecta dispositivo/emulador online via `adb devices`, e orquestra "rodar app": se já há device online usa ele, senão sobe o emulador e espera, depois roda o `run-device.ps1` do projeto `app-follow-me`.
  - `ApiEnvironment` — lê/escreve o `api.ts` do portal-cliente-frontend, trocando qual `baseURL` está ativo (Local / Dev002 / Staging) por comentário/descomentário de linha.
  - `DockerDesktop` — verifica se o Docker Desktop está rodando (`docker info`); se não estiver, abre o executável e aguarda o daemon responder antes de liberar qualquer build/push.
  - `DockerDeploy` — monta comandos `docker build`/`docker push` para as 3 imagens (Pedidos API .NET, Portal Cliente Backend Node, Portal Cliente Frontend Next.js), com tag configurável (default `staging`).
  - `SshHelper` / `DeployRunbook` — abre sessão SSH para os servidores de deploy e guarda o runbook de comandos remotos (docker-compose down/pull/up) como texto de referência.
  - `KeepAlive` — motor "manter tela ativa" via P/Invoke (`SetThreadExecutionState` do kernel32 + `SetCursorPos`/`GetCursorPos` do user32), sem depender de WinForms.

- **`Protege.Central.PoC.App`** (WinForms exe, referencia o Core): `Program.cs` + `DashboardForm.cs` (toda a UI) + `Theme.cs` (paleta/fontes/botões).

## 2. Visual — cópia do app mobile "Protege Follow Me" para desktop

Paleta extraída do tema real do app (`src/theme` do projeto React Native):

- Header: navy `#043154`, texto branco, logo da Protege (shield) à esquerda.
- Fundo da página: cinza-azulado claro `#E6EBF0`.
- Cards de seção: fundo branco, cantos arredondados (raio ~10px via `GraphicsPath`/`Region`), borda `#d8dce0`, título em navy `#043154` bold.
- Botão primário: fundo navy `#012651`, texto branco, hover `#0256B6`, cantos arredondados (raio 6px).
- Botão secundário: fundo branco, texto navy, borda `#d8dce0`.
- Botão selecionado/ativo (ex. ambiente de API atual): fundo `#EEF3F8`, texto navy, borda azul.
- Indicador "Check Run" ativo: fundo `#E3F5E6` / texto `#1E7B33` (verde de sucesso); inativo: fundo `#EFEFEF` / texto `#595959` (neutro) — mesmas cores de status do app mobile.
- Fonte: Open Sans (Regular + Bold) embutida via `PrivateFontCollection`, arquivos `.ttf` em `Assets/Fonts/` (baixados do Google Fonts, licença Apache 2.0), igual ao app mobile.
- Console/log de atividade: painel estilo terminal (fundo navy, texto verde-claro), cantos arredondados.

## 3. Funcionalidades (botões da central)

### Android — app-follow-me
- Abrir Android Studio.
- Selecionar AVD (combo, lista dinâmica dos AVDs instalados) + iniciar emulador direto.
- **Rodar App (auto)**: usa o celular físico se já estiver plugado (`adb devices`); senão sobe o emulador selecionado, espera ficar online, e roda o `run-device.ps1` do projeto (que builda se necessário e sobe o Metro/Expo).
- Rodar limpando cache (`run-device-clean.ps1`), resetar ADB/USB (`reset-adb.ps1`), configurar ambiente (`setup-android.ps1`) — reaproveita os scripts `.ps1` já existentes no repo do app.
- Abrir pasta do APK release / instalar APK release via `adb install -r`.

### Portal Cliente
- Backend completo (abre 2 terminais: `npm run build:watch` + `npm run debug`), ou cada um separado.
- Frontend: `npm run dev`.
- Alternar o ambiente da API (Local / Dev002 / Staging) reescrevendo `frontend/src/services/api.ts`, com destaque visual do ambiente ativo.

### Docker & Deploy
- Antes de qualquer build/push/login: garante o **Docker Desktop aberto e o daemon respondendo** — se não estiver rodando, abre o Docker Desktop automaticamente e aguarda (com timeout) antes de liberar o comando.
- Docker Login (usuário/senha vêm do `secrets.txt`, senha nunca aparece na tela — passada via variável de ambiente do processo filho, não por linha de comando).
- Build/Push com tag configurável para as 3 imagens (Pedidos API, Portal Cliente Backend, Portal Cliente Frontend).
- Abrir sessão SSH para os 2 servidores de deploy (10.2.0.7 e 10.2.0.99), copiando a senha para a área de transferência (nunca exibida na tela).
- Copiar os comandos de deploy remoto (`docker-compose down/pull/up`) para colar na sessão SSH.
- Abrir Portainer (10.2.0.7:9000) e copiar login.
- Painel de notas/referência (URLs internas, branches, códigos de evento) copiado do runbook da equipe.

### Rodapé — Check Run / Check Stop
- Mesmo motor de "manter tela ativa" do utilitário original (move o cursor 5px verticalmente a cada 2s + `SetThreadExecutionState`).
- Liga manualmente pelo botão **Check Run** (vira **Check Stop** quando ativo), rotulado e visível (não escondido) — e também **liga automaticamente sempre que qualquer tarefa disparada pela central estiver rodando** (build:watch, debug, npm dev, docker build, etc.), desligando quando todas terminarem, a menos que o usuário tenha ligado manualmente.

## 4. Credenciais locais

`secrets.txt` na pasta do executável, nunca commitado (`.gitignore`), gerado automaticamente no primeiro uso (varredura da máquina) e editável a qualquer momento pelo botão "Editar credenciais": Docker Hub, SSH dos 2 servidores, Portainer, Senha Segura, keystore de assinatura do APK.

## 5. Build seguro (ofuscado)

Script `build-secure.ps1` na raiz do projeto:
1. `dotnet publish` self-contained win-x64 (sem single-file, para permitir ofuscar as DLLs soltas).
2. Ofusca `Protege.Central.PoC.dll` e `Protege.Central.PoC.Core.dll` juntas com **Obfuscar** (ferramenta open source, `tools/obfuscar/`) — renomeia classes/métodos/campos/propriedades internos, com as duas DLLs processadas no mesmo projeto Obfuscar para manter as referências cruzadas entre elas consistentes.
3. Remove os `.pdb` do artefato final.
4. Resultado em `publish/Protege.Central.PoC.exe` + DLLs ofuscadas + runtime .NET + `Assets/` + `secrets.txt`.

Isso eleva bastante o esforço de engenharia reversa (nomes internos irreconhecíveis, sem símbolos de debug), mas não é uma garantia absoluta — qualquer binário .NET rodando localmente pode, em tese, ser instrumentado por quem tem acesso à máquina.

---

**Resumo do propósito**: um hub único para o time abrir o ambiente Android, subir o Portal Cliente (back+front), apontar o frontend para o ambiente certo, e fazer login/build/push/deploy no Docker — tudo com a cara do app mobile da Protege, mantendo a tela ativa automaticamente enquanto qualquer uma dessas tarefas estiver rodando.
