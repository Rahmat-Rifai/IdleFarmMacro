using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace IdleFarmMacro;

public partial class Form1 : Form
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    // Minimalist Dark Palette (Zinc Neutral)
    private static readonly Color BgMain = Color.FromArgb(24, 24, 27);         // #18181b
    private static readonly Color BgSurface = Color.FromArgb(39, 39, 42);      // #27272a
    private static readonly Color BgHover = Color.FromArgb(63, 63, 70);        // #3f3f46
    private static readonly Color BorderColor = Color.FromArgb(63, 63, 70);    // #3f3f46
    private static readonly Color TextPrimary = Color.FromArgb(244, 244, 245);  // #f4f4f5
    private static readonly Color TextMuted = Color.FromArgb(161, 161, 170);    // #a1a1aa
    private static readonly Color AccentBlue = Color.FromArgb(37, 99, 235);     // #2563eb
    private static readonly Color AccentRed = Color.FromArgb(220, 38, 38);      // #dc2626
    private static readonly Color AccentHighlight = Color.FromArgb(96, 165, 250); // #60a5fa
    private static readonly Color StatusSuccess = Color.FromArgb(74, 222, 128); // #4ade80 soft green
    private static readonly Color StatusWarning = Color.FromArgb(250, 204, 21); // #facc15 soft yellow
    private static readonly Color StatusDanger = Color.FromArgb(248, 113, 113); // #f87171 soft red

    private const string DefaultDiscordUrl = "https://discord.com/app";

    private Panel pnlHeader = null!;
    private FlowLayoutPanel pnlLeft = null!;
    private Label lblGuru = null!;
    private Button btnGuruLegendary = null!;
    private Button btnGuruMythic = null!;
    private string selectedGuruDice = "legendary"; // "legendary" or "mythic"

    private Button btnStart = null!;
    private CheckBox chkLoop = null!;
    private Label lblDelay = null!;
    private TextBox txtDelay = null!;
    private Label lblDelaySec = null!;

    private CheckBox chkPreCommand = null!;
    private Button btnPreCommandMode = null!;
    private TextBox txtPreCommand = null!;
    private Button btnManagePreCommands = null!;
    private readonly ToolTip toolTip = new();
    private readonly object preCommandsLock = new();
    private List<string> preCommandsList = new() { "idle use time speeder 6", "idle use time compressor 3" };
    private string preCommandMode = "rotate"; // "rotate" or "all"
    private int preCommandDelayMs = 800;
    private bool isUpdatingPreCommandText = false;

    private static readonly string ConfigFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "IdleFarmMacro", "config.json"
    );

    private class MacroConfig
    {
        public string GuruDice { get; set; } = "legendary";
        public bool Loop { get; set; } = true;
        public string Delay { get; set; } = "3";
        public bool PreCommandEnabled { get; set; } = true;
        public string PreCommandText { get; set; } = "idle use time speeder 6; idle use time compressor 3";
        public List<string> PreCommands { get; set; } = new() { "idle use time speeder 6", "idle use time compressor 3" };
        public string PreCommandMode { get; set; } = "rotate";
        public int PreCommandDelayMs { get; set; } = 800;
    }

    private Panel pnlRight = null!;
    private Button btnReload = null!;

    private Panel pnlStatus = null!;
    private Label lblStatus = null!;

    private WebView2 webView = null!;
    private bool isRunning = false;
    private int cycleCount = 0;

    public Form1()
    {
        InitializeComponent();
        ApplyImmersiveDarkMode();
        InitializeCustomComponents();
        InitWebViewAsync();
    }

    private void ApplyImmersiveDarkMode()
    {
        try
        {
            int darkMode = 1;
            DwmSetWindowAttribute(this.Handle, 20, ref darkMode, sizeof(int));
        }
        catch { }
    }

    private void InitializeCustomComponents()
    {
        this.Text = "Idle Farm Macro";
        this.Size = new Size(1100, 750);
        this.MinimumSize = new Size(960, 480);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = BgMain;
        this.ForeColor = TextPrimary;
        this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

        // --- Top Bar ---
        pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = BgMain
        };
        pnlHeader.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderColor, 1);
            e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
        };

        pnlLeft = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(12, 8, 0, 8),
            BackColor = Color.Transparent
        };

        // Guru Label
        lblGuru = new Label
        {
            Text = "Guru dice:",
            ForeColor = TextMuted,
            AutoSize = true,
            Margin = new Padding(0, 5, 6, 0)
        };

        // Segmented Guru dice buttons: Legendary | Mythic
        btnGuruLegendary = CreateSegmentButton("Legendary", true);
        btnGuruMythic = CreateSegmentButton("Mythic", false);

        btnGuruLegendary.Click += (s, e) =>
        {
            selectedGuruDice = "legendary";
            UpdateGuruSegmentState();
        };

        btnGuruMythic.Click += (s, e) =>
        {
            selectedGuruDice = "mythic";
            UpdateGuruSegmentState();
        };

        // Action: Start / Stop Button
        btnStart = new Button
        {
            Text = "Start",
            BackColor = AccentBlue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(68, 27),
            Margin = new Padding(12, 1, 10, 0),
            Cursor = Cursors.Hand
        };
        btnStart.FlatAppearance.BorderSize = 0;
        btnStart.Click += async (s, e) => await OnStartStopClickedAsync();

        // Loop Checkbox
        chkLoop = new CheckBox
        {
            Text = "Loop terus",
            Checked = true,
            ForeColor = TextPrimary,
            AutoSize = true,
            Margin = new Padding(0, 5, 12, 0),
            Cursor = Cursors.Hand
        };

        // Jeda Label & Input
        lblDelay = new Label
        {
            Text = "Jeda:",
            ForeColor = TextMuted,
            AutoSize = true,
            Margin = new Padding(0, 5, 4, 0)
        };

        txtDelay = new TextBox
        {
            Text = "3",
            BackColor = BgSurface,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Width = 36,
            TextAlign = HorizontalAlignment.Center,
            Margin = new Padding(0, 3, 4, 0)
        };

        lblDelaySec = new Label
        {
            Text = "detik",
            ForeColor = TextMuted,
            AutoSize = true,
            Margin = new Padding(0, 5, 0, 0)
        };

        // Custom Pre-command (sebelum eksekusi claim muffin)
        chkPreCommand = new CheckBox
        {
            Text = "Sebelum klaim:",
            Checked = true,
            ForeColor = TextPrimary,
            AutoSize = true,
            Margin = new Padding(10, 5, 4, 0),
            Cursor = Cursors.Hand
        };
        chkPreCommand.CheckedChanged += (s, e) =>
        {
            UpdatePreCommandUI();
            SaveConfig();
        };

        // Tombol Toggle Mode: 🔄 Gantian vs 📑 Semua
        btnPreCommandMode = new Button
        {
            Text = "🔄 Gantian",
            BackColor = BgSurface,
            ForeColor = AccentHighlight,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(82, 27),
            Margin = new Padding(0, 1, 4, 0),
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
        };
        btnPreCommandMode.FlatAppearance.BorderSize = 1;
        btnPreCommandMode.FlatAppearance.BorderColor = BorderColor;
        btnPreCommandMode.Click += (s, e) =>
        {
            lock (preCommandsLock)
            {
                preCommandMode = (preCommandMode == "rotate") ? "all" : "rotate";
            }
            UpdatePreCommandUI();
            SaveConfig();
        };

        txtPreCommand = new TextBox
        {
            Text = "idle use time speeder 6; idle use time compressor 3",
            BackColor = BgSurface,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Width = 190,
            Margin = new Padding(0, 3, 4, 0)
        };
        txtPreCommand.DoubleClick += (s, e) => OpenPreCommandsDialog();
        txtPreCommand.TextChanged += (s, e) =>
        {
            if (isUpdatingPreCommandText) return;

            string text = txtPreCommand.Text.Trim();
            if (!string.IsNullOrWhiteSpace(text) && !chkPreCommand.Checked)
            {
                chkPreCommand.Checked = true;
            }

            var parsed = text.Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                             .Where(c => !string.IsNullOrWhiteSpace(c))
                             .ToList();

            lock (preCommandsLock)
            {
                preCommandsList = parsed;
            }
            UpdatePreCommandUI();
        };

        btnManagePreCommands = new Button
        {
            Text = "⚙️ Atur (2)",
            BackColor = BgSurface,
            ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(82, 27),
            Margin = new Padding(0, 1, 10, 0),
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 8.5f)
        };
        btnManagePreCommands.FlatAppearance.BorderSize = 1;
        btnManagePreCommands.FlatAppearance.BorderColor = BorderColor;
        btnManagePreCommands.Click += (s, e) => OpenPreCommandsDialog();

        toolTip.SetToolTip(chkPreCommand, "Centang untuk mengirim perintah otomatis sebelum klaim 'idle use valentine muffin'.");
        toolTip.SetToolTip(txtPreCommand, "Perintah sebelum klaim muffin (pisahkan ';' untuk daftar perintah). Klik 2x atau klik '⚙️ Atur' untuk membuka daftar & preset.");
        toolTip.SetToolTip(btnManagePreCommands, "Buka daftar pesan kustom (tambah pesan sepuasnya, preset cepat Speeder & Compressor, mode rotasi/gantian).");

        pnlLeft.Controls.AddRange(new Control[] {
            lblGuru, btnGuruLegendary, btnGuruMythic,
            btnStart, chkLoop,
            lblDelay, txtDelay, lblDelaySec,
            chkPreCommand, btnPreCommandMode, txtPreCommand, btnManagePreCommands
        });

        // Right side: Reload
        pnlRight = new Panel
        {
            Dock = DockStyle.Right,
            Width = 80,
            Padding = new Padding(0, 8, 12, 8),
            BackColor = Color.Transparent
        };

        btnReload = new Button
        {
            Text = "Reload",
            BackColor = BgSurface,
            ForeColor = TextMuted,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(64, 27),
            Location = new Point(4, 8),
            Cursor = Cursors.Hand
        };
        btnReload.FlatAppearance.BorderSize = 1;
        btnReload.FlatAppearance.BorderColor = BorderColor;
        btnReload.Click += (s, e) => webView?.Reload();
        pnlRight.Controls.Add(btnReload);

        pnlHeader.Controls.Add(pnlLeft);
        pnlHeader.Controls.Add(pnlRight);

        // --- Bottom Status Bar ---
        pnlStatus = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 24,
            BackColor = BgMain
        };
        pnlStatus.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderColor, 1);
            e.Graphics.DrawLine(pen, 0, 0, pnlStatus.Width, 0);
        };

        lblStatus = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Memuat browser...",
            ForeColor = TextMuted,
            Padding = new Padding(12, 4, 12, 0),
            UseMnemonic = false
        };
        pnlStatus.Controls.Add(lblStatus);

        // --- Center WebView2 ---
        webView = new WebView2
        {
            Dock = DockStyle.Fill
        };

        this.Controls.Add(webView);
        this.Controls.Add(pnlHeader);
        this.Controls.Add(pnlStatus);

        LoadConfig();
    }

    private Button CreateSegmentButton(string text, bool isActive)
    {
        var btn = new Button
        {
            Text = text,
            Height = 27,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 1, 2, 0),
            Font = new Font("Segoe UI", 9f, FontStyle.Regular)
        };
        btn.FlatAppearance.BorderSize = 1;
        ApplySegmentStyle(btn, isActive);
        return btn;
    }

    private void ApplySegmentStyle(Button btn, bool isActive)
    {
        if (isActive)
        {
            btn.BackColor = AccentBlue;
            btn.ForeColor = Color.White;
            btn.FlatAppearance.BorderColor = AccentBlue;
        }
        else
        {
            btn.BackColor = BgSurface;
            btn.ForeColor = TextMuted;
            btn.FlatAppearance.BorderColor = BorderColor;
        }
    }

    private void UpdateGuruSegmentState()
    {
        ApplySegmentStyle(btnGuruLegendary, selectedGuruDice == "legendary");
        ApplySegmentStyle(btnGuruMythic, selectedGuruDice == "mythic");
    }

    private async void InitWebViewAsync()
    {
        try
        {
            string profileDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "IdleFarmMacro", "UserData"
            );
            Directory.CreateDirectory(profileDir);

            // Optimasi argumen Chromium: cegah memory leak & driver GPU crash di laptop Nitro 5
            var options = new CoreWebView2EnvironmentOptions(
                "--disable-background-timer-throttling " +
                "--disable-renderer-backgrounding " +
                "--disable-features=CalculateNativeWinOcclusion " +
                "--renderer-process-limit=4"
            );

            var env = await CoreWebView2Environment.CreateAsync(null, profileDir, options);
            await webView.EnsureCoreWebView2Async(env);

            webView.Source = new Uri(DefaultDiscordUrl);
            UpdateStatus("Siap. Membuka channel Discord...", TextMuted);
        }
        catch (Exception ex)
        {
            UpdateStatus($"Gagal memuat browser: {ex.Message}", StatusDanger);
        }
    }

    private void UpdateStatus(string message, Color? color = null)
    {
        if (lblStatus.InvokeRequired)
        {
            lblStatus.Invoke(() => UpdateStatus(message, color));
            return;
        }
        lblStatus.Text = message;
        lblStatus.ForeColor = color ?? TextMuted;
    }

    private async Task OnStartStopClickedAsync()
    {
        if (isRunning)
        {
            isRunning = false;
            btnStart.Text = "Start";
            btnStart.BackColor = AccentBlue;
            UpdateStatus("Menghentikan loop macro...", StatusWarning);
            return;
        }

        if (webView?.CoreWebView2 == null)
        {
            MessageBox.Show("Browser belum siap.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SaveConfig();
        isRunning = true;
        btnStart.Text = "Stop";
        btnStart.BackColor = AccentRed;
        cycleCount = 0;

        try
        {
            while (isRunning)
            {
                cycleCount++;
                bool success = await ExecuteSingleCycleAsync(cycleCount);
                if (!success || !isRunning) break;

                if (!chkLoop.Checked)
                {
                    // Hanya 1x klaim jika Loop tidak dicentang
                    break;
                }

                // Ambil delay dari input (default 3 detik)
                int delaySec = 3;
                if (!int.TryParse(txtDelay.Text.Trim(), out delaySec) || delaySec < 1)
                {
                    delaySec = 3;
                }

                UpdateStatus($"Klaim #{cycleCount} selesai. Jeda {delaySec}s sebelum klaim berikutnya...", StatusSuccess);
                for (int i = 0; i < delaySec * 10; i++)
                {
                    if (!isRunning) break;
                    await Task.Delay(100);
                }
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error loop: {ex.Message}", StatusDanger);
        }
        finally
        {
            isRunning = false;
            btnStart.Text = "Start";
            btnStart.BackColor = AccentBlue;
        }
    }

    private void UpdatePreCommandUI()
    {
        if (this.InvokeRequired)
        {
            this.Invoke(UpdatePreCommandUI);
            return;
        }

        int count;
        string mode;
        lock (preCommandsLock)
        {
            count = preCommandsList.Count;
            mode = preCommandMode;
        }

        bool enabled = chkPreCommand.Checked;
        bool isRotate = (mode == "rotate");

        btnPreCommandMode.Text = isRotate ? "🔄 Gantian" : "📑 Semua";
        btnPreCommandMode.ForeColor = enabled
            ? (isRotate ? AccentHighlight : TextPrimary)
            : TextMuted;
        btnPreCommandMode.Enabled = enabled;

        toolTip.SetToolTip(btnPreCommandMode, isRotate
            ? "Mode Gantian: Mengirim 1 item bergantian tiap siklus (misal: Speeder 6 lalu Compressor 3). Klik untuk ubah ke 'Kirim Semua'."
            : "Mode Kirim Semua: Mengirim semua item berurutan di tiap siklus sebelum klaim. Klik untuk ubah ke 'Gantian'.");

        btnManagePreCommands.Text = $"⚙️ Atur ({count})";
        btnManagePreCommands.ForeColor = enabled ? TextPrimary : TextMuted;
        btnManagePreCommands.Enabled = enabled;

        txtPreCommand.ForeColor = enabled ? TextPrimary : TextMuted;
    }

    private void OpenPreCommandsDialog()
    {
        List<string> currentList;
        string currentMode;
        int currentDelay;
        lock (preCommandsLock)
        {
            currentList = new List<string>(preCommandsList);
            currentMode = preCommandMode;
            currentDelay = preCommandDelayMs;
        }

        using var dlg = new PreCommandsForm(currentList, currentMode, currentDelay);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            lock (preCommandsLock)
            {
                preCommandsList = new List<string>(dlg.Commands);
                preCommandMode = dlg.ExecutionMode;
                preCommandDelayMs = dlg.CommandDelayMs;
            }

            isUpdatingPreCommandText = true;
            txtPreCommand.Text = string.Join("; ", preCommandsList);
            isUpdatingPreCommandText = false;

            UpdatePreCommandUI();

            if (preCommandsList.Count > 0 && !chkPreCommand.Checked)
            {
                chkPreCommand.Checked = true;
            }

            SaveConfig();
        }
    }

    private List<string> GetActivePreCommands(int cycle)
    {
        lock (preCommandsLock)
        {
            var list = preCommandsList
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .ToList();

            if (list.Count == 0) return new List<string>();

            if (preCommandMode == "rotate")
            {
                int index = (cycle - 1) % list.Count;
                if (index < 0) index = 0;
                return new List<string> { list[index] };
            }

            return list;
        }
    }

    private void LoadConfig()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                string json = File.ReadAllText(ConfigFilePath);
                var config = JsonSerializer.Deserialize<MacroConfig>(json);
                if (config != null)
                {
                    selectedGuruDice = (config.GuruDice == "mythic") ? "mythic" : "legendary";
                    chkLoop.Checked = config.Loop;
                    txtDelay.Text = string.IsNullOrWhiteSpace(config.Delay) ? "3" : config.Delay;
                    chkPreCommand.Checked = config.PreCommandEnabled;

                    preCommandMode = string.IsNullOrWhiteSpace(config.PreCommandMode) ? "rotate" : config.PreCommandMode;
                    preCommandDelayMs = config.PreCommandDelayMs > 0 ? config.PreCommandDelayMs : 800;

                    if (config.PreCommands != null && config.PreCommands.Count > 0)
                    {
                        preCommandsList = new List<string>(config.PreCommands);
                    }
                    else if (!string.IsNullOrWhiteSpace(config.PreCommandText))
                    {
                        preCommandsList = config.PreCommandText
                            .Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .ToList();
                    }
                    else
                    {
                        preCommandsList = new List<string> { "idle use time speeder 6", "idle use time compressor 3" };
                    }

                    isUpdatingPreCommandText = true;
                    txtPreCommand.Text = string.Join("; ", preCommandsList);
                    isUpdatingPreCommandText = false;

                    UpdateGuruSegmentState();
                    UpdatePreCommandUI();
                }
            }
            else
            {
                UpdatePreCommandUI();
            }
        }
        catch
        {
            UpdatePreCommandUI();
        }
    }

    private void SaveConfig()
    {
        try
        {
            List<string> listCopy;
            string modeCopy;
            int delayCopy;
            lock (preCommandsLock)
            {
                listCopy = new List<string>(preCommandsList);
                modeCopy = preCommandMode;
                delayCopy = preCommandDelayMs;
            }

            var config = new MacroConfig
            {
                GuruDice = selectedGuruDice,
                Loop = chkLoop.Checked,
                Delay = txtDelay.Text.Trim(),
                PreCommandEnabled = chkPreCommand.Checked,
                PreCommandText = string.Join("; ", listCopy),
                PreCommands = listCopy,
                PreCommandMode = modeCopy,
                PreCommandDelayMs = delayCopy
            };
            string dir = Path.GetDirectoryName(ConfigFilePath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, json);
        }
        catch { }
    }

    private async Task<bool> SendDiscordCommandAsync(string commandText, int cycle)
    {
        try
        {
            // 1. Fokus kotak chat Discord
            string focusScript = @"
            (function() {
                const box = document.querySelector('[role=""textbox""]');
                if (!box) return { ok: false, err: 'Kotak chat tidak ditemukan.' };
                box.focus();
                const sel = window.getSelection();
                sel.selectAllChildren(box);
                return { ok: true };
            })();";

            string focusResJson = await webView.ExecuteScriptAsync(focusScript);
            if (focusResJson != "null" && !string.IsNullOrWhiteSpace(focusResJson))
            {
                using var doc = JsonDocument.Parse(focusResJson);
                if (doc.RootElement.TryGetProperty("ok", out var okProp) && !okProp.GetBoolean())
                {
                    string err = doc.RootElement.GetProperty("err").GetString() ?? "Kotak chat tidak ditemukan.";
                    UpdateStatus($"[#{cycle}] {err}", StatusDanger);
                    return false;
                }
            }

            await Task.Delay(150);
            if (!isRunning) return false;

            // 2. Ketik perintah menggunakan Chrome DevTools Protocol (Input.insertText)
            string insertParamJson = JsonSerializer.Serialize(new { text = commandText });
            await webView.CoreWebView2.CallDevToolsProtocolMethodAsync(
                "Input.insertText",
                insertParamJson
            );

            await Task.Delay(350);
            if (!isRunning) return false;

            // 3. Tekan Enter menggunakan Chrome DevTools Protocol (isTrusted: true)
            await webView.CoreWebView2.CallDevToolsProtocolMethodAsync(
                "Input.dispatchKeyEvent",
                "{\"type\":\"rawKeyDown\",\"windowsVirtualKeyCode\":13,\"unmodifiedText\":\"\\r\",\"text\":\"\\r\",\"key\":\"Enter\",\"code\":\"Enter\"}"
            );
            await webView.CoreWebView2.CallDevToolsProtocolMethodAsync(
                "Input.dispatchKeyEvent",
                "{\"type\":\"keyUp\",\"windowsVirtualKeyCode\":13,\"unmodifiedText\":\"\\r\",\"text\":\"\\r\",\"key\":\"Enter\",\"code\":\"Enter\"}"
            );

            return true;
        }
        catch (Exception ex)
        {
            UpdateStatus($"[#{cycle}] Gagal mengirim perintah: {ex.Message}", StatusDanger);
            return false;
        }
    }

    private async Task<bool> ExecuteSingleCycleAsync(int cycle)
    {
        try
        {
            // Jalankan perintah sebelum klaim jika diaktifkan (misal: rotasi Speeder 6 & Compressor 3)
            if (chkPreCommand.Checked)
            {
                var commands = GetActivePreCommands(cycle);
                int totalCount;
                string mode;
                lock (preCommandsLock)
                {
                    totalCount = preCommandsList.Count;
                    mode = preCommandMode;
                }

                for (int cIdx = 0; cIdx < commands.Count; cIdx++)
                {
                    string cmd = commands[cIdx];
                    if (string.IsNullOrWhiteSpace(cmd)) continue;
                    if (!isRunning) return false;

                    string statusMsg = (mode == "rotate" && totalCount > 1)
                        ? $"[#{cycle}] Mengirim '{cmd}' (gantian {((cycle - 1) % totalCount) + 1}/{totalCount})..."
                        : (commands.Count > 1)
                            ? $"[#{cycle}] Mengirim '{cmd}' ({cIdx + 1}/{commands.Count})..."
                            : $"[#{cycle}] Mengirim '{cmd}'...";

                    UpdateStatus(statusMsg, Color.FromArgb(96, 165, 250));
                    bool sentCmd = await SendDiscordCommandAsync(cmd, cycle);
                    if (!sentCmd) return false;

                    await Task.Delay(preCommandDelayMs);
                    if (!isRunning) return false;
                }
            }

            UpdateStatus($"[#{cycle}] Mengirim 'idle use valentine muffin'...", Color.FromArgb(96, 165, 250));
            bool sentMuffin = await SendDiscordCommandAsync("idle use valentine muffin", cycle);
            if (!sentMuffin) return false;

            UpdateStatus($"[#{cycle}] Menunggu respon bot IDLE FARM...", StatusWarning);

            // 4. Poll respon bot IDLE FARM & deteksi Guru (maksimal 20 detik)
            bool foundBotMsg = false;
            bool isGuruActive = false;

            for (int i = 0; i < 25; i++)
            {
                if (!isRunning) return false;
                await Task.Delay(800);

                string checkScript = @"
                (function() {
                    const msgs = Array.from(document.querySelectorAll('[role=""article""], li[id^=""chat-messages-""]'));
                    let target = null;
                    for (let j = msgs.length - 1; j >= Math.max(0, msgs.length - 8); j--) {
                        const txt = (msgs[j].innerText || '').toLowerCase();
                        // Cek pesan habis/error khusus muffin
                        const isOutOfMuffin = (txt.includes('don\'t have') || txt.includes('not enough') || txt.includes('do not have')) && txt.includes('muffin');
                        if (isOutOfMuffin || txt.includes('nothing to claim')) {
                            return { outOfItem: true, txt: txt };
                        }
                        if (txt.includes('farm productions') || txt.includes('produced items') || txt.includes('nice claim') || txt.includes('mini idlons')) {
                            target = msgs[j];
                            break;
                        }
                    }
                    if (!target) return null;

                    const fullText = (target.innerText || '').toLowerCase();
                    const isGuru = fullText.includes('guru worker skill activated') || fullText.includes('your guru worker skill');
                    
                    // Cek ketersediaan tombol boost
                    const allButtons = Array.from(document.querySelectorAll('button'));
                    const boostBtn = allButtons.find(b => (b.innerText || '').toLowerCase().includes('boost') && !b.disabled);

                    return { found: true, isGuru: isGuru, hasBoost: !!boostBtn };
                })();";

                string checkRes = await webView.ExecuteScriptAsync(checkScript);
                if (checkRes != "null" && !string.IsNullOrWhiteSpace(checkRes))
                {
                    using var checkDoc = JsonDocument.Parse(checkRes);
                    if (checkDoc.RootElement.TryGetProperty("outOfItem", out var outProp) && outProp.GetBoolean())
                    {
                        UpdateStatus($"[#{cycle}] Muffin habis atau tidak ditemukan. Loop selesai.", StatusWarning);
                        return false;
                    }
                    if (checkDoc.RootElement.TryGetProperty("found", out var f) && f.GetBoolean())
                    {
                        foundBotMsg = true;
                        isGuruActive = checkDoc.RootElement.GetProperty("isGuru").GetBoolean();
                        bool hasBoost = checkDoc.RootElement.GetProperty("hasBoost").GetBoolean();
                        if (hasBoost) break;
                    }
                }
            }

            if (!foundBotMsg)
            {
                UpdateStatus($"[#{cycle}] Waktu habis menunggu respon bot.", StatusDanger);
                return false;
            }

            string targetDice = isGuruActive ? selectedGuruDice : "mythic";
            string guruInfo = isGuruActive ? "Guru AKTIF" : "Guru nonaktif";
            UpdateStatus($"[#{cycle}] {guruInfo} ➔ Menekan tombol boost...", StatusWarning);

            // 5. Klik tombol 'boost' (poll sampai tombol aktif / tertekan)
            bool boostClicked = false;
            for (int b = 0; b < 10; b++)
            {
                if (!isRunning) return false;
                string clickBoostScript = @"
                (function() {
                    const btns = Array.from(document.querySelectorAll('button'));
                    const boost = btns.find(b => (b.innerText || '').toLowerCase().includes('boost') && !b.disabled);
                    if (boost) {
                        boost.click();
                        return true;
                    }
                    return false;
                })();";

                string boostRes = await webView.ExecuteScriptAsync(clickBoostScript);
                if (boostRes == "true")
                {
                    boostClicked = true;
                    break;
                }
                await Task.Delay(500);
            }

            if (!boostClicked)
            {
                UpdateStatus($"[#{cycle}] Tombol boost tidak dapat diklik.", StatusDanger);
                return false;
            }

            UpdateStatus($"[#{cycle}] Memilih {targetDice.ToUpper()} dice secara otomatis...", StatusWarning);

            // 6. Pilih Dadu secara otomatis via ikon emoji dadu (alt / src)
            bool diceClicked = false;
            for (int d = 0; d < 12; d++)
            {
                if (!isRunning) return false;
                await Task.Delay(600);

                string selectDiceScript = $@"
                (function() {{
                    const target = '{targetDice}'.toLowerCase();
                    const btns = Array.from(document.querySelectorAll('button'));
                    const diceBtn = btns.find(b => {{
                        if (b.disabled) return false;
                        const imgs = Array.from(b.querySelectorAll('img'));
                        return imgs.some(img => {{
                            const alt = (img.alt || '').toLowerCase();
                            const src = (img.src || '').toLowerCase();
                            if (target === 'mythic') {{
                                return alt.includes('mythic') || src.includes('1107404681887613019');
                            }}
                            if (target === 'legendary') {{
                                return alt.includes('legendary') || src.includes('1250918647623450668');
                            }}
                            return alt.includes(target);
                        }});
                    }});

                    if (diceBtn) {{
                        diceBtn.click();
                        return true;
                    }}
                    return false;
                }})();";

                string diceRes = await webView.ExecuteScriptAsync(selectDiceScript);
                if (diceRes == "true")
                {
                    diceClicked = true;
                    break;
                }
            }

            if (diceClicked)
            {
                UpdateStatus($"[#{cycle}] Berhasil! Dadu {targetDice.ToUpper()} otomatis dipilih.", StatusSuccess);
                return true;
            }
            else
            {
                UpdateStatus($"[#{cycle}] Gagal mendeteksi tombol dadu {targetDice.ToUpper()}.", StatusWarning);
                return false;
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"[#{cycle}] Error: {ex.Message}", StatusDanger);
            return false;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        isRunning = false;
        SaveConfig();
        try
        {
            webView?.Dispose();
        }
        catch { }
        base.OnFormClosing(e);
    }
}
