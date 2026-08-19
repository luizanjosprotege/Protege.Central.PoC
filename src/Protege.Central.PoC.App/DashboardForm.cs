using Protege.Central.PoC.Core;

namespace Protege.Central.PoC.App;

public sealed class DashboardForm : Form
{
    private readonly KeepAlive _keepAlive = new();
    private readonly ToolTip _toolTip = new();
    private TextBox _console = null!;
    private Label _checkStatusLabel = null!;
    private Button _checkButton = null!;
    private bool _manualKeepAlive;
    private readonly Dictionary<ApiTarget, Button> _apiButtons = new();

    public DashboardForm()
    {
        Text = "Protege.Central.PoC — Central de Controle de Projetos";
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1100, 820);
        MinimumSize = new Size(920, 650);
        BackColor = Theme.PageBg;
        Font = Theme.Regular(9F);

        ProcessLauncher.TrackedProcessCountChanged += () => BeginInvoke(RefreshKeepAliveEffectiveState);
        _keepAlive.ActiveChanged += _ => BeginInvoke(RefreshKeepAliveEffectiveState);

        BuildUi();
        RefreshKeepAliveEffectiveState();

        var created = SecretsStore.EnsureExists();
        if (created) Log("Primeira execucao: secrets.txt gerado varrendo a maquina (Docker/JDK/SDK/SSH/keystore). Preencha as senhas antes de usar login/deploy.");
    }

    // ------------------------------------------------------------- layout

    private void BuildUi()
    {
        Controls.Add(BuildFooter());
        Controls.Add(BuildConsole());
        Controls.Add(BuildContent());
        Controls.Add(BuildHeader());
    }

    private Control BuildHeader()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 96, BackColor = Theme.HeaderNavy };

        var logoBox = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(52, 52),
            Location = new Point(20, 16),
            BackColor = Color.Transparent,
        };
        var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "protege-logo.png");
        if (File.Exists(logoPath)) logoBox.Image = Image.FromFile(logoPath);
        header.Controls.Add(logoBox);

        var titles = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Location = new Point(86, 18),
            BackColor = Color.Transparent,
        };
        titles.Controls.Add(new Label
        {
            Text = "Central de Controle de Projetos",
            Font = Theme.Bold(15F),
            ForeColor = Color.White,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4),
            BackColor = Color.Transparent,
        });
        titles.Controls.Add(new Label
        {
            Text = "Protege.Central.PoC — Android · Portal Cliente · Docker/Deploy",
            Font = Theme.Regular(9.5F),
            ForeColor = Color.FromArgb(255, 190, 205, 220),
            AutoSize = true,
            Margin = new Padding(0),
            BackColor = Color.Transparent,
        });
        header.Controls.Add(titles);

        return header;
    }

    private Control BuildContent()
    {
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.PageBg };
        var content = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(20),
            BackColor = Theme.PageBg,
        };
        scroll.Controls.Add(content);

        content.Controls.Add(BuildAndroidSection());
        content.Controls.Add(BuildPortalClienteSection());
        content.Controls.Add(BuildDockerSection());
        content.Controls.Add(BuildReferenceSection());

        return scroll;
    }

    private Control BuildConsole()
    {
        var panel = new Panel { Dock = DockStyle.Bottom, Height = 130, BackColor = Theme.PageBg, Padding = new Padding(20, 0, 20, 16) };
        _console = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Theme.HeaderNavy,
            ForeColor = Color.FromArgb(255, 170, 220, 190),
            Font = new Font("Consolas", 9F),
            BorderStyle = BorderStyle.None,
        };
        panel.Controls.Add(_console);
        Theme.RoundCorners(_console, 8);
        return panel;
    }

    private Control BuildFooter()
    {
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 44, BackColor = Theme.CardBg };
        var topBorder = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Theme.Divider };
        footer.Controls.Add(topBorder);

        _checkStatusLabel = new Label
        {
            AutoSize = true,
            ForeColor = Theme.SecondaryText,
            Font = Theme.Regular(8.5F),
            Location = new Point(20, 15),
        };
        footer.Controls.Add(_checkStatusLabel);

        _checkButton = new Button
        {
            Text = "Check Run",
            Size = new Size(110, 28),
            FlatStyle = FlatStyle.Flat,
            Font = Theme.Bold(9F),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand,
        };
        _checkButton.FlatAppearance.BorderSize = 0;
        Theme.RoundCorners(_checkButton, 14);
        _checkButton.Click += (_, _) =>
        {
            _manualKeepAlive = !_manualKeepAlive;
            RefreshKeepAliveEffectiveState();
        };
        _toolTip.SetToolTip(_checkButton,
            "Mantem o Windows ativo (sem bloqueio de tela/suspensao) enquanto ligado, " +
            "ou automaticamente enquanto houver tarefas em execucao no painel.");
        footer.Resize += (_, _) => _checkButton.Location = new Point(footer.Width - _checkButton.Width - 20, 8);
        footer.Controls.Add(_checkButton);

        return footer;
    }

    // ------------------------------------------------------------- sections

    private Control BuildAndroidSection()
    {
        var avds = AndroidRunner.ListAvds();
        var avdCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
        avdCombo.Items.AddRange(avds.Count > 0 ? [.. avds] : new object[] { "(nenhum AVD encontrado)" });
        avdCombo.SelectedIndex = 0;
        StyleCombo(avdCombo);

        var row1 = NewRow();
        row1.Controls.Add(CreateActionButton("🖥 Abrir Android Studio", "Abre a IDE completa, se precisar configurar algo manualmente.",
            (_, _) => SafeRun(() => ProcessLauncher.OpenPath(AndroidRunner.AndroidStudio))));
        row1.Controls.Add(avdCombo);
        row1.Controls.Add(CreateActionButton("▶ Iniciar Emulador", "Sobe o AVD selecionado diretamente (mais rapido que abrir o Studio inteiro).",
            (_, _) => SafeRun(() =>
            {
                if (!AndroidRunner.StartEmulator((string)avdCombo.SelectedItem!))
                    Log("Emulator nao encontrado em: " + AndroidRunner.Emulator);
                else
                    Log($"Iniciando emulador '{avdCombo.SelectedItem}'...");
            })));

        var row2 = NewRow();
        row2.Controls.Add(CreateActionButton("📱 Rodar App (auto)", "Usa o celular fisico se estiver plugado; senao sobe o emulador e roda run-device.ps1.",
            async (_, _) => await SafeRunAsync(async () =>
                await AndroidRunner.RunAppSmartAsync((string)avdCombo.SelectedItem!, Log)), Theme.ButtonKind.Primary));
        row2.Controls.Add(CreateActionButton("🧹 Rodar limpando cache", "Executa run-device-clean.ps1 (limpa Metro/Expo antes de subir).",
            (_, _) => SafeRun(() => ProcessLauncher.OpenTerminal(AndroidRunner.AppDir,
                "powershell -NoExit -ExecutionPolicy Bypass -File run-device-clean.ps1", "app-follow-me: clean"))));
        row2.Controls.Add(CreateActionButton("🔄 Resetar ADB/USB", "Reseta o servidor ADB e as chaves de autorizacao USB.",
            (_, _) => SafeRun(() => ProcessLauncher.OpenTerminal(AndroidRunner.AppDir,
                "powershell -NoExit -ExecutionPolicy Bypass -File reset-adb.ps1", "app-follow-me: reset-adb"))));
        row2.Controls.Add(CreateActionButton("🛠 Configurar ambiente", "Roda setup-android.ps1 (JDK 17 + Android SDK). So precisa 1x por maquina.",
            (_, _) => SafeRun(() => ProcessLauncher.OpenTerminal(AndroidRunner.AppDir,
                "powershell -NoExit -ExecutionPolicy Bypass -File setup-android.ps1", "app-follow-me: setup"))));

        var releaseApkDir = Path.Combine(AndroidRunner.AppDir, "android", "app", "build", "outputs", "apk", "release");
        var row3 = NewRow();
        row3.Controls.Add(CreateActionButton("📦 Abrir pasta do APK (release)", releaseApkDir,
            (_, _) => SafeRun(() => ProcessLauncher.OpenPath(releaseApkDir))));
        row3.Controls.Add(CreateActionButton("⬆ Instalar APK release", "adb install -r app-release.apk (no dispositivo/emulador online).",
            (_, _) => SafeRun(() => ProcessLauncher.OpenTerminal(releaseApkDir,
                "adb install -r app-release.apk", "adb install release"))));

        return CreateSection("Android — app-follow-me", row1, row2, row3);
    }

    private Control BuildPortalClienteSection()
    {
        const string backendDir = @"C:\proj\portal-cliente\backend";
        const string frontendDir = @"C:\proj\portal-cliente\frontend";

        var row1 = NewRow();
        row1.Controls.Add(CreateActionButton("▶ Backend completo", "Abre 2 terminais: build:watch (compila TS) + debug (nodemon).",
            (_, _) => SafeRun(() =>
            {
                ProcessLauncher.OpenTerminal(backendDir, "npm run build:watch", "portal-cliente: build:watch");
                ProcessLauncher.OpenTerminal(backendDir, "npm run debug", "portal-cliente: debug");
            }), Theme.ButtonKind.Primary));
        row1.Controls.Add(CreateActionButton("⚙ Backend: build:watch", "npm run build:watch",
            (_, _) => SafeRun(() => ProcessLauncher.OpenTerminal(backendDir, "npm run build:watch", "portal-cliente: build:watch"))));
        row1.Controls.Add(CreateActionButton("🐞 Backend: debug", "npm run debug",
            (_, _) => SafeRun(() => ProcessLauncher.OpenTerminal(backendDir, "npm run debug", "portal-cliente: debug"))));
        row1.Controls.Add(CreateActionButton("🌐 Frontend: dev", "npm run dev",
            (_, _) => SafeRun(() => ProcessLauncher.OpenTerminal(frontendDir, "npm run dev", "portal-cliente: dev"))));

        var row2 = NewRow();
        var apiLabel = new Label { Text = "API aponta para:", ForeColor = Theme.SecondaryText, AutoSize = true, Padding = new Padding(0, 10, 4, 0) };
        row2.Controls.Add(apiLabel);
        foreach (var target in Enum.GetValues<ApiTarget>())
        {
            var btn = CreateActionButton(ApiLabel(target), ApiUrl(target), (_, _) => SafeRun(() =>
            {
                ApiEnvironment.SetTarget(target);
                Log($"api.ts atualizado -> {ApiLabel(target)} ({ApiUrl(target)})");
                RefreshApiButtons();
            }));
            _apiButtons[target] = btn;
            row2.Controls.Add(btn);
        }
        row2.Controls.Add(CreateActionButton("📝 Editar api.ts", ApiEnvironment.FilePath,
            (_, _) => SafeRun(() => ProcessLauncher.OpenPath(ApiEnvironment.FilePath))));

        RefreshApiButtons();

        return CreateSection("Portal Cliente", row1, row2);
    }

    private Control BuildDockerSection()
    {
        var row1 = NewRow();
        row1.Controls.Add(CreateActionButton("🔐 Docker Login", "Usa DOCKER_USER/DOCKER_PASS do secrets.txt (senha nunca aparece na tela). Abre o Docker Desktop se preciso.",
            async (_, _) => await RunDockerGuarded(DoDockerLogin), Theme.ButtonKind.Primary));
        row1.Controls.Add(CreateActionButton("🔑 Editar credenciais", SecretsStore.FilePath,
            (_, _) => SafeRun(() => ProcessLauncher.OpenPath(SecretsStore.FilePath))));

        var imagesPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        foreach (var target in Enum.GetValues<DockerImageTarget>())
            imagesPanel.Controls.Add(BuildImageRow(target));

        var row2 = NewRow();
        var server1 = DeployRunbook.Servers[DeployServer.PedidosApiServer];
        row2.Controls.Add(CreateActionButton($"💻 SSH — {server1.Label}", $"{server1.Host} · {server1.RemotePath}",
            (_, _) => SafeRun(() => OpenSsh(DeployServer.PedidosApiServer))));
        row2.Controls.Add(CreateActionButton("📋 Copiar comandos", server1.ComposeSteps,
            (_, _) => SafeRun(() => CopyToClipboard(server1.ComposeSteps, "Comandos de deploy copiados."))));
        row2.Controls.Add(CreateActionButton("🌍 Abrir Portainer", "http://10.2.0.7:9000",
            (_, _) => SafeRun(() => ProcessLauncher.OpenUrl("http://10.2.0.7:9000/#!/home"))));
        row2.Controls.Add(CreateActionButton("📋 Copiar login Portainer", "usa PORTAINER_USER/PASS do secrets.txt",
            (_, _) => SafeRun(() =>
            {
                var s = SecretsStore.Load();
                CopyToClipboard($"{s.Get("PORTAINER_USER")} / {s.Get("PORTAINER_PASS")}", "Login do Portainer copiado.");
            })));

        var row3 = NewRow();
        var server2 = DeployRunbook.Servers[DeployServer.PortalClienteServer];
        row3.Controls.Add(CreateActionButton($"💻 SSH — {server2.Label}", $"{server2.Host} · {server2.RemotePath}",
            (_, _) => SafeRun(() => OpenSsh(DeployServer.PortalClienteServer))));
        row3.Controls.Add(CreateActionButton("📋 Copiar comandos", server2.ComposeSteps,
            (_, _) => SafeRun(() => CopyToClipboard(server2.ComposeSteps, "Comandos de deploy copiados."))));

        return CreateSection("Docker & Deploy", row1, imagesPanel, row2, row3);
    }

    private Control BuildImageRow(DockerImageTarget target)
    {
        var info = DockerDeploy.Images[target];
        var row = NewRow();

        var label = new Label { Text = info.Label, ForeColor = Theme.TitleText, AutoSize = true, Padding = new Padding(0, 10, 8, 0), Width = 220 };
        row.Controls.Add(label);

        var tagBox = new TextBox { Text = "staging", Width = 90 };
        StyleTextBox(tagBox);
        row.Controls.Add(tagBox);

        row.Controls.Add(CreateActionButton("🏗 Build", info.BuildPath + "  (abre o Docker Desktop se preciso)",
            async (_, _) => await RunDockerGuarded(() => ProcessLauncher.OpenTerminal(info.BuildPath,
                DockerDeploy.BuildCommand(target, tagBox.Text), $"docker build {info.Label}"))));
        row.Controls.Add(CreateActionButton("📤 Push", info.Image + "  (abre o Docker Desktop se preciso)",
            async (_, _) => await RunDockerGuarded(() => ProcessLauncher.OpenTerminal(info.BuildPath,
                DockerDeploy.PushCommand(target, tagBox.Text), $"docker push {info.Label}"))));

        return row;
    }

    private Control BuildReferenceSection()
    {
        var box = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            Width = 1000,
            Height = 200,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Theme.PageBg,
            ForeColor = Theme.SecondaryText,
            Font = new Font("Consolas", 9F),
            BorderStyle = BorderStyle.FixedSingle,
            Text = DeployRunbook.Notes.ReplaceLineEndings(),
        };
        var row = NewRow();
        row.Controls.Add(box);
        return CreateSection("Notas / Referências", row);
    }

    // ------------------------------------------------------------- actions

    private void DoDockerLogin()
    {
        var s = SecretsStore.Load();
        var user = s.Get("DOCKER_USER");
        var pass = s.Get("DOCKER_PASS");
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            MessageBox.Show("Preencha DOCKER_USER e DOCKER_PASS em secrets.txt antes de logar.",
                "Credenciais ausentes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var (command, env) = DockerDeploy.BuildLoginCommand(user, pass);
        ProcessLauncher.OpenTerminalWithEnv(AppContext.BaseDirectory, command, "docker login", env, track: false);
        Log($"Docker login solicitado para o usuario '{user}'.");
    }

    private void OpenSsh(DeployServer server)
    {
        var info = DeployRunbook.Servers[server];
        var s = SecretsStore.Load();
        var userKey = server == DeployServer.PedidosApiServer ? "SSH_USER_1" : "SSH_USER_2";
        var passKey = server == DeployServer.PedidosApiServer ? "SSH_PASS_1" : "SSH_PASS_2";
        var user = s.Get(userKey);
        if (string.IsNullOrEmpty(user))
        {
            MessageBox.Show($"Preencha {userKey} em secrets.txt antes de conectar.",
                "Credenciais ausentes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        SshHelper.OpenSession(info.Host, user);
        var pass = s.Get(passKey);
        if (!string.IsNullOrEmpty(pass))
            CopyToClipboard(pass, $"Senha de {info.Host} copiada — cole no prompt do SSH.");
        Log($"Sessao SSH aberta para {user}@{info.Host}.");
    }

    private void CopyToClipboard(string text, string message)
    {
        Clipboard.SetText(text);
        Log(message);
    }

    private void RefreshApiButtons()
    {
        var current = ApiEnvironment.GetCurrent();
        foreach (var (target, btn) in _apiButtons)
            Theme.SetKind(btn, target == current ? Theme.ButtonKind.Selected : Theme.ButtonKind.Secondary);
    }

    private void RefreshKeepAliveEffectiveState()
    {
        var tasksRunning = ProcessLauncher.TrackedCount;
        var shouldBeActive = _manualKeepAlive || tasksRunning > 0;

        if (shouldBeActive) _keepAlive.Start(); else _keepAlive.Stop();

        _checkButton.Text = _keepAlive.IsActive ? "Check Stop" : "Check Run";
        _checkButton.BackColor = _keepAlive.IsActive ? Theme.SuccessBg : Theme.NeutralBg;
        _checkButton.ForeColor = _keepAlive.IsActive ? Theme.SuccessText : Theme.NeutralText;
        _checkStatusLabel.Text = _keepAlive.IsActive
            ? (tasksRunning > 0
                ? $"Tela ativa — {tasksRunning} tarefa(s) em execucao"
                : "Tela ativa — ligado manualmente")
            : "Inativo";
    }

    private void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
        if (_console.InvokeRequired) { _console.BeginInvoke(() => AppendLog(line)); return; }
        AppendLog(line);
    }

    private void AppendLog(string line)
    {
        _console.AppendText(line);
    }

    private static void SafeRun(Action action)
    {
        try { action(); }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task SafeRunAsync(Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>Garante o Docker Desktop aberto e o daemon respondendo antes de rodar a acao.</summary>
    private async Task RunDockerGuarded(Action action) => await SafeRunAsync(async () =>
    {
        var ready = await DockerDesktop.EnsureRunningAsync(Log);
        if (!ready)
        {
            MessageBox.Show("Nao foi possivel iniciar o Docker Desktop. Abra manualmente e tente novamente.",
                "Docker Desktop", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        action();
    });

    private static string ApiLabel(ApiTarget t) => t switch
    {
        ApiTarget.Local => "Local",
        ApiTarget.Dev002 => "Dev002",
        ApiTarget.Staging => "Staging",
        _ => t.ToString(),
    };

    private static string ApiUrl(ApiTarget t) => t switch
    {
        ApiTarget.Local => "http://localhost:3333/backend",
        ApiTarget.Dev002 => "http://10.2.0.99:33052/backend",
        ApiTarget.Staging => "https://portal-hml2.protege.com.br/backend",
        _ => "",
    };

    // ------------------------------------------------------------- widgets

    private Panel CreateSection(string title, params Control[] rows)
    {
        var section = new Panel
        {
            BackColor = Theme.CardBg,
            Width = 1020,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(18),
            Margin = new Padding(0, 0, 0, 18),
        };
        section.Paint += (_, e) => ControlPaint.DrawBorder(e.Graphics, section.ClientRectangle,
            Theme.Divider, ButtonBorderStyle.Solid);
        Theme.RoundCorners(section, 10);

        var stack = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.CardBg,
        };
        var header = new Label
        {
            Text = title,
            Font = Theme.Bold(13F),
            ForeColor = Theme.TitleText,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10),
        };
        stack.Controls.Add(header);
        foreach (var row in rows) stack.Controls.Add(row);
        section.Controls.Add(stack);
        return section;
    }

    private static FlowLayoutPanel NewRow() => new()
    {
        FlowDirection = FlowDirection.LeftToRight,
        WrapContents = true,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Margin = new Padding(0, 0, 0, 8),
        MaximumSize = new Size(980, 0),
        BackColor = Theme.CardBg,
    };

    private Button CreateActionButton(string text, string tooltip, EventHandler onClick, Theme.ButtonKind kind = Theme.ButtonKind.Secondary)
    {
        var btn = Theme.CreateButton(text, kind);
        btn.Click += onClick;
        _toolTip.SetToolTip(btn, tooltip);
        return btn;
    }

    private static void StyleCombo(ComboBox combo)
    {
        combo.BackColor = Theme.CardBg;
        combo.ForeColor = Theme.TitleText;
        combo.FlatStyle = FlatStyle.Flat;
        combo.Font = Theme.Regular(9F);
        combo.Margin = new Padding(0, 0, 8, 8);
    }

    private static void StyleTextBox(TextBox box)
    {
        box.BackColor = Theme.CardBg;
        box.ForeColor = Theme.TitleText;
        box.Font = Theme.Regular(9F);
        box.BorderStyle = BorderStyle.FixedSingle;
        box.Margin = new Padding(0, 6, 8, 8);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _keepAlive.Stop();
        base.OnFormClosing(e);
    }
}
