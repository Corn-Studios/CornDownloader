using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AppDownloader
{
    public class MainForm : Form
    {
        // ── Colours ──────────────────────────────────────────────────────────
        private static readonly Color BG          = Color.FromArgb(13, 13, 18);
        private static readonly Color SURFACE     = Color.FromArgb(22, 22, 30);
        private static readonly Color SURFACE2    = Color.FromArgb(30, 30, 40);
        private static readonly Color ACCENT      = Color.FromArgb(99, 102, 241);   // indigo
        private static readonly Color ACCENT_HOV  = Color.FromArgb(129, 132, 255);
        private static readonly Color SUCCESS     = Color.FromArgb(34, 197, 94);
        private static readonly Color WARNING     = Color.FromArgb(251, 191, 36);
        private static readonly Color DANGER      = Color.FromArgb(239, 68, 68);
        private static readonly Color TEXT_PRI    = Color.FromArgb(240, 240, 255);
        private static readonly Color TEXT_SEC    = Color.FromArgb(140, 140, 170);
        private static readonly Color BORDER      = Color.FromArgb(40, 40, 58);

        // ── State ────────────────────────────────────────────────────────────
        private readonly DownloadManager _dm;
        private readonly Dictionary<string, Panel> _categoryPanels = new Dictionary<string, Panel>();
        private readonly Dictionary<AppEntry, AppTile> _tiles = new Dictionary<AppEntry, AppTile>();
        private string _activeCategory = "All";
        private bool _isInstalling = false;

        // ── Controls ─────────────────────────────────────────────────────────
        private Panel        _sidebar;
        private Panel        _mainArea;
        private Panel        _topBar;
        private FlowLayoutPanel _appGrid;
        private Panel        _bottomBar;
        private Label        _statusLabel;
        private Label        _selectionCountLabel;
        private ProgressBar  _overallProgress;
        private Button       _installBtn;
        private Button       _clearBtn;
        private TextBox      _searchBox;
        private Label        _wingetBadge;
        private TextBox      _folderBox;
        private Button       _browseBtn;
        private CheckBox     _preferWingetChk;
        private RichTextBox  _logBox;
        private Panel        _logPanel;

        private readonly string[] _categories = {
            "All", "Browsers", "Dev Tools", "Media & Entertainment",
            "Productivity", "Gaming", "Utilities & System Tools", "Customization"
        };

        public MainForm()
        {
            _dm = new DownloadManager();
            InitializeComponent();
            PopulateApps("All");
            UpdateSelectionCount();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  UI CONSTRUCTION
        // ─────────────────────────────────────────────────────────────────────
        private void InitializeComponent()
        {
            this.Text          = "App Downloader";
            this.Size          = new Size(1180, 760);
            this.MinimumSize   = new Size(900, 600);
            this.BackColor     = BG;
            this.ForeColor     = TEXT_PRI;
            this.Font          = new Font("Segoe UI", 9f, FontStyle.Regular);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;

            BuildTopBar();
            BuildSidebar();
            BuildMainArea();
            BuildBottomBar();

            this.Controls.AddRange(new Control[] { _topBar, _sidebar, _mainArea, _bottomBar });
            this.Resize += (s, e) => LayoutPanels();
            LayoutPanels();
        }

        private void LayoutPanels()
        {
            int w = ClientSize.Width;
            int h = ClientSize.Height;

            int topH    = 60;
            int botH    = 120;
            int sideW   = 190;
            int contentH = h - topH - botH;

            _topBar.SetBounds(0, 0, w, topH);
            _sidebar.SetBounds(0, topH, sideW, contentH);
            _mainArea.SetBounds(sideW, topH, w - sideW, contentH);
            _bottomBar.SetBounds(0, h - botH, w, botH);
        }

        // ── TOP BAR ──────────────────────────────────────────────────────────
        private void BuildTopBar()
        {
            _topBar = new Panel { BackColor = SURFACE, Dock = DockStyle.None };

            var titleLbl = new Label
            {
                Text = "⬇  App Downloader",
                Font = new Font("Segoe UI Semibold", 13f),
                ForeColor = TEXT_PRI,
                AutoSize = true,
                Location = new Point(18, 17)
            };

            _searchBox = new TextBox
            {
                BackColor   = SURFACE2,
                ForeColor   = TEXT_PRI,
                BorderStyle = BorderStyle.FixedSingle,
                Font        = new Font("Segoe UI", 10f),
                Size        = new Size(260, 30),
                Location    = new Point(220, 15)
            };
            // .NET 4.8 placeholder via Win32 EM_SETCUEBANNER
            _searchBox.HandleCreated += (s, e) =>
                NativeMethods.SetCueBanner(_searchBox.Handle, "🔍  Search apps...");
            _searchBox.TextChanged += (s, e) => FilterApps(_searchBox.Text);

            _wingetBadge = new Label
            {
                AutoSize  = true,
                Font      = new Font("Segoe UI", 8.5f),
                Location  = new Point(500, 21)
            };
            UpdateWingetBadge();

            _topBar.Controls.AddRange(new Control[] { titleLbl, _searchBox, _wingetBadge });
        }

        private void UpdateWingetBadge()
        {
            if (_dm.WingetAvailable)
            {
                _wingetBadge.Text      = "✔  winget detected";
                _wingetBadge.ForeColor = SUCCESS;
            }
            else
            {
                _wingetBadge.Text      = "⚠  winget not found — direct URLs only";
                _wingetBadge.ForeColor = WARNING;
            }
        }

        // ── SIDEBAR ──────────────────────────────────────────────────────────
        private void BuildSidebar()
        {
            _sidebar = new Panel { BackColor = SURFACE };

            int y = 10;
            foreach (var cat in _categories)
            {
                var btn = CreateSidebarBtn(cat);
                btn.Location = new Point(8, y);
                btn.Width    = 174;
                _sidebar.Controls.Add(btn);
                y += 42;
            }

            // Select-All / Deselect-All
            var selAll = CreateSmallBtn("✔ Select All", ACCENT);
            selAll.Location = new Point(8, y + 10);
            selAll.Width    = 80;
            selAll.Click   += (s, e) => SetAllInView(true);

            var deselAll = CreateSmallBtn("✘ None", SURFACE2);
            deselAll.Location = new Point(96, y + 10);
            deselAll.Width    = 86;
            deselAll.Click   += (s, e) => SetAllInView(false);

            _sidebar.Controls.AddRange(new Control[] { selAll, deselAll });
        }

        private Button CreateSidebarBtn(string category)
        {
            string emoji = category switch
            {
                "All"                      => "🏠",
                "Browsers"                 => "🌐",
                "Dev Tools"                => "💻",
                "Media & Entertainment"    => "🎬",
                "Productivity"             => "📋",
                "Gaming"                   => "🎮",
                "Utilities & System Tools" => "🔧",
                "Customization"            => "🎨",
                _                          => "📦"
            };

            var btn = new Button
            {
                Text      = $" {emoji}  {category}",
                TextAlign = ContentAlignment.MiddleLeft,
                FlatStyle = FlatStyle.Flat,
                BackColor = _activeCategory == category ? ACCENT : Color.Transparent,
                ForeColor = _activeCategory == category ? Color.White : TEXT_SEC,
                Font      = new Font("Segoe UI", 9f),
                Height    = 36,
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize     = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 99, 102, 241);
            btn.Click += (s, e) =>
            {
                _activeCategory = category;
                RefreshSidebarButtons();
                PopulateApps(category);
            };
            return btn;
        }

        private Button CreateSmallBtn(string text, Color bg)
        {
            var btn = new Button
            {
                Text      = text,
                Height    = 28,
                BackColor = bg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 8f),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void RefreshSidebarButtons()
        {
            foreach (Control c in _sidebar.Controls)
            {
                if (c is Button btn)
                {
                    string cat = btn.Text.Substring(btn.Text.IndexOf("  ") + 2).Trim();
                    bool active = cat == _activeCategory;
                    btn.BackColor = active ? ACCENT : Color.Transparent;
                    btn.ForeColor = active ? Color.White : TEXT_SEC;
                }
            }
        }

        // ── MAIN APP GRID ────────────────────────────────────────────────────
        private void BuildMainArea()
        {
            _mainArea = new Panel { BackColor = BG };

            _appGrid = new FlowLayoutPanel
            {
                AutoScroll      = true,
                WrapContents    = true,
                BackColor       = BG,
                Padding         = new Padding(10),
                Dock            = DockStyle.Fill
            };

            _mainArea.Controls.Add(_appGrid);
        }

        private void PopulateApps(string category)
        {
            _appGrid.SuspendLayout();
            _appGrid.Controls.Clear();

            IEnumerable<AppEntry> apps = category == "All"
                ? AppCatalog.All
                : AppCatalog.All.Where(a => a.Category == category);

            string search = _searchBox?.Text?.Trim().ToLowerInvariant() ?? "";
            if (!string.IsNullOrEmpty(search))
                apps = apps.Where(a => a.Name.ToLowerInvariant().Contains(search) ||
                                        a.Description.ToLowerInvariant().Contains(search));

            foreach (var app in apps)
            {
                if (!_tiles.TryGetValue(app, out var tile))
                {
                    tile = new AppTile(app, SURFACE, SURFACE2, ACCENT, TEXT_PRI, TEXT_SEC, BORDER);
                    tile.CheckedChanged += (s, e) => UpdateSelectionCount();
                    _tiles[app] = tile;
                }
                _appGrid.Controls.Add(tile);
            }

            _appGrid.ResumeLayout(true);
        }

        private void FilterApps(string query)
        {
            PopulateApps(_activeCategory);
        }

        private void SetAllInView(bool check)
        {
            foreach (AppTile tile in _appGrid.Controls.OfType<AppTile>())
                tile.IsChecked = check;
            UpdateSelectionCount();
        }

        // ── BOTTOM BAR ───────────────────────────────────────────────────────
        private void BuildBottomBar()
        {
            _bottomBar = new Panel { BackColor = SURFACE };

            // Row 1: folder picker + options
            var folderLbl = new Label
            {
                Text      = "Install / Save To:",
                ForeColor = TEXT_SEC,
                AutoSize  = true,
                Location  = new Point(16, 12)
            };

            _folderBox = new TextBox
            {
                Text        = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads",
                BackColor   = SURFACE2,
                ForeColor   = TEXT_PRI,
                BorderStyle = BorderStyle.FixedSingle,
                Size        = new Size(340, 24),
                Location    = new Point(130, 9)
            };

            _browseBtn = new Button
            {
                Text      = "Browse",
                Size      = new Size(70, 24),
                Location  = new Point(478, 9),
                BackColor = SURFACE2,
                ForeColor = TEXT_SEC,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            _browseBtn.FlatAppearance.BorderColor = BORDER;
            _browseBtn.Click += (s, e) =>
            {
                using var dlg = new FolderBrowserDialog();
                if (dlg.ShowDialog() == DialogResult.OK)
                    _folderBox.Text = dlg.SelectedPath;
            };

            _preferWingetChk = new CheckBox
            {
                Text      = "Prefer winget (faster, auto-installs silently)",
                ForeColor = TEXT_SEC,
                Checked   = _dm.WingetAvailable,
                Enabled   = _dm.WingetAvailable,
                AutoSize  = true,
                Location  = new Point(570, 12)
            };

            // Row 2: progress + buttons
            _overallProgress = new ProgressBar
            {
                Size     = new Size(460, 18),
                Location = new Point(16, 50),
                Style    = ProgressBarStyle.Continuous,
                Minimum  = 0,
                Maximum  = 100,
                Value    = 0
            };

            _statusLabel = new Label
            {
                Text      = "Ready",
                ForeColor = TEXT_SEC,
                AutoSize  = true,
                Location  = new Point(16, 75)
            };

            _selectionCountLabel = new Label
            {
                ForeColor = TEXT_SEC,
                AutoSize  = true,
                Location  = new Point(490, 50)
            };

            _clearBtn = new Button
            {
                Text      = "Clear Selection",
                Size      = new Size(130, 36),
                Location  = new Point(800, 45),
                BackColor = SURFACE2,
                ForeColor = TEXT_SEC,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            _clearBtn.FlatAppearance.BorderColor = BORDER;
            _clearBtn.Click += (s, e) => SetAllInView(false);

            _installBtn = new Button
            {
                Text      = "⬇  Install Selected",
                Size      = new Size(160, 36),
                Location  = new Point(940, 45),
                BackColor = ACCENT,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI Semibold", 10f),
                Cursor    = Cursors.Hand
            };
            _installBtn.FlatAppearance.BorderSize = 0;
            _installBtn.Click += OnInstallClicked;

            // Log toggle
            var logToggle = new Button
            {
                Text      = "📋 Log",
                Size      = new Size(70, 24),
                Location  = new Point(935, 9),
                BackColor = SURFACE2,
                ForeColor = TEXT_SEC,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            logToggle.FlatAppearance.BorderColor = BORDER;
            logToggle.Click += (s, e) => ToggleLog();

            _bottomBar.Controls.AddRange(new Control[] {
                folderLbl, _folderBox, _browseBtn, _preferWingetChk,
                _overallProgress, _statusLabel, _selectionCountLabel,
                _clearBtn, _installBtn, logToggle
            });

            // Log panel (hidden by default)
            _logBox = new RichTextBox
            {
                BackColor   = Color.FromArgb(10, 10, 14),
                ForeColor   = Color.FromArgb(100, 220, 100),
                BorderStyle = BorderStyle.None,
                ReadOnly    = true,
                Font        = new Font("Consolas", 8.5f),
                Dock        = DockStyle.Fill,
                ScrollBars  = RichTextBoxScrollBars.Vertical
            };

            _logPanel = new Panel
            {
                BackColor = Color.FromArgb(10, 10, 14),
                Visible   = false,
                Dock      = DockStyle.Bottom,
                Height    = 160
            };
            _logPanel.Controls.Add(_logBox);
            this.Controls.Add(_logPanel);
        }

        private void ToggleLog()
        {
            _logPanel.Visible = !_logPanel.Visible;
            if (_logPanel.Visible)
            {
                _logPanel.BringToFront();
                _logPanel.SetBounds(0, ClientSize.Height - _logPanel.Height, ClientSize.Width, _logPanel.Height);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  INSTALL LOGIC
        // ─────────────────────────────────────────────────────────────────────
        private async void OnInstallClicked(object sender, EventArgs e)
        {
            if (_isInstalling) return;

            var selected = _tiles.Where(kv => kv.Value.IsChecked).Select(kv => kv.Key).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Please select at least one app to install.", "Nothing selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string folder = _folderBox.Text.Trim();
            if (!Directory.Exists(folder))
            {
                try { Directory.CreateDirectory(folder); }
                catch
                {
                    MessageBox.Show($"Cannot create folder:\n{folder}", "Invalid path",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            bool preferWinget = _preferWingetChk.Checked;

            _isInstalling = true;
            _installBtn.Enabled  = false;
            _installBtn.Text     = "⏳ Installing...";
            _overallProgress.Value = 0;
            _overallProgress.Maximum = selected.Count;

            Log($"[START] Installing {selected.Count} app(s) — {DateTime.Now:HH:mm:ss}");

            var results = await _dm.InstallAllAsync(
                selected,
                folder,
                preferWinget,
                (app, status, msg) =>
                {
                    this.Invoke((Action)(() =>
                    {
                        if (_tiles.TryGetValue(app, out var tile))
                            tile.SetStatus(status);
                        _statusLabel.Text = $"{app.Name}: {msg}";
                        Log($"[{app.Name}] {msg}");
                    }));
                },
                (done, total) =>
                {
                    this.Invoke((Action)(() =>
                    {
                        _overallProgress.Value = done;
                    }));
                }
            );

            int ok   = results.Count(r => r.Status == InstallStatus.Success);
            int fail = results.Count(r => r.Status == InstallStatus.Failed);

            _statusLabel.Text   = $"Done — {ok} succeeded, {fail} failed.";
            _installBtn.Text    = "⬇  Install Selected";
            _installBtn.Enabled = true;
            _isInstalling       = false;

            Log($"[DONE] {ok}/{selected.Count} succeeded — {DateTime.Now:HH:mm:ss}");

            if (fail > 0)
            {
                var failNames = string.Join("\n", results.Where(r => r.Status == InstallStatus.Failed)
                                                         .Select(r => $"  • {r.App.Name}: {r.Message}"));
                MessageBox.Show($"{ok} app(s) installed successfully.\n\nFailed ({fail}):\n{failNames}",
                    "Installation Complete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show($"All {ok} app(s) installed successfully! 🎉",
                    "Installation Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateSelectionCount()
        {
            int count = _tiles.Values.Count(t => t.IsChecked);
            if (_selectionCountLabel != null)
                _selectionCountLabel.Text = count == 0
                    ? "No apps selected"
                    : $"{count} app{(count == 1 ? "" : "s")} selected";
        }

        private void Log(string msg)
        {
            if (_logBox.InvokeRequired)
                _logBox.Invoke((Action)(() => Log(msg)));
            else
            {
                _logBox.AppendText(msg + "\n");
                _logBox.ScrollToCaret();
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  APP TILE CONTROL
    // ─────────────────────────────────────────────────────────────────────────
    public class AppTile : Panel
    {
        private bool _checked;
        private readonly Color _normalBg;
        private readonly Color _checkedBg;
        private readonly Color _accentColor;
        private readonly Label _statusDot;
        private InstallStatus _status = InstallStatus.Pending;

        public event EventHandler CheckedChanged;
        public bool IsChecked
        {
            get => _checked;
            set
            {
                _checked  = value;
                BackColor = value ? _checkedBg : _normalBg;
                Invalidate();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public AppTile(AppEntry app, Color normalBg, Color checkedBg, Color accent,
                       Color textPri, Color textSec, Color border)
        {
            _normalBg   = normalBg;
            _checkedBg  = checkedBg;
            _accentColor = accent;

            Size      = new Size(210, 100);
            BackColor = normalBg;
            Margin    = new Padding(6);
            Cursor    = Cursors.Hand;

            // Rounded border via paint
            this.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using var pen = new System.Drawing.Pen(_checked ? accent : border, 1.5f);
                g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);

                if (_checked)
                {
                    // Checkbox tick
                    g.FillRectangle(new SolidBrush(accent), Width - 22, 6, 16, 16);
                    using var whitePen = new System.Drawing.Pen(Color.White, 2f);
                    g.DrawLines(whitePen, new[] {
                        new Point(Width - 19, 14),
                        new Point(Width - 15, 18),
                        new Point(Width - 9,  9)
                    });
                }
            };

            var iconLbl = new Label
            {
                Text      = app.IconChar,
                Font      = new Font("Segoe UI Emoji", 18f),
                AutoSize  = true,
                Location  = new Point(10, 10),
                BackColor = Color.Transparent
            };

            var nameLbl = new Label
            {
                Text      = app.Name,
                Font      = new Font("Segoe UI Semibold", 9f),
                ForeColor = textPri,
                AutoSize  = false,
                Size      = new Size(170, 18),
                Location  = new Point(48, 11),
                BackColor = Color.Transparent
            };

            var descLbl = new Label
            {
                Text      = app.Description,
                Font      = new Font("Segoe UI", 7.5f),
                ForeColor = textSec,
                AutoSize  = false,
                Size      = new Size(190, 32),
                Location  = new Point(10, 55),
                BackColor = Color.Transparent
            };

            // Method badge
            string method = app.WingetId != null ? "winget" : "direct";
            Color  badgeBg = app.WingetId != null
                ? Color.FromArgb(30, 99, 102, 241)
                : Color.FromArgb(30, 251, 191, 36);
            Color badgeFg = app.WingetId != null
                ? Color.FromArgb(160, 165, 255)
                : Color.FromArgb(251, 191, 36);

            var methodBadge = new Label
            {
                Text      = method,
                Font      = new Font("Segoe UI", 7f),
                ForeColor = badgeFg,
                BackColor = badgeBg,
                AutoSize  = true,
                Location  = new Point(10, 37),
                Padding   = new Padding(3, 1, 3, 1)
            };

            var catBadge = new Label
            {
                Text      = app.Category,
                Font      = new Font("Segoe UI", 6.5f),
                ForeColor = textSec,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(70, 40)
            };

            _statusDot = new Label
            {
                Text      = "",
                AutoSize  = true,
                Location  = new Point(10, 82),
                Font      = new Font("Segoe UI", 7.5f),
                BackColor = Color.Transparent
            };

            Controls.AddRange(new Control[] { iconLbl, nameLbl, descLbl, methodBadge, catBadge, _statusDot });

            // Click to toggle
            void Toggle(object s, EventArgs e) => IsChecked = !_checked;
            this.Click     += Toggle;
            iconLbl.Click  += Toggle;
            nameLbl.Click  += Toggle;
            descLbl.Click  += Toggle;
            catBadge.Click += Toggle;

            this.MouseEnter += (s, e) => { if (!_checked) BackColor = Color.FromArgb(28, 28, 38); };
            this.MouseLeave += (s, e) => { if (!_checked) BackColor = _normalBg; };
        }

        public void SetStatus(InstallStatus status)
        {
            _status = status;
            switch (status)
            {
                case InstallStatus.Installing:
                case InstallStatus.Downloading:
                    _statusDot.Text      = "⏳ Installing...";
                    _statusDot.ForeColor = Color.FromArgb(251, 191, 36);
                    break;
                case InstallStatus.Success:
                    _statusDot.Text      = "✔ Done";
                    _statusDot.ForeColor = Color.FromArgb(34, 197, 94);
                    IsChecked = false;
                    break;
                case InstallStatus.Failed:
                    _statusDot.Text      = "✘ Failed";
                    _statusDot.ForeColor = Color.FromArgb(239, 68, 68);
                    break;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  P/INVOKE — TextBox placeholder text for .NET 4.8
    // ─────────────────────────────────────────────────────────────────────────
    internal static class NativeMethods
    {
        private const int EM_SETCUEBANNER = 0x1501;

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, string lParam);

        public static void SetCueBanner(IntPtr handle, string text)
        {
            SendMessage(handle, EM_SETCUEBANNER, 1, text);
        }
    }
}
