using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace IdleFarmMacro;

public class PreCommandsForm : Form
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    // Minimalist Dark Palette (Zinc Neutral)
    private static readonly Color BgMain = Color.FromArgb(24, 24, 27);           // #18181b
    private static readonly Color BgSurface = Color.FromArgb(39, 39, 42);        // #27272a
    private static readonly Color BgHover = Color.FromArgb(63, 63, 70);          // #3f3f46
    private static readonly Color BorderColor = Color.FromArgb(63, 63, 70);      // #3f3f46
    private static readonly Color TextPrimary = Color.FromArgb(244, 244, 245);   // #f4f4f5
    private static readonly Color TextMuted = Color.FromArgb(161, 161, 170);     // #a1a1aa
    private static readonly Color AccentBlue = Color.FromArgb(37, 99, 235);      // #2563eb
    private static readonly Color AccentRed = Color.FromArgb(220, 38, 38);       // #dc2626
    private static readonly Color AccentHighlight = Color.FromArgb(96, 165, 250); // #60a5fa

    private ListBox lstCommands = null!;
    private TextBox txtNewCmd = null!;
    private Button btnAdd = null!;
    private Button btnUp = null!;
    private Button btnDown = null!;
    private Button btnDelete = null!;
    private Button btnClear = null!;
    private RadioButton rbModeRotate = null!;
    private RadioButton rbModeAll = null!;
    private TextBox txtDelayMs = null!;
    private Label lblSummary = null!;
    private Button btnSave = null!;
    private Button btnCancel = null!;

    public List<string> Commands { get; private set; } = new();
    public string ExecutionMode { get; private set; } = "rotate";
    public int CommandDelayMs { get; private set; } = 800;

    public PreCommandsForm(List<string> initialCommands, string initialMode, int initialDelayMs)
    {
        Commands = initialCommands != null && initialCommands.Count > 0
            ? new List<string>(initialCommands)
            : new List<string> { "idle use time speeder 6", "idle use time compressor 3" };

        ExecutionMode = (initialMode == "all") ? "all" : "rotate";
        CommandDelayMs = initialDelayMs > 0 ? initialDelayMs : 800;

        InitializeComponents();
        ApplyImmersiveDarkMode();
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

    private void InitializeComponents()
    {
        this.Text = "Pengaturan Pesan Sebelum Klaim (Pre-command)";
        this.Size = new Size(640, 600);
        this.MinimumSize = new Size(580, 520);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = BgMain;
        this.ForeColor = TextPrimary;
        this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Padding = new Padding(16, 14, 16, 14);

        // --- Header Panel ---
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 56,
            BackColor = Color.Transparent
        };
        var lblTitle = new Label
        {
            Text = "Daftar Pesan Sebelum Klaim Muffin",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = TextPrimary,
            AutoSize = true,
            Location = new Point(0, 0)
        };
        var lblSubtitle = new Label
        {
            Text = "Pesan/perintah di bawah akan dikirim ke Discord otomatis sebelum 'idle use valentine muffin'.",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = TextMuted,
            AutoSize = true,
            Location = new Point(0, 24)
        };
        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSubtitle });

        // --- Execution Mode Panel ---
        var pnlMode = new Panel
        {
            Dock = DockStyle.Top,
            Height = 58,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 0, 4)
        };

        var lblModeTitle = new Label
        {
            Text = "Mode Eksekusi Perintah:",
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(0, 2)
        };

        rbModeRotate = new RadioButton
        {
            Text = "🔄 Gantian / Rotasi (1 Pesan per Siklus)",
            ForeColor = AccentHighlight,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            AutoSize = true,
            Checked = (ExecutionMode == "rotate"),
            Cursor = Cursors.Hand,
            Location = new Point(0, 26)
        };

        rbModeAll = new RadioButton
        {
            Text = "📑 Kirim Semua Tiap Siklus",
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            AutoSize = true,
            Checked = (ExecutionMode == "all"),
            Cursor = Cursors.Hand,
            Location = new Point(270, 26)
        };

        var lblDelay = new Label
        {
            Text = "Jeda:",
            ForeColor = TextMuted,
            AutoSize = true,
            Location = new Point(470, 29)
        };

        txtDelayMs = new TextBox
        {
            Text = CommandDelayMs.ToString(),
            Width = 48,
            BackColor = BgSurface,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            TextAlign = HorizontalAlignment.Center,
            Location = new Point(508, 26)
        };

        var lblDelayMs = new Label
        {
            Text = "ms",
            ForeColor = TextMuted,
            AutoSize = true,
            Location = new Point(560, 29)
        };

        pnlMode.Controls.AddRange(new Control[] {
            lblModeTitle, rbModeRotate, rbModeAll,
            lblDelay, txtDelayMs, lblDelayMs
        });

        // --- Bottom Action Panel ---
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            BackColor = Color.Transparent
        };

        lblSummary = new Label
        {
            Text = $"Total: {Commands.Count} pesan tersimpan",
            ForeColor = TextMuted,
            AutoSize = true,
            Location = new Point(0, 12)
        };

        btnCancel = new Button
        {
            Text = "Batal",
            Size = new Size(82, 28),
            BackColor = BgSurface,
            ForeColor = TextMuted,
            FlatStyle = FlatStyle.Flat,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 1;
        btnCancel.FlatAppearance.BorderColor = BorderColor;
        btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

        btnSave = new Button
        {
            Text = "Simpan",
            Size = new Size(82, 28),
            BackColor = AccentBlue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += OnSaveClicked;

        pnlBottom.Resize += (s, e) =>
        {
            btnSave.Location = new Point(pnlBottom.ClientSize.Width - btnSave.Width, 8);
            btnCancel.Location = new Point(btnSave.Left - btnCancel.Width - 8, 8);
        };

        pnlBottom.Controls.AddRange(new Control[] { lblSummary, btnCancel, btnSave });

        // --- Add New Command Panel ---
        var pnlAdd = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 64,
            BackColor = Color.Transparent
        };

        var lblAdd = new Label
        {
            Text = "Ketik perintah baru (atau klik 2x item di atas untuk edit; pisahkan ';' jika banyak):",
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 8.5f),
            AutoSize = true,
            Location = new Point(0, 4)
        };

        txtNewCmd = new TextBox
        {
            Location = new Point(0, 26),
            Height = 26,
            Width = 490,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = BgSurface,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5f)
        };
        txtNewCmd.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                AddCommandFromInput();
            }
        };

        btnAdd = new Button
        {
            Text = "+ Tambah",
            Location = new Point(500, 25),
            Size = new Size(95, 27),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = AccentBlue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnAdd.FlatAppearance.BorderSize = 0;
        btnAdd.Click += (s, e) => AddCommandFromInput();

        pnlAdd.Controls.AddRange(new Control[] { lblAdd, txtNewCmd, btnAdd });

        // --- Quick Presets Panel ---
        var pnlPresets = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 36,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        var lblPresets = new Label
        {
            Text = "Preset cepat:",
            ForeColor = TextMuted,
            AutoSize = true,
            Margin = new Padding(0, 6, 6, 0)
        };

        pnlPresets.Controls.Add(lblPresets);
        pnlPresets.Controls.Add(CreateDuoPresetButton("⚡ Duo (Speeder 6 & Compressor 3)"));
        pnlPresets.Controls.Add(CreateSinglePresetButton("+ Speeder 6", "idle use time speeder 6"));
        pnlPresets.Controls.Add(CreateSinglePresetButton("+ Compressor 3", "idle use time compressor 3"));
        pnlPresets.Controls.Add(CreateSinglePresetButton("+ Speeder 12", "idle use time speeder 12"));
        pnlPresets.Controls.Add(CreateSinglePresetButton("+ Compressor 6", "idle use time compressor 6"));

        // --- Center List Area ---
        var pnlListArea = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 6, 0, 8)
        };

        var pnlListButtons = new Panel
        {
            Dock = DockStyle.Right,
            Width = 96,
            BackColor = Color.Transparent,
            Padding = new Padding(8, 0, 0, 0)
        };

        btnUp = CreateListActionButton("▲ Naik", 0, (s, e) => MoveSelectedItem(-1));
        btnDown = CreateListActionButton("▼ Turun", 34, (s, e) => MoveSelectedItem(1));
        btnDelete = CreateListActionButton("🗑 Hapus", 68, (s, e) => DeleteSelectedItem());
        btnDelete.ForeColor = AccentRed;

        btnClear = CreateListActionButton("Bersihkan", 108, (s, e) => ClearAllItems());

        pnlListButtons.Controls.AddRange(new Control[] { btnUp, btnDown, btnDelete, btnClear });

        lstCommands = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = BgSurface,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 28,
            Font = new Font("Segoe UI", 9.5f)
        };

        lstCommands.DrawItem += OnDrawListItem;
        lstCommands.SelectedIndexChanged += (s, e) => UpdateListButtonStates();
        lstCommands.DoubleClick += (s, e) =>
        {
            if (lstCommands.SelectedIndex >= 0)
            {
                txtNewCmd.Text = lstCommands.SelectedItem?.ToString() ?? "";
                txtNewCmd.Focus();
                txtNewCmd.SelectAll();
            }
        };

        foreach (var cmd in Commands)
        {
            lstCommands.Items.Add(cmd);
        }
        if (lstCommands.Items.Count > 0)
        {
            lstCommands.SelectedIndex = 0;
        }

        pnlListArea.Controls.Add(lstCommands);
        pnlListArea.Controls.Add(pnlListButtons);

        // Add controls in WinForms Dock order:
        this.Controls.Add(pnlListArea);
        this.Controls.Add(pnlPresets);
        this.Controls.Add(pnlAdd);
        this.Controls.Add(pnlBottom);
        this.Controls.Add(pnlMode);
        this.Controls.Add(pnlHeader);

        UpdateSummary();
        UpdateListButtonStates();
    }

    private Button CreateDuoPresetButton(string text)
    {
        var btn = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 26,
            BackColor = Color.FromArgb(30, 58, 138), // dark blue accent
            ForeColor = AccentHighlight,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 2, 6, 0),
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
        };
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.BorderColor = AccentHighlight;
        btn.Click += (s, e) =>
        {
            lstCommands.Items.Clear();
            lstCommands.Items.Add("idle use time speeder 6");
            lstCommands.Items.Add("idle use time compressor 3");
            rbModeRotate.Checked = true;
            lstCommands.SelectedIndex = 0;
            UpdateSummary();
            UpdateListButtonStates();
        };
        return btn;
    }

    private Button CreateSinglePresetButton(string text, string command)
    {
        var btn = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 26,
            BackColor = BgSurface,
            ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 2, 6, 0),
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 8.5f)
        };
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.BorderColor = BorderColor;
        btn.Click += (s, e) =>
        {
            lstCommands.Items.Add(command);
            lstCommands.SelectedIndex = lstCommands.Items.Count - 1;
            UpdateSummary();
            UpdateListButtonStates();
        };
        return btn;
    }

    private Button CreateListActionButton(string text, int top, EventHandler onClick)
    {
        var btn = new Button
        {
            Text = text,
            Size = new Size(86, 28),
            Location = new Point(8, top),
            BackColor = BgSurface,
            ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 8.5f)
        };
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.BorderColor = BorderColor;
        btn.Click += onClick;
        return btn;
    }

    private void OnDrawListItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= lstCommands.Items.Count) return;

        bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        using var bgBrush = new SolidBrush(isSelected ? BgHover : BgSurface);
        e.Graphics.FillRectangle(bgBrush, e.Bounds);

        if (isSelected)
        {
            using var borderPen = new Pen(AccentHighlight, 1);
            var rect = new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);
            e.Graphics.DrawRectangle(borderPen, rect);
        }

        string text = $"{e.Index + 1}.  {lstCommands.Items[e.Index]}";
        using var textBrush = new SolidBrush(isSelected ? Color.White : TextPrimary);
        using var sf = new StringFormat
        {
            LineAlignment = StringAlignment.Center,
            Alignment = StringAlignment.Near,
            Trimming = StringTrimming.EllipsisCharacter
        };

        var textRect = new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 16, e.Bounds.Height);
        e.Graphics.DrawString(text, e.Font ?? this.Font, textBrush, textRect, sf);
    }

    private void AddCommandFromInput()
    {
        string raw = txtNewCmd.Text.Trim();
        if (string.IsNullOrWhiteSpace(raw)) return;

        var parts = raw.Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int added = 0;
        foreach (var p in parts)
        {
            if (!string.IsNullOrWhiteSpace(p))
            {
                lstCommands.Items.Add(p);
                added++;
            }
        }

        if (added > 0)
        {
            txtNewCmd.Clear();
            lstCommands.SelectedIndex = lstCommands.Items.Count - 1;
            UpdateSummary();
            UpdateListButtonStates();
        }
        txtNewCmd.Focus();
    }

    private void MoveSelectedItem(int direction)
    {
        int idx = lstCommands.SelectedIndex;
        if (idx < 0) return;

        int newIdx = idx + direction;
        if (newIdx < 0 || newIdx >= lstCommands.Items.Count) return;

        var item = lstCommands.Items[idx];
        lstCommands.Items.RemoveAt(idx);
        lstCommands.Items.Insert(newIdx, item);
        lstCommands.SelectedIndex = newIdx;
        UpdateListButtonStates();
    }

    private void DeleteSelectedItem()
    {
        int idx = lstCommands.SelectedIndex;
        if (idx < 0) return;

        lstCommands.Items.RemoveAt(idx);
        if (lstCommands.Items.Count > 0)
        {
            lstCommands.SelectedIndex = Math.Min(idx, lstCommands.Items.Count - 1);
        }
        UpdateSummary();
        UpdateListButtonStates();
    }

    private void ClearAllItems()
    {
        if (lstCommands.Items.Count == 0) return;

        var res = MessageBox.Show(this, "Hapus semua pesan dari daftar?", "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (res == DialogResult.Yes)
        {
            lstCommands.Items.Clear();
            UpdateSummary();
            UpdateListButtonStates();
        }
    }

    private void UpdateListButtonStates()
    {
        int idx = lstCommands.SelectedIndex;
        int count = lstCommands.Items.Count;

        btnUp.Enabled = (idx > 0);
        btnDown.Enabled = (idx >= 0 && idx < count - 1);
        btnDelete.Enabled = (idx >= 0);
        btnClear.Enabled = (count > 0);

        btnUp.ForeColor = btnUp.Enabled ? TextPrimary : TextMuted;
        btnDown.ForeColor = btnDown.Enabled ? TextPrimary : TextMuted;
        btnDelete.ForeColor = btnDelete.Enabled ? AccentRed : TextMuted;
        btnClear.ForeColor = btnClear.Enabled ? TextMuted : Color.FromArgb(80, 80, 90);
    }

    private void UpdateSummary()
    {
        int count = lstCommands.Items.Count;
        lblSummary.Text = $"Total: {count} pesan tersimpan";
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        Commands = lstCommands.Items.Cast<string>().Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        ExecutionMode = rbModeRotate.Checked ? "rotate" : "all";

        if (int.TryParse(txtDelayMs.Text.Trim(), out int delay) && delay >= 100)
        {
            CommandDelayMs = delay;
        }
        else
        {
            CommandDelayMs = 800;
        }

        this.DialogResult = DialogResult.OK;
        this.Close();
    }
}
