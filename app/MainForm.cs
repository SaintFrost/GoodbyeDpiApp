using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace GoodbyeDpiApp;

sealed class MainForm : Form
{
    const string RepoUrl = "https://github.com/cagritaskn/GoodbyeDPI-Turkey";

    static readonly Color Green = Color.FromArgb(34, 148, 83);
    static readonly Color Amber = Color.FromArgb(214, 145, 20);
    static readonly Color Gray = Color.FromArgb(128, 133, 141);
    static readonly Color Red = Color.FromArgb(196, 52, 43);

    static readonly bool IsAdmin =
        new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

    readonly DpiProcess _process = new();
    readonly Settings _settings = Settings.Load();
    readonly System.Windows.Forms.Timer _timer = new() { Interval = 2000 };
    readonly Dictionary<Color, Icon> _trayIcons = [];

    readonly Label _dot = new()
    {
        Text = "●", AutoSize = true, Font = new Font("Segoe UI", 22F),
        Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 10, 0),
    };
    readonly Label _statusTitle = new()
    {
        AutoSize = true, Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 13F), Margin = new Padding(0, 4, 0, 0),
    };
    readonly Label _statusDetail = Note();
    readonly ComboBox _preset = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 340, Margin = new Padding(0, 3, 0, 3) };
    readonly Label _presetInfo = Note();
    readonly TextBox _args = new()
    {
        Multiline = true, Height = 42, Dock = DockStyle.Fill, Font = new Font("Consolas", 9.5F), Margin = new Padding(0, 3, 0, 3),
    };
    readonly Label _dnsNote = Note(
        "⚠ Bu yöntem DNS yönlendirmesi yapmaz. Windows ağ ayarlarından DNS sunucunuzu " +
        "77.88.8.8 / 77.88.8.1 (Yandex) veya 1.1.1.1 / 1.0.0.1 (Cloudflare) yapın.");
    readonly Label _testNet = Note("—");
    readonly Label _testDiscord = Note("—");
    readonly TextBox _log = new()
    {
        Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill,
        Font = new Font("Consolas", 9F), BackColor = SystemColors.Window, MaxLength = 0,
    };
    readonly Button _start, _stop, _install, _remove, _test;
    readonly NotifyIcon _tray;
    readonly ToolStripMenuItem _trayStop;

    bool _busy, _exiting, _trayHintShown;
    string _busyText = "", _runningPreset = "";

    public MainForm()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9F);
        Text = "GoodbyeDPI Türkiye";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        ClientSize = new Size(640, 720);
        MinimumSize = new Size(560, 640);
        StartPosition = FormStartPosition.CenterScreen;

        _start = MakeButton("Başlat", (_, _) => OnStart());
        _stop = MakeButton("Durdur", (_, _) => OnStop());
        _install = MakeButton("Hizmet olarak kur", (_, _) => OnInstall());
        _remove = MakeButton("Hizmeti kaldır", (_, _) => OnRemove());
        _test = MakeButton("Bağlantıyı test et", (_, _) => OnTest());
        _test.Margin = new Padding(0, 3, 16, 3);

        var header = Grid(SizeType.AutoSize, SizeType.Percent);
        header.Controls.Add(_dot, 0, 0);
        header.SetRowSpan(_dot, 2);
        header.Controls.Add(_statusTitle, 1, 0);
        header.Controls.Add(_statusDetail, 1, 1);

        var banner = Grid(SizeType.Percent, SizeType.AutoSize);
        banner.BackColor = Color.FromArgb(253, 237, 235);
        banner.Padding = new Padding(8);
        banner.Margin = new Padding(0, 10, 0, 0);
        banner.Visible = !IsAdmin;
        var bannerText = Note("Yönetici yetkisi yok. GoodbyeDPI'ı başlatmak ve hizmet kurmak için uygulamayı yönetici olarak açın.");
        bannerText.TextAlign = ContentAlignment.MiddleLeft;
        banner.Controls.Add(bannerText, 0, 0);
        banner.Controls.Add(MakeButton("Yönetici olarak aç", (_, _) => RestartAsAdmin()), 1, 0);

        _preset.Items.AddRange(Presets.All);
        var method = Grid(SizeType.AutoSize, SizeType.Percent);
        method.Controls.Add(_preset, 0, 0);
        method.SetColumnSpan(_preset, 2);
        method.Controls.Add(_presetInfo, 0, 1);
        method.SetColumnSpan(_presetInfo, 2);
        method.Controls.Add(new Label { Text = "Parametreler:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 3, 8, 3) }, 0, 2);
        method.Controls.Add(_args, 1, 2);
        _dnsNote.ForeColor = Color.FromArgb(150, 88, 0);
        method.Controls.Add(_dnsNote, 0, 3);
        method.SetColumnSpan(_dnsNote, 2);

        var run = Grid(SizeType.Percent, SizeType.AutoSize, SizeType.AutoSize);
        run.Controls.Add(RowLabel("Geçici oturum — uygulama açık kaldığı sürece çalışır."), 0, 0);
        run.Controls.Add(_start, 1, 0);
        run.Controls.Add(_stop, 2, 0);
        run.Controls.Add(RowLabel("Windows hizmeti — bilgisayar her açıldığında otomatik başlar."), 0, 1);
        run.Controls.Add(_install, 1, 1);
        run.Controls.Add(_remove, 2, 1);

        var test = Grid(SizeType.AutoSize, SizeType.AutoSize, SizeType.Percent);
        test.Controls.Add(_test, 0, 0);
        test.SetRowSpan(_test, 2);
        test.Controls.Add(RowLabel("İnternet:"), 1, 0);
        test.Controls.Add(_testNet, 2, 0);
        test.Controls.Add(RowLabel("discord.com:"), 1, 1);
        test.Controls.Add(_testDiscord, 2, 1);
        _testNet.TextAlign = _testDiscord.TextAlign = ContentAlignment.MiddleLeft;

        string version = typeof(MainForm).Assembly.GetName().Version!.ToString(3);
        var footer = new LinkLabel
        {
            Text = $"v{version} · Kaynak: github.com/cagritaskn/GoodbyeDPI-Turkey",
            AutoSize = true, Margin = new Padding(0, 8, 0, 0), LinkColor = Gray,
        };
        footer.LinkArea = new LinkArea(footer.Text.IndexOf("github", StringComparison.Ordinal), footer.Text.Length);
        footer.LinkClicked += (_, _) => Process.Start(new ProcessStartInfo(RepoUrl) { UseShellExecute = true });

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(16, 12, 16, 12) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        void Row(Control control, float percent = 0)
        {
            root.RowStyles.Add(percent > 0 ? new RowStyle(SizeType.Percent, percent) : new RowStyle(SizeType.AutoSize));
            root.RowCount = root.RowStyles.Count;
            root.Controls.Add(control, 0, root.RowCount - 1);
        }
        Row(header);
        Row(banner);
        Row(Section("Yöntem"));
        Row(method);
        Row(Section("Çalıştır"));
        Row(run);
        Row(Section("Bağlantı testi"));
        Row(test);
        Row(Section("Kayıt"));
        Row(_log, 100);
        Row(footer);
        Controls.Add(root);

        _trayStop = new ToolStripMenuItem("Geçici oturumu durdur", null, (_, _) => OnStop());
        var menu = new ContextMenuStrip();
        menu.Items.Add("Göster", null, (_, _) => ShowFromTray());
        menu.Items.Add(_trayStop);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Çıkış", null, (_, _) => ExitApp());
        _tray = new NotifyIcon { Text = Text, Icon = TrayIcon(Gray), ContextMenuStrip = menu, Visible = true };
        _tray.DoubleClick += (_, _) => ShowFromTray();

        _preset.SelectedIndexChanged += (_, _) => OnPresetChanged();
        _args.TextChanged += (_, _) => _dnsNote.Visible = !_args.Text.Contains("--dns-addr");
        _timer.Tick += (_, _) => RefreshStatus();
        _process.Output += line => Log(line);
        _process.Exited += code => Post(() =>
        {
            Log($"goodbyedpi.exe kapandı (çıkış kodu {code}).");
            RefreshStatus();
        });

        ResumeLayout(true);
    }

    Preset SelectedPreset => (Preset)_preset.SelectedItem!;

    // The box wraps across lines, so pasted line breaks are just separators.
    string Arguments => _args.Text.ReplaceLineEndings(" ").Trim();

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Log($"GoodbyeDPI Türkiye v{typeof(MainForm).Assembly.GetName().Version!.ToString(3)}");
        if (!IsAdmin)
            Log("Yönetici yetkisi yok: başlatma ve hizmet işlemleri kapalı.");
        if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
            Log("Uyarı: ARM tabanlı Windows'ta WinDivert sürücüsü çalışmaz.");
        if (IsProcessRunning("avp"))
            Log("Uyarı: Kaspersky çalışıyor. Kaspersky GoodbyeDPI'ın çalışmasını engelliyor; kaldırmanız gerekebilir.");
        SelectInitialPreset();
        RefreshStatus();
        _timer.Start();
    }

    void SelectInitialPreset()
    {
        if (DpiService.ReadCommandLine() is { } service)
        {
            Log($"Kurulu hizmet: \"{service.Exe}\" {service.Args}");
            if (!string.Equals(Path.GetFullPath(service.Exe), Binaries.ExePath, StringComparison.OrdinalIgnoreCase))
                Log("Not: Hizmet bu uygulamanın dışındaki bir klasörden çalışıyor. \"Hizmeti güncelle\" ile bu uygulamaya taşıyabilirsiniz.");
            SelectPreset(Presets.Match(service.Args), service.Args);
        }
        else
        {
            var saved = Presets.All.FirstOrDefault(p => p.Name == _settings.Preset) ?? Presets.All[0];
            SelectPreset(saved, _settings.CustomArgs);
        }
    }

    void SelectPreset(Preset preset, string? customArgs)
    {
        _preset.SelectedItem = preset;
        if (preset == Presets.Custom && customArgs != null)
            _args.Text = customArgs;
    }

    void OnPresetChanged()
    {
        var preset = SelectedPreset;
        _presetInfo.Text = preset.Description;
        bool custom = preset == Presets.Custom;
        _args.ReadOnly = !custom;
        // Switching to custom keeps the previous preset's arguments as a starting point.
        if (!custom)
            _args.Text = preset.Args;
    }

    void RefreshStatus()
    {
        var (serviceState, servicePid) = DpiService.Query();
        bool temporary = _process.IsRunning;
        int[] external = OtherInstances(servicePid);

        Color color;
        string title, detail;
        if (_busy)
            (color, title, detail) = (Amber, "İşlem sürüyor…", _busyText);
        else if (serviceState == ServiceState.Running)
            (color, title, detail) = (Green, "Aktif — Windows hizmeti",
                $"Yöntem: {ServicePresetName()}. Bilgisayar açıldığında otomatik başlar.");
        else if (serviceState is ServiceState.Starting or ServiceState.Stopping)
            (color, title, detail) = (Amber, serviceState == ServiceState.Starting ? "Hizmet başlatılıyor…" : "Hizmet durduruluyor…", "");
        else if (temporary)
            (color, title, detail) = (Green, "Aktif — geçici oturum", $"Yöntem: {_runningPreset}. Uygulamadan çıkınca durur.");
        else if (external.Length > 0)
            (color, title, detail) = (Green, "Aktif — harici işlem",
                $"Bu uygulama dışında başlatılmış goodbyedpi.exe çalışıyor (PID {string.Join(", ", external)}).");
        else
            (color, title, detail) = (Gray, "Kapalı",
                serviceState == ServiceState.Stopped ? "GoodbyeDPI hizmeti kurulu ama durdurulmuş." : "GoodbyeDPI çalışmıyor.");

        _dot.ForeColor = color;
        SetText(_statusTitle, title);
        SetText(_statusDetail, detail);
        var icon = TrayIcon(color);
        if (_tray.Icon != icon)
            _tray.Icon = icon;
        string trayText = $"GoodbyeDPI — {title}";
        _tray.Text = trayText.Length > 127 ? trayText[..127] : trayText;

        bool serviceActive = serviceState is ServiceState.Running or ServiceState.Starting or ServiceState.Stopping;
        bool ready = IsAdmin && !_busy;
        _start.Enabled = ready && !temporary && !serviceActive && external.Length == 0;
        _stop.Enabled = _trayStop.Enabled = ready && (temporary || external.Length > 0);
        _install.Enabled = ready;
        _remove.Enabled = ready && serviceState is not (ServiceState.NotInstalled or ServiceState.Unknown);
        SetText(_install, serviceState == ServiceState.NotInstalled ? "Hizmet olarak kur" : "Hizmeti güncelle");
        _preset.Enabled = _args.Enabled = !temporary && !_busy;
    }

    async void OnStart()
    {
        string args = Arguments;
        await RunBusy("Geçici oturum başlatılıyor…", async () =>
        {
            string exe = await Task.Run(() => Binaries.Prepare(Log));
            Log($"> goodbyedpi.exe {args}");
            _runningPreset = SelectedPreset.Name;
            _process.Start(exe, args);
            SaveSettings();

            await Task.Delay(1500);
            if (!_process.IsRunning)
                throw new InvalidOperationException("goodbyedpi.exe hemen kapandı. Ayrıntılar için kayda bakın.");
            DnsFlushResolverCache();
            Log("Geçici oturum başladı.");
        });
    }

    async void OnStop() => await RunBusy("Durduruluyor…", StopLocalInstancesAsync);

    /// <summary>Stops the temporary session and any goodbyedpi.exe started outside this app (not the service).</summary>
    async Task StopLocalInstancesAsync()
    {
        if (_process.IsRunning)
            await Task.Run(_process.Stop);
        foreach (int pid in OtherInstances(DpiService.Query().Pid))
        {
            try
            {
                using var process = Process.GetProcessById(pid);
                process.Kill();
                await process.WaitForExitAsync();
                Log($"Harici goodbyedpi.exe (PID {pid}) durduruldu.");
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or Win32Exception)
            {
                Log($"PID {pid} durdurulamadı: {ex.Message}");
            }
        }
    }

    async void OnInstall()
    {
        string args = Arguments;
        var answer = MessageBox.Show(this,
            $"GoodbyeDPI \"{SelectedPreset.Name}\" yöntemiyle Windows hizmeti olarak kurulacak ve bilgisayar her açıldığında otomatik başlayacak.\n\n" +
            "Varsa mevcut GoodbyeDPI hizmeti bununla değiştirilecek. Devam edilsin mi?",
            Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer != DialogResult.Yes)
            return;

        await RunBusy("Hizmet kuruluyor…", async () =>
        {
            await StopLocalInstancesAsync();
            await DpiService.RemoveAsync(Log, removeDriver: false);
            string exe = await Task.Run(() => Binaries.Prepare(Log));
            await DpiService.InstallAsync(exe, args, Log);
            SaveSettings();
            DnsFlushResolverCache();
            Log("Hizmet kuruldu ve başlatıldı.");
        });
    }

    async void OnRemove()
    {
        var answer = MessageBox.Show(this,
            "GoodbyeDPI hizmeti kaldırılacak ve engelli sitelere erişim duracak. Devam edilsin mi?",
            Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer != DialogResult.Yes)
            return;

        await RunBusy("Hizmet kaldırılıyor…", async () =>
        {
            // Unloading WinDivert would break a goodbyedpi.exe that is still running outside the service.
            bool driverInUse = _process.IsRunning || OtherInstances(DpiService.Query().Pid).Length > 0;
            await DpiService.RemoveAsync(Log, removeDriver: !driverInUse);
            Log(DpiService.Query().State == ServiceState.NotInstalled
                ? "Hizmet kaldırıldı."
                : "Hizmet silinmek üzere işaretlendi; bilgisayar yeniden başlatılınca tamamen kalkacak.");
        });
    }

    async void OnTest()
    {
        _test.Enabled = false;
        try
        {
            // Otherwise Windows keeps answering from DNS replies cached before GoodbyeDPI started.
            DnsFlushResolverCache();
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            await Task.WhenAll(
                Probe(http, _testNet, "https://www.google.com/generate_204"),
                Probe(http, _testDiscord, "https://discord.com/api/v9/gateway"));
        }
        finally
        {
            _test.Enabled = true;
        }
    }

    async Task Probe(HttpClient http, Label result, string url)
    {
        result.ForeColor = Gray;
        result.Text = "Deneniyor…";
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Any HTTP response, even an error status, means the site was reachable.
            using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            result.ForeColor = Green;
            result.Text = $"✓ Erişilebilir ({stopwatch.ElapsedMilliseconds} ms)";
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            result.ForeColor = Red;
            result.Text = "✗ Erişilemedi";
            Log($"Test {new Uri(url).Host}: {ex.GetBaseException().Message}");
        }
    }

    async Task RunBusy(string text, Func<Task> action)
    {
        _busy = true;
        _busyText = text;
        RefreshStatus();
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Log("Hata: " + ex.Message);
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _busy = false;
            RefreshStatus();
        }
    }

    int[] OtherInstances(int servicePid)
    {
        var ids = new List<int>();
        foreach (var process in Process.GetProcessesByName("goodbyedpi"))
            using (process)
                if (process.Id != servicePid && !_process.Owns(process.Id))
                    ids.Add(process.Id);
        return [.. ids];
    }

    static string ServicePresetName()
    {
        if (DpiService.ReadCommandLine() is not { } service)
            return "bilinmiyor";
        var preset = Presets.Match(service.Args);
        return preset == Presets.Custom ? "özel parametreler" : preset.Name;
    }

    void SaveSettings()
    {
        _settings.Preset = SelectedPreset.Name;
        if (SelectedPreset == Presets.Custom)
            _settings.CustomArgs = Arguments;
        _settings.Save();
    }

    void RestartAsAdmin()
    {
        try
        {
            Process.Start(new ProcessStartInfo(Application.ExecutablePath) { UseShellExecute = true, Verb = "runas" });
        }
        catch (Win32Exception)
        {
            return; // UAC prompt was cancelled
        }
        ExitApp();
    }

    void ShowFromTray()
    {
        Show();
        if (WindowState == FormWindowState.Minimized)
            WindowState = FormWindowState.Normal;
        Activate();
    }

    void ExitApp()
    {
        _exiting = true;
        Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_busy && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            return;
        }
        if (!_exiting && e.CloseReason == CloseReason.UserClosing && _process.IsRunning)
        {
            e.Cancel = true;
            Hide();
            if (!_trayHintShown)
            {
                _trayHintShown = true;
                _tray.ShowBalloonTip(4000, "GoodbyeDPI çalışmaya devam ediyor",
                    "Durdurmak ya da çıkmak için sistem tepsisindeki simgeye sağ tıklayın.", ToolTipIcon.Info);
            }
            return;
        }
        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer.Stop();
        _process.Stop();
        SaveSettings();
        _tray.Visible = false;
        _tray.Dispose();
        base.OnFormClosed(e);
    }

    void Log(string line)
    {
        if (InvokeRequired)
        {
            Post(() => Log(line));
            return;
        }
        _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {line}{Environment.NewLine}");
    }

    void Post(Action action)
    {
        try
        {
            if (IsHandleCreated && !IsDisposed)
                BeginInvoke(action);
        }
        catch (InvalidOperationException)
        {
            // The window closed while a background event was arriving.
        }
    }

    Icon TrayIcon(Color color)
    {
        if (!_trayIcons.TryGetValue(color, out var icon))
            _trayIcons[color] = icon = AppIcon.Create(color, SystemInformation.SmallIconSize.Width);
        return icon;
    }

    static bool IsProcessRunning(string name)
    {
        var processes = Process.GetProcessesByName(name);
        foreach (var process in processes)
            process.Dispose();
        return processes.Length > 0;
    }

    static void SetText(Control control, string text)
    {
        if (control.Text != text)
            control.Text = text;
    }

    static TableLayoutPanel Grid(params SizeType[] columns)
    {
        var grid = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = columns.Length, Margin = new Padding(0) };
        foreach (var column in columns)
            grid.ColumnStyles.Add(column == SizeType.Percent ? new ColumnStyle(SizeType.Percent, 100) : new ColumnStyle(column));
        return grid;
    }

    static Label Section(string text) => new()
    {
        Text = text, AutoSize = true, Font = new Font("Segoe UI Semibold", 10F), Margin = new Padding(0, 14, 0, 4),
    };

    static Label Note(string text = "") => new() { Text = text, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 3, 0, 3) };

    static Label RowLabel(string text)
    {
        var label = Note(text);
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.Margin = new Padding(0, 3, 8, 3);
        return label;
    }

    static Button MakeButton(string text, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text, AutoSize = true, MinimumSize = new Size(120, 30),
            Margin = new Padding(6, 3, 0, 3), UseVisualStyleBackColor = true,
        };
        button.Click += onClick;
        return button;
    }

    [DllImport("dnsapi.dll")]
    static extern bool DnsFlushResolverCache();

#if DEBUG
    /// <summary>Renders the window to a PNG without showing it, for checking the layout.</summary>
    public static void SaveScreenshot(string path, int presetIndex)
    {
        using var form = new MainForm { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false };
        form.Show();
        Application.DoEvents();
        if (presetIndex >= 0)
        {
            form._preset.SelectedIndex = presetIndex;
            Application.DoEvents();
        }
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
        bitmap.Save(path);
        form.ExitApp();
    }
#endif
}
