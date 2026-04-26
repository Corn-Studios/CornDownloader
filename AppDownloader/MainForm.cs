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
        private readonly Dictionary<AppEntry, bool> _installedCache = new Dictionary<AppEntry, bool>();

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
        private Label        _scanStatusLabel;

        private readonly string[] _categories;

        public MainForm()
        {
            _dm = new DownloadManager();
            // Build category list sorted alphabetically — matches the grid's OrderBy(g => g.Key)
            _categories = new[] { "All" }
                .Concat(AppCatalog.All.Select(a => a.Category).Distinct().OrderBy(c => c))
                .ToArray();
            InitializeComponent();
            ApplyRecommendedPreset();
            PopulateApps("All");
            UpdateSelectionCount();
            _ = ScanInstalledAsync(); // fire-and-forget background scan
        }

        // ─────────────────────────────────────────────────────────────────────
        //  INSTALLED DETECTION
        // ─────────────────────────────────────────────────────────────────────
        private async Task ScanInstalledAsync()
        {
            if (!_dm.WingetAvailable) return;

            if (_scanStatusLabel != null)
            {
                _scanStatusLabel.Text      = "🔍 Scanning installed apps...";
                _scanStatusLabel.ForeColor = TEXT_SEC;
            }

            // Single winget list call — returns all installed IDs at once
            var installedIds = await _dm.GetAllInstalledIdsAsync();

            foreach (var app in AppCatalog.All)
            {
                _installedCache[app] = !string.IsNullOrEmpty(app.WingetId) &&
                                       installedIds.Contains(app.WingetId);
            }

            this.Invoke((Action)(() =>
            {
                foreach (var kv in _tiles)
                {
                    if (_installedCache.TryGetValue(kv.Key, out bool inst))
                        kv.Value.SetInstalled(inst);
                }

                if (_scanStatusLabel != null)
                {
                    int installedCount = _installedCache.Values.Count(v => v);
                    int totalCount     = AppCatalog.All.Count;
                    _scanStatusLabel.Text      = $"✔ {installedCount}/{totalCount} apps installed";
                    _scanStatusLabel.ForeColor = SUCCESS;
                }

                UpdateSelectionCount();
            }));
        }

        // ─────────────────────────────────────────────────────────────────────
        //  RECOMMENDED PRESET
        // ─────────────────────────────────────────────────────────────────────
        private void ApplyRecommendedPreset()
        {
            foreach (var app in AppCatalog.All)
            {
                if (_tiles.TryGetValue(app, out var tile))
                    tile.IsChecked = app.IsRecommended;
            }
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
                PlaceholderText = "🔍  Search apps...",
                BackColor       = SURFACE2,
                ForeColor       = TEXT_PRI,
                BorderStyle     = BorderStyle.FixedSingle,
                Font            = new Font("Segoe UI", 10f),
                Size            = new Size(260, 30),
                Location        = new Point(220, 15)
            };
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

            // Recommended preset button
            var recBtn = CreateSmallBtn("⭐ Recommended", ACCENT);
            recBtn.Location  = new Point(8, y + 46);
            recBtn.Width     = 174;
            recBtn.Height    = 30;
            recBtn.Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            recBtn.Click    += (s, e) =>
            {
                // Clear all first, then check recommended
                foreach (var kv in _tiles) kv.Value.IsChecked = false;
                foreach (var kv in _tiles) kv.Value.IsChecked = kv.Key.IsRecommended;
                UpdateSelectionCount();
            };

            // Scan status label
            _scanStatusLabel = new Label
            {
                Text      = _dm.WingetAvailable ? "🔍 Scanning installed apps..." : "",
                ForeColor = TEXT_SEC,
                Font      = new Font("Segoe UI", 7.5f),
                AutoSize  = false,
                Size      = new Size(174, 28),
                Location  = new Point(8, y + 84),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _sidebar.Controls.AddRange(new Control[] { selAll, deselAll, recBtn, _scanStatusLabel });
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

            var appList = apps.ToList();

            // Group by category and insert section headers
            bool showHeaders = true;
            var groups = appList.GroupBy(a => a.Category).OrderBy(g => g.Key);

            foreach (var group in groups)
            {
                if (showHeaders)
                {
                    var header = new SectionHeader(group.Key, CategoryEmoji(group.Key), SURFACE, TEXT_PRI, TEXT_SEC, ACCENT);
                    _appGrid.Controls.Add(header);
                    // Force header onto its own row; tiles start fresh on the next row
                    _appGrid.SetFlowBreak(header, true);
                }

                foreach (var app in group)
                {
                    if (!_tiles.TryGetValue(app, out var tile))
                    {
                        tile = new AppTile(app, SURFACE, SURFACE2, ACCENT, TEXT_PRI, TEXT_SEC, BORDER);
                        tile.IsChecked = app.IsRecommended;
                        tile.CheckedChanged += (s, e) => UpdateSelectionCount();
                        _tiles[app] = tile;
                    }
                    // Apply cached installed state if scan has results
                    if (_installedCache.TryGetValue(app, out bool installed))
                        tile.SetInstalled(installed);
                    _appGrid.Controls.Add(tile);
                }
            }

            _appGrid.ResumeLayout(true);
        }

        private static string CategoryEmoji(string cat) => cat switch
        {
            "Browsers"                 => "🌐",
            "Dev Tools"                => "💻",
            "Media & Entertainment"    => "🎬",
            "Productivity"             => "📋",
            "Gaming"                   => "🎮",
            "Utilities & System Tools" => "🔧",
            "Customization"            => "🎨",
            _                          => "📦"
        };

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

            // Show summary — loop allows retry of failed apps
            var pendingResults = results;
            while (true)
            {
                using var summary = new SummaryForm(pendingResults);
                var dr = summary.ShowDialog(this);

                if (dr != DialogResult.Retry || summary.FailedResults.Count == 0)
                    break;

                // Re-run only the failed apps
                var retryApps = summary.FailedResults.Select(r => r.App).ToList();
                Log($"[RETRY] Retrying {retryApps.Count} failed app(s)...");

                _isInstalling = true;
                _installBtn.Enabled = false;
                _installBtn.Text = "⏳ Retrying...";
                _overallProgress.Maximum = retryApps.Count;
                _overallProgress.Value   = 0;

                pendingResults = await _dm.InstallAllAsync(
                    retryApps, folder, preferWinget,
                    (app, status, msg) => this.Invoke((Action)(() =>
                    {
                        if (_tiles.TryGetValue(app, out var tile)) tile.SetStatus(status);
                        _statusLabel.Text = $"{app.Name}: {msg}";
                        Log($"[{app.Name}] {msg}");
                    })),
                    (done2, total2) => this.Invoke((Action)(() =>
                        _overallProgress.Value = done2)));

                _isInstalling = false;
                _installBtn.Enabled = true;
                _installBtn.Text    = "⬇  Install Selected";
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
        private bool _isInstalled = false;
        private readonly Color _normalBg;
        private readonly Color _checkedBg;
        private readonly Color _accentColor;
        private readonly Color _borderColor;
        private readonly Label _statusDot;

        // Green = already installed, pre-scan
        private static readonly Color GREEN       = Color.FromArgb(34, 197, 94);
        private static readonly Color INSTALLED_BG = Color.FromArgb(14, 30, 18); // dark green tint

        public event EventHandler CheckedChanged;

        public bool IsChecked
        {
            get => _checked;
            set
            {
                // Block selecting an installed tile
                if (value && _isInstalled) return;
                _checked  = value;
                BackColor = value ? _checkedBg : (_isInstalled ? INSTALLED_BG : _normalBg);
                Invalidate();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public AppTile(AppEntry app, Color normalBg, Color checkedBg, Color accent,
                       Color textPri, Color textSec, Color border)
        {
            _normalBg    = normalBg;
            _checkedBg   = checkedBg;
            _accentColor = accent;
            _borderColor = border;

            Size      = new Size(230, 125);
            BackColor = normalBg;
            Margin    = new Padding(6);
            Cursor    = Cursors.Hand;

            // Paint: border + selection checkbox OR installed checkmark
            this.Paint += (s, e) =>
            {
                var g = e.Graphics;

                // Border — green when installed, accent when selected, dim otherwise
                Color borderCol = _isInstalled ? Color.FromArgb(40, 140, 70)
                                : _checked     ? accent
                                :                border;
                using var pen = new System.Drawing.Pen(borderCol, _isInstalled ? 1.5f : 1.5f);
                g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);

                if (_isInstalled)
                {
                    // Green filled circle with white tick in top-right corner
                    var circleRect = new Rectangle(Width - 24, 4, 18, 18);
                    g.FillEllipse(new SolidBrush(GREEN), circleRect);
                    using var tickPen = new System.Drawing.Pen(Color.White, 2f);
                    g.DrawLines(tickPen, new[]
                    {
                        new Point(Width - 21, 13),
                        new Point(Width - 17, 17),
                        new Point(Width - 10,  8)
                    });
                }
                else if (_checked)
                {
                    // Accent filled square tick (existing selection indicator)
                    g.FillRectangle(new SolidBrush(accent), Width - 22, 6, 16, 16);
                    using var whitePen = new System.Drawing.Pen(Color.White, 2f);
                    g.DrawLines(whitePen, new[]
                    {
                        new Point(Width - 19, 14),
                        new Point(Width - 15, 18),
                        new Point(Width - 9,  9)
                    });
                }
            };

            var iconLbl = new Label
            {
                Text      = app.IconChar,
                Font      = new Font("Segoe UI Emoji", 16f),
                AutoSize  = false,
                Size      = new Size(36, 36),
                Location  = new Point(8, 8),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var nameLbl = new Label
            {
                Text         = app.Name,
                Font         = new Font("Segoe UI Semibold", 9f),
                ForeColor    = textPri,
                AutoSize     = false,
                Size         = new Size(148, 36),
                Location     = new Point(52, 7),
                BackColor    = Color.Transparent,
                AutoEllipsis = false,
                UseMnemonic  = false
            };

            var descLbl = new Label
            {
                Text      = app.Description,
                Font      = new Font("Segoe UI", 7.5f),
                ForeColor = textSec,
                AutoSize  = false,
                Size      = new Size(210, 28),
                Location  = new Point(10, 70),
                BackColor = Color.Transparent
            };

            // Method badge
            string method  = app.WingetId != null ? "winget" : "direct";
            Color  badgeBg = app.WingetId != null
                ? Color.FromArgb(30, 99, 102, 241)
                : Color.FromArgb(30, 251, 191, 36);
            Color  badgeFg = app.WingetId != null
                ? Color.FromArgb(160, 165, 255)
                : Color.FromArgb(251, 191, 36);

            var methodBadge = new Label
            {
                Text      = method,
                Font      = new Font("Segoe UI", 7f),
                ForeColor = badgeFg,
                BackColor = badgeBg,
                AutoSize  = true,
                Location  = new Point(10, 48),
                Padding   = new Padding(3, 1, 3, 1)
            };

            var catBadge = new Label
            {
                Text      = app.Category,
                Font      = new Font("Segoe UI", 6.5f),
                ForeColor = textSec,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(70, 51)
            };

            _statusDot = new Label
            {
                Text      = "",
                AutoSize  = true,
                Location  = new Point(10, 104),
                Font      = new Font("Segoe UI", 7.5f),
                BackColor = Color.Transparent
            };

            Controls.AddRange(new Control[] { iconLbl, nameLbl, descLbl, methodBadge, catBadge, _statusDot });

            // Click to toggle — blocked silently if already installed
            void Toggle(object s, EventArgs e)
            {
                if (_isInstalled) return;
                IsChecked = !_checked;
            }
            this.Click     += Toggle;
            iconLbl.Click  += Toggle;
            nameLbl.Click  += Toggle;
            descLbl.Click  += Toggle;
            catBadge.Click += Toggle;

            this.MouseEnter += (s, e) =>
            {
                if (!_checked && !_isInstalled) BackColor = Color.FromArgb(28, 28, 38);
            };
            this.MouseLeave += (s, e) =>
            {
                if (!_checked) BackColor = _isInstalled ? INSTALLED_BG : _normalBg;
            };
        }

        public void SetStatus(InstallStatus status)
        {
            switch (status)
            {
                case InstallStatus.Installing:
                case InstallStatus.Downloading:
                    _statusDot.Text      = "⏳ Installing...";
                    _statusDot.ForeColor = Color.FromArgb(251, 191, 36);
                    break;
                case InstallStatus.Success:
                    _statusDot.Text = "";
                    // Mark as installed so the green check appears immediately
                    SetInstalled(true);
                    break;
                case InstallStatus.Failed:
                    _statusDot.Text      = "✘ Failed";
                    _statusDot.ForeColor = Color.FromArgb(239, 68, 68);
                    break;
            }
        }

        public void SetInstalled(bool installed)
        {
            _isInstalled = installed;
            if (installed)
            {
                // Force deselect — installed apps can't be queued
                bool wasChecked = _checked;
                _checked  = false;
                BackColor = INSTALLED_BG;
                Cursor    = Cursors.Default;
                if (wasChecked)
                    CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                BackColor = _normalBg;
                Cursor    = Cursors.Hand;
            }
            Invalidate(); // repaint the corner checkmark
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  SECTION HEADER CONTROL
    // ─────────────────────────────────────────────────────────────────────────
    public class SectionHeader : Panel
    {
        public SectionHeader(string title, string emoji, Color bg, Color textPri, Color textSec, Color accent)
        {
            Height    = 44;
            Margin    = new Padding(6, 14, 6, 4);
            BackColor = Color.Transparent;
            Anchor    = AnchorStyles.Left | AnchorStyles.Right;

            // Resize width to match parent FlowLayoutPanel's client area
            this.ParentChanged += (s, e) =>
            {
                if (Parent == null) return;
                void Sync() => Width = Parent.ClientSize.Width - Margin.Horizontal;
                Sync();
                Parent.ClientSizeChanged += (ps, pe) => Sync();
            };

            var bar = new Panel
            {
                BackColor = accent,
                Size      = new Size(3, 26),
                Location  = new Point(4, 9)
            };

            var lbl = new Label
            {
                Text      = $"{emoji}  {title}",
                Font      = new Font("Segoe UI Semibold", 11f),
                ForeColor = textPri,
                AutoSize  = true,
                Location  = new Point(14, 10),
                BackColor = Color.Transparent
            };

            this.Paint += (s, e) =>
            {
                int lineY = Height - 6;
                using var pen = new System.Drawing.Pen(Color.FromArgb(45, 45, 65), 1);
                e.Graphics.DrawLine(pen, lbl.Right + 12, lineY, Width - 20, lineY);
            };

            Controls.AddRange(new Control[] { bar, lbl });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  POST-INSTALL SUMMARY FORM
    // ─────────────────────────────────────────────────────────────────────────
    public class SummaryForm : Form
    {
        private static readonly Color BG       = Color.FromArgb(13, 13, 18);
        private static readonly Color SURFACE  = Color.FromArgb(22, 22, 30);
        private static readonly Color SURFACE2 = Color.FromArgb(30, 30, 40);
        private static readonly Color ACCENT   = Color.FromArgb(99, 102, 241);
        private static readonly Color SUCCESS  = Color.FromArgb(34, 197, 94);
        private static readonly Color DANGER   = Color.FromArgb(239, 68, 68);
        private static readonly Color TEXT_PRI = Color.FromArgb(240, 240, 255);
        private static readonly Color TEXT_SEC = Color.FromArgb(140, 140, 170);
        private static readonly Color BORDER   = Color.FromArgb(40, 40, 58);

        public List<InstallResult> FailedResults { get; private set; } = new List<InstallResult>();

        public SummaryForm(List<InstallResult> results)
        {
            int ok   = results.Count(r => r.Status == InstallStatus.Success);
            int fail = results.Count(r => r.Status == InstallStatus.Failed);

            Text             = "Installation Summary";
            Size             = new Size(540, 520);
            MinimumSize      = new Size(440, 400);
            BackColor        = BG;
            ForeColor        = TEXT_PRI;
            Font             = new Font("Segoe UI", 9f);
            StartPosition    = FormStartPosition.CenterParent;
            FormBorderStyle  = FormBorderStyle.FixedDialog;
            MaximizeBox      = false;

            // ── Header ───────────────────────────────────────────────────────
            var header = new Panel
            {
                BackColor = SURFACE,
                Dock      = DockStyle.Top,
                Height    = 70
            };

            bool allOk = fail == 0;
            var titleLbl = new Label
            {
                Text      = allOk ? "✔  All apps installed!" : $"⚠  {fail} installation{(fail == 1 ? "" : "s")} failed",
                Font      = new Font("Segoe UI Semibold", 13f),
                ForeColor = allOk ? SUCCESS : DANGER,
                AutoSize  = true,
                Location  = new Point(18, 14)
            };

            var subLbl = new Label
            {
                Text      = $"{ok} succeeded   •   {fail} failed   •   {results.Count} total",
                Font      = new Font("Segoe UI", 9f),
                ForeColor = TEXT_SEC,
                AutoSize  = true,
                Location  = new Point(20, 42)
            };

            header.Controls.AddRange(new Control[] { titleLbl, subLbl });

            // ── Scroll area ──────────────────────────────────────────────────
            var scroll = new Panel
            {
                AutoScroll = true,
                BackColor  = BG,
                Dock       = DockStyle.Fill,
                Padding    = new Padding(14, 10, 14, 10)
            };

            int y = 10;

            // Successes
            if (ok > 0)
            {
                scroll.Controls.Add(MakeSectionLabel("Installed successfully", SUCCESS, y));
                y += 28;
                foreach (var r in results.Where(r => r.Status == InstallStatus.Success))
                {
                    scroll.Controls.Add(MakeResultRow(r.App.IconChar, r.App.Name, "✔", SUCCESS, y));
                    y += 38;
                }
                y += 8;
            }

            // Failures
            if (fail > 0)
            {
                scroll.Controls.Add(MakeSectionLabel("Failed", DANGER, y));
                y += 28;
                foreach (var r in results.Where(r => r.Status == InstallStatus.Failed))
                {
                    FailedResults.Add(r);
                    scroll.Controls.Add(MakeResultRow(r.App.IconChar, r.App.Name,
                        $"✘  {TrimError(r.Message)}", DANGER, y));
                    y += 38;
                }
            }

            // Pad the scroll area
            var spacer = new Panel { Height = 10, Top = y, BackColor = Color.Transparent };
            scroll.Controls.Add(spacer);

            // ── Footer ───────────────────────────────────────────────────────
            var footer = new Panel
            {
                BackColor = SURFACE,
                Dock      = DockStyle.Bottom,
                Height    = 58
            };

            var closeBtn = new Button
            {
                Text      = "Close",
                Size      = new Size(100, 34),
                BackColor = SURFACE2,
                ForeColor = TEXT_PRI,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
                Anchor    = AnchorStyles.Right | AnchorStyles.Top
            };
            closeBtn.FlatAppearance.BorderColor = BORDER;
            closeBtn.Location = new Point(this.ClientSize.Width - 118, 12);
            closeBtn.Click   += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            footer.Controls.Add(closeBtn);

            if (fail > 0)
            {
                var retryBtn = new Button
                {
                    Text      = $"↺  Retry {fail} Failed",
                    Size      = new Size(140, 34),
                    BackColor = DANGER,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font      = new Font("Segoe UI Semibold", 9f),
                    Cursor    = Cursors.Hand
                };
                retryBtn.FlatAppearance.BorderSize = 0;
                retryBtn.Location = new Point(this.ClientSize.Width - 268, 12);
                retryBtn.Click   += (s, e) => { DialogResult = DialogResult.Retry; Close(); };
                footer.Controls.Add(retryBtn);
            }

            Controls.AddRange(new Control[] { scroll, header, footer });
        }

        private Label MakeSectionLabel(string text, Color color, int y) => new Label
        {
            Text      = text.ToUpperInvariant(),
            Font      = new Font("Segoe UI Semibold", 7.5f),
            ForeColor = color,
            AutoSize  = true,
            Top       = y,
            Left      = 2,
            BackColor = Color.Transparent
        };

        private Panel MakeResultRow(string icon, string name, string statusText, Color statusColor, int y)
        {
            var row = new Panel
            {
                BackColor = SURFACE,
                Size      = new Size(490, 32),
                Top       = y,
                Left      = 0
            };

            row.Paint += (s, e) =>
            {
                using var pen = new System.Drawing.Pen(BORDER, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, row.Width - 1, row.Height - 1);
            };

            var iconLbl = new Label
            {
                Text      = icon,
                Font      = new Font("Segoe UI Emoji", 11f),
                AutoSize  = false,
                Size      = new Size(28, 28),
                Location  = new Point(4, 2),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var nameLbl = new Label
            {
                Text      = name,
                Font      = new Font("Segoe UI", 9f),
                ForeColor = TEXT_PRI,
                AutoSize  = true,
                Location  = new Point(36, 8),
                BackColor = Color.Transparent
            };

            var statusLbl = new Label
            {
                Text      = statusText,
                Font      = new Font("Segoe UI", 8f),
                ForeColor = statusColor,
                AutoSize  = true,
                BackColor = Color.Transparent
            };
            // Right-align the status
            statusLbl.Location = new Point(row.Width - statusLbl.PreferredWidth - 10, 9);
            statusLbl.Anchor   = AnchorStyles.Right | AnchorStyles.Top;

            row.Controls.AddRange(new Control[] { iconLbl, nameLbl, statusLbl });
            return row;
        }

        private static string TrimError(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return "Unknown error";
            return msg.Length > 48 ? msg.Substring(0, 45) + "..." : msg;
        }
    }

}