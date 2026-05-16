using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CornDownloader
{
    public class MainForm : Form
    {
        // ── Colours — Corn Studios website palette ────────────────────────
        private static readonly Color BG          = Color.FromArgb(  8,   8,  18);  // --bg:      #080812
        private static readonly Color BG2         = Color.FromArgb( 13,  13,  32);  // --bg2:     #0d0d20
        private static readonly Color SURFACE     = Color.FromArgb( 19,  18,  42);  // --surface: #13122a
        private static readonly Color SURFACE2    = Color.FromArgb( 26,  24,  53);  // --surface2:#1a1835
        private static readonly Color CARD        = Color.FromArgb( 16,  15,  34);  // --card:    #100f22
        private static readonly Color ACCENT      = Color.FromArgb(245, 200,  66);  // --accent:  #f5c842 corn gold
        private static readonly Color ACCENT_DIM  = Color.FromArgb(201, 153,  30);  // --accent-dim
        private static readonly Color METEOR      = Color.FromArgb(244,  81,  30);  // --meteor:  #f4511e
        private static readonly Color SUCCESS     = Color.FromArgb( 76, 175,  80);  // --corn-green
        private static readonly Color DANGER      = Color.FromArgb(239,  68,  68);
        private static readonly Color TEXT_PRI    = Color.FromArgb(240, 238, 252);  // --text:    #f0eefc
        private static readonly Color TEXT_SEC    = Color.FromArgb(160, 157, 192);  // --text-dim:#a09dc0
        private static readonly Color MUTED       = Color.FromArgb(101,  97, 160);  // --muted:   #6561a0
        private static readonly Color BORDER      = Color.FromArgb( 42,  40,  80);  // --border:  #2a2850
        private static readonly Color BORDER2     = Color.FromArgb( 61,  58, 112);  // --border2: #3d3a70
        private static readonly Color SKY_PURPLE  = Color.FromArgb(124,  58, 237);  // --sky-purple

        // ── State ────────────────────────────────────────────────────────────
        private readonly DownloadManager _dm;
        private readonly Dictionary<string, Panel> _categoryPanels = new Dictionary<string, Panel>();
        private readonly Dictionary<AppEntry, AppTile> _tiles = new Dictionary<AppEntry, AppTile>();
        private readonly Dictionary<string, Button> _sidebarBtns   = new Dictionary<string, Button>();
        private string _activeCategory = "All";
        private bool _isInstalling = false;
        private readonly Dictionary<AppEntry, bool> _installedCache = new Dictionary<AppEntry, bool>();
        private readonly Dictionary<AppEntry, bool> _upgradeCache   = new Dictionary<AppEntry, bool>();

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

        private AppSettings _settings;

        public MainForm()
        {
            _settings = SettingsManager.Load();
            _dm = new DownloadManager();
            // Build category list sorted alphabetically — matches the grid's OrderBy(g => g.Key)
            _categories = new[] { "All" }
                .Concat(AppCatalog.All.Select(a => a.Category).Distinct().OrderBy(c => c))
                .ToArray();
            InitializeComponent();
            ApplySettings();
            PopulateApps("All");
            UpdateSelectionCount();
            _ = RunStartupAsync();  // refresh sources, then scan installed + upgrades

            this.FormClosing += (s, e) => SaveSettings();
        }

        private async Task RunStartupAsync()
        {
            await RefreshWingetSourcesAsync();
            await ScanInstalledAsync();
            _ = ScanUpgradesAsync();
        }

        private Button       _upgradeBtn;

        private void ApplySettings()
        {
            // Restore window size
            if (_settings.WindowWidth > 0 && _settings.WindowHeight > 0)
                this.Size = new Size(_settings.WindowWidth, _settings.WindowHeight);

            // Restore folder path
            string folder = _settings.DownloadFolder;
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                folder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
            if (_folderBox != null) _folderBox.Text = folder;

            // Restore winget preference
            if (_preferWingetChk != null)
                _preferWingetChk.Checked = _settings.PreferWinget && _dm.WingetAvailable;
        }

        private void SaveSettings()
        {
            _settings.DownloadFolder = _folderBox?.Text.Trim() ?? "";
            _settings.PreferWinget   = _preferWingetChk?.Checked ?? true;
            _settings.WindowWidth    = this.Width;
            _settings.WindowHeight   = this.Height;
            SettingsManager.Save(_settings);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  WINGET SOURCE REFRESH
        // ─────────────────────────────────────────────────────────────────────
        private async Task RefreshWingetSourcesAsync()
        {
            if (!_dm.WingetAvailable) return;

            this.Invoke((Action)(() =>
            {
                if (_scanStatusLabel != null)
                {
                    _scanStatusLabel.Text      = "🔄 Refreshing winget sources...";
                    _scanStatusLabel.ForeColor = TEXT_SEC;
                }
            }));

            await _dm.RefreshSourcesAsync();

            this.Invoke((Action)(() =>
            {
                if (_scanStatusLabel != null)
                {
                    _scanStatusLabel.Text      = "🔍 Scanning installed apps...";
                    _scanStatusLabel.ForeColor = TEXT_SEC;
                }
            }));
        }

        // ─────────────────────────────────────────────────────────────────────
        //  INSTALLED DETECTION
        // ─────────────────────────────────────────────────────────────────────
        private async Task ScanInstalledAsync()
        {
            if (!_dm.WingetAvailable) return;

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
        //  UPGRADE DETECTION
        // ─────────────────────────────────────────────────────────────────────
        private async Task ScanUpgradesAsync()
        {
            if (!_dm.WingetAvailable) return;

            var updatableIds = await _dm.GetAvailableUpdatesAsync();

            foreach (var app in AppCatalog.All)
                _upgradeCache[app] = !string.IsNullOrEmpty(app.WingetId) &&
                                     updatableIds.Contains(app.WingetId);

            this.Invoke((Action)(() =>
            {
                int count = _upgradeCache.Values.Count(v => v);
                if (_upgradeBtn != null)
                {
                    _upgradeBtn.Visible = count > 0;
                    string plural = count == 1 ? "" : "s";
                    _upgradeBtn.Text    = $"⬆  Update {count} App{plural}";
                }

                // Mark updatable tiles with a badge
                foreach (var kv in _tiles)
                {
                    if (_upgradeCache.TryGetValue(kv.Key, out bool hasUpdate))
                        kv.Value.SetHasUpdate(hasUpdate);
                }
            }));
        }

        private async void OnUpgradeClicked(object sender, EventArgs e)
        {
            if (_isInstalling) return;

            var toUpgrade = _upgradeCache
                .Where(kv => kv.Value)
                .Select(kv => kv.Key)
                .ToList();

            if (toUpgrade.Count == 0) return;

            _isInstalling       = true;
            _upgradeBtn.Enabled = false;
            _upgradeBtn.Text    = "⏳ Updating...";
            _installBtn.Enabled = false;
            _overallProgress.Maximum = toUpgrade.Count;
            _overallProgress.Value   = 0;
            int done = 0;

            Log($"[UPGRADE] Upgrading {toUpgrade.Count} app(s) — {DateTime.Now:HH:mm:ss}");

            var results = new List<InstallResult>();
            foreach (var app in toUpgrade)
            {
                var result = await _dm.UpgradeAsync(app, msg =>
                    this.Invoke((Action)(() =>
                    {
                        _statusLabel.Text = $"{app.Name}: {msg}";
                        Log($"[{app.Name}] {msg}");
                    })));

                results.Add(result);
                done++;
                this.Invoke((Action)(() =>
                {
                    _overallProgress.Value = done;
                    if (_tiles.TryGetValue(app, out var tile))
                        tile.SetStatus(result.Status);
                    if (result.Status == InstallStatus.Success)
                        _upgradeCache[app] = false;
                }));
            }

            _isInstalling       = false;
            _installBtn.Enabled = true;

            int ok   = results.Count(r => r.Status == InstallStatus.Success);
            int fail = results.Count(r => r.Status == InstallStatus.Failed);
            _statusLabel.Text = $"Updates done — {ok} succeeded, {fail} failed.";
            Log($"[UPGRADE DONE] {ok}/{toUpgrade.Count} — {DateTime.Now:HH:mm:ss}");

            int remaining = _upgradeCache.Values.Count(v => v);
            _upgradeBtn.Visible = remaining > 0;
            string remPlural = remaining == 1 ? "" : "s";
            _upgradeBtn.Text    = remaining > 0 ? $"⬆  Update {remaining} App{remPlural}" : "";
            _upgradeBtn.Enabled = remaining > 0;

            using var summary = new SummaryForm(results);
            summary.ShowDialog(this);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  UI CONSTRUCTION
        // ─────────────────────────────────────────────────────────────────────
        private void InitializeComponent()
        {
            this.Text          = "Corn Downloader";
            this.Size          = new Size(1180, 760);
            this.MinimumSize   = new Size(900, 600);
            this.BackColor     = BG;
            this.ForeColor     = TEXT_PRI;
            this.Font          = new Font("Courier New", 8.5f, FontStyle.Regular);
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
            int w        = ClientSize.Width;
            int h        = ClientSize.Height;
            int topH     = 60;
            int botH     = 120;
            int sideW    = 210;
            int logH     = (_logPanel != null && _logPanel.Visible) ? _logPanel.Height : 0;
            int contentH = h - topH - botH - logH;

            _topBar.SetBounds(0, 0, w, topH);
            _sidebar.SetBounds(0, topH, sideW, contentH);
            _mainArea.SetBounds(sideW, topH, w - sideW, contentH);

            if (_logPanel != null)
            {
                _logPanel.SetBounds(0, topH + contentH, w, _logPanel.Height);
                if (_logPanel.Visible)
                    foreach (Control c in _logPanel.Controls)
                        if (c is Button) c.Location = new Point(_logPanel.Width - 80, 4);
            }

            _bottomBar.SetBounds(0, h - botH, w, botH);
        }

        // ── TOP BAR ──────────────────────────────────────────────────────────
        private void BuildTopBar()
        {
            _topBar = new Panel { BackColor = SURFACE, Dock = DockStyle.None };

            // Gold accent bar on left edge — mirrors website's section-label line
            var accentBar = new Panel
            {
                BackColor = ACCENT,
                Size      = new Size(3, 34),
                Location  = new Point(14, 13)
            };

            var titleLbl = new Label
            {
                Text      = "🌽  CORN_DOWNLOADER",
                Font      = new Font("Courier New", 9.5f, FontStyle.Bold),
                ForeColor = ACCENT,
                AutoSize  = true,
                Location  = new Point(24, 19)
            };

            _searchBox = new TextBox
            {
                PlaceholderText = "  search apps...",
                BackColor       = CARD,
                ForeColor       = TEXT_PRI,
                BorderStyle     = BorderStyle.FixedSingle,
                Font            = new Font("Courier New", 9f),
                Size            = new Size(240, 28),
                Location        = new Point(255, 16)
            };
            _searchBox.TextChanged += (s, e) => FilterApps(_searchBox.Text);

            _wingetBadge = new Label
            {
                AutoSize  = true,
                Font      = new Font("Courier New", 7.5f),
                Location  = new Point(510, 21)
            };
            UpdateWingetBadge();

            _upgradeBtn = new Button
            {
                Text      = "⬆  UPDATES AVAILABLE",
                AutoSize  = true,
                BackColor = METEOR,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Courier New", 7.5f, FontStyle.Bold),
                Location  = new Point(700, 14),
                Height    = 30,
                Visible   = false,
                Cursor    = Cursors.Hand
            };
            _upgradeBtn.FlatAppearance.BorderSize = 0;
            _upgradeBtn.Click += OnUpgradeClicked;

            _topBar.Controls.AddRange(new Control[] { accentBar, titleLbl, _searchBox, _wingetBadge, _upgradeBtn });
        }

        private void UpdateWingetBadge()
        {
            if (_dm.WingetAvailable)
            {
                _wingetBadge.Text      = "✦  winget detected";
                _wingetBadge.ForeColor = SUCCESS;
            }
            else
            {
                _wingetBadge.Text      = "⚠  winget not found — direct URLs only";
                _wingetBadge.ForeColor = METEOR;
            }
        }

        // ── SIDEBAR ──────────────────────────────────────────────────────────
        private void BuildSidebar()
        {
            _sidebar = new Panel { BackColor = SURFACE };

            // Sidebar header label — website "section-label" style
            var sideHeader = new Label
            {
                Text      = "// CATEGORIES",
                Font      = new Font("Courier New", 6.5f, FontStyle.Bold),
                ForeColor = MUTED,
                AutoSize  = true,
                Location  = new Point(12, 12),
                BackColor = Color.Transparent
            };
            _sidebar.Controls.Add(sideHeader);

            int y = 34;
            foreach (var cat in _categories)
            {
                var btn = CreateSidebarBtn(cat);
                btn.Location = new Point(8, y);
                btn.Width    = 194;
                _sidebar.Controls.Add(btn);
                y += 38;
            }

            // Divider line
            var divider = new Panel
            {
                BackColor = BORDER,
                Size      = new Size(178, 1),
                Location  = new Point(12, y + 6)
            };
            _sidebar.Controls.Add(divider);
            y += 14;

            // Select-All / Deselect-All
            var selAll = CreateSmallBtn("✦ ALL", ACCENT);
            selAll.Location  = new Point(8, y + 6);
            selAll.Width     = 92;
            selAll.ForeColor = Color.FromArgb(8, 8, 18);
            selAll.Click    += (s, e) => SetAllInView(true);

            var deselAll = CreateSmallBtn("✗ NONE", SURFACE2);
            deselAll.Location  = new Point(106, y + 6);
            deselAll.Width     = 96;
            deselAll.ForeColor = TEXT_SEC;
            deselAll.Click    += (s, e) => SetAllInView(false);

            // Recommended preset button — styled like website's btn-primary
            var recBtn = new Button
            {
                Text      = "★  RECOMMENDED",
                Size      = new Size(194, 32),
                Location  = new Point(8, y + 42),
                BackColor = ACCENT,
                ForeColor = Color.FromArgb(8, 8, 18),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Courier New", 7f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            recBtn.FlatAppearance.BorderSize = 0;
            recBtn.Click += (s, e) =>
            {
                foreach (var kv in _tiles) kv.Value.IsChecked = false;
                foreach (var kv in _tiles) kv.Value.IsChecked = kv.Key.IsRecommended;
                UpdateSelectionCount();
            };

            // Scan status label
            _scanStatusLabel = new Label
            {
                Text      = _dm.WingetAvailable ? "🔍 scanning..." : "",
                ForeColor = MUTED,
                Font      = new Font("Courier New", 6.5f),
                AutoSize  = false,
                Size      = new Size(194, 24),
                Location  = new Point(8, y + 82),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _sidebar.Controls.AddRange(new Control[] { selAll, deselAll, recBtn, _scanStatusLabel });
        }

        private Button CreateSidebarBtn(string category)
        {
            string emoji = category switch
            {
                "All"                      => "✦",
                "Browsers"                 => "🌐",
                "Dev Tools"                => "💻",
                "Media & Entertainment"    => "🎬",
                "Productivity"             => "📋",
                "Gaming"                   => "🎮",
                "Utilities & System Tools" => "🔧",
                "Customization"            => "🎨",
                _                          => "◈"
            };

            string display = category switch
            {
                "Media & Entertainment"    => "MEDIA & ENTERTAIN.",
                "Utilities & System Tools" => "UTILITIES & SYS.",
                _                          => category.ToUpper()
            };

            int total = category == "All"
                ? AppCatalog.All.Count
                : AppCatalog.All.Count(a => a.Category == category);

            bool active = _activeCategory == category;
            var btn = new Button
            {
                Text          = $"  {emoji}  {display}",
                TextAlign     = ContentAlignment.MiddleLeft,
                FlatStyle     = FlatStyle.Flat,
                BackColor     = active ? Color.FromArgb(40, 245, 200, 66) : Color.Transparent,
                ForeColor     = active ? ACCENT : TEXT_SEC,
                Font          = new Font("Courier New", 7f, active ? FontStyle.Bold : FontStyle.Regular),
                Height        = 32,
                Padding       = new Padding(0, 0, 36, 0),
                AutoEllipsis  = true,
                AutoSize      = false,
                Cursor        = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize         = 0;
            btn.FlatAppearance.BorderColor        = active ? ACCENT : Color.FromArgb(1, 8, 8, 18);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 245, 200, 66);

            // Count badge — sits flush on the right edge
            var badge = new Label
            {
                Text      = total.ToString(),
                AutoSize  = false,
                Size      = new Size(28, 14),
                Font      = new Font("Courier New", 6f, FontStyle.Bold),
                ForeColor = MUTED,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.Controls.Add(badge);
            void PositionBadge() => badge.Location = new Point(btn.Width - 32, (btn.Height - 14) / 2);
            btn.SizeChanged  += (s, e) => PositionBadge();
            btn.HandleCreated += (s, e) => PositionBadge();

            // Left gold bar for active state — painted on
            btn.Paint += (s, e) =>
            {
                PositionBadge();
                if (_activeCategory == category)
                {
                    using var b = new SolidBrush(ACCENT);
                    e.Graphics.FillRectangle(b, 0, 4, 2, btn.Height - 8);
                }
            };

            btn.Click += (s, e) =>
            {
                _activeCategory = category;
                RefreshSidebarButtons();
                PopulateApps(category);
            };

            _sidebarBtns[category] = btn;
            return btn;
        }

        /// <summary>
        /// Refreshes the count badge on every sidebar button.
        /// Shows "selected/total" in gold when anything is checked, plain total otherwise.
        /// </summary>
        private void UpdateSidebarCounts()
        {
            foreach (var kv in _sidebarBtns)
            {
                string cat    = kv.Key;
                var    btn    = kv.Value;
                int    total  = cat == "All"
                    ? AppCatalog.All.Count
                    : AppCatalog.All.Count(a => a.Category == cat);
                int    sel    = _tiles
                    .Where(t => (cat == "All" || t.Key.Category == cat) && t.Value.IsChecked)
                    .Count();

                if (btn.Controls.Count > 0 && btn.Controls[0] is Label badge)
                {
                    badge.Text      = sel > 0 ? $"{sel}/{total}" : total.ToString();
                    badge.ForeColor = sel > 0 ? ACCENT : MUTED;
                }
            }
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
                Font      = new Font("Courier New", 7f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void RefreshSidebarButtons()
        {
            foreach (Control c in _sidebar.Controls)
            {
                if (c is Button btn && btn.Text.Contains("  "))
                {
                    // Extract category from button text — format is "  emoji  CATEGORY"
                    string raw = btn.Text.Trim();
                    // Find the category by matching against known categories
                    string matchedCat = _categories.FirstOrDefault(cat =>
                        raw.EndsWith(cat.ToUpper(), StringComparison.OrdinalIgnoreCase)) ?? "";
                    bool active = matchedCat == _activeCategory;
                    btn.BackColor = active ? Color.FromArgb(40, 245, 200, 66) : Color.Transparent;
                    btn.ForeColor = active ? ACCENT : TEXT_SEC;
                    btn.Font      = new Font("Courier New", 7f, active ? FontStyle.Bold : FontStyle.Regular);
                    btn.Invalidate();
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
                Padding         = new Padding(12),
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
                    var header = new SectionHeader(group.Key, CategoryEmoji(group.Key), CARD, TEXT_PRI, MUTED, ACCENT);
                    _appGrid.Controls.Add(header);
                    // Force header onto its own row; tiles start fresh on the next row
                    _appGrid.SetFlowBreak(header, true);
                }

                foreach (var app in group)
                {
                    bool isNew = !_tiles.TryGetValue(app, out var tile);
                    if (isNew)
                    {
                        tile = new AppTile(app, CARD, SURFACE2, ACCENT, TEXT_PRI, TEXT_SEC, BORDER);
                        tile.IsChecked = app.IsRecommended;
                        tile.CheckedChanged += (s, e) => UpdateSelectionCount();
                        _tiles[app] = tile;
                    }
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

            // Top border line
            _bottomBar.Paint += (s, e) =>
            {
                using var pen = new System.Drawing.Pen(BORDER, 1);
                e.Graphics.DrawLine(pen, 0, 0, _bottomBar.Width, 0);
            };

            // Row 1: folder picker
            var folderLbl = new Label
            {
                Text      = "// SAVE TO",
                ForeColor = MUTED,
                Font      = new Font("Courier New", 7f, FontStyle.Bold),
                AutoSize  = true,
                Location  = new Point(16, 14)
            };

            _folderBox = new TextBox
            {
                Text        = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads",
                BackColor   = CARD,
                ForeColor   = TEXT_PRI,
                BorderStyle = BorderStyle.FixedSingle,
                Font        = new Font("Courier New", 8.5f),
                Size        = new Size(320, 24),
                Location    = new Point(110, 11)
            };

            _browseBtn = new Button
            {
                Text      = "BROWSE",
                Size      = new Size(70, 24),
                Location  = new Point(438, 11),
                BackColor = SURFACE2,
                ForeColor = TEXT_SEC,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Courier New", 6.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            _browseBtn.FlatAppearance.BorderColor = BORDER2;
            _browseBtn.FlatAppearance.BorderSize  = 1;
            _browseBtn.Click += (s, e) =>
            {
                using var dlg = new FolderBrowserDialog();
                if (dlg.ShowDialog() == DialogResult.OK)
                    _folderBox.Text = dlg.SelectedPath;
            };

            _preferWingetChk = new CheckBox
            {
                Text      = "prefer winget",
                ForeColor = MUTED,
                Font      = new Font("Courier New", 7.5f),
                Checked   = _dm.WingetAvailable,
                Enabled   = _dm.WingetAvailable,
                AutoSize  = true,
                Location  = new Point(524, 14)
            };

            // Row 2: progress + status
            _overallProgress = new ProgressBar
            {
                Size     = new Size(460, 6),
                Location = new Point(16, 50),
                Style    = ProgressBarStyle.Continuous,
                Minimum  = 0,
                Maximum  = 100,
                Value    = 0
            };

            _statusLabel = new Label
            {
                Text      = "ready.",
                ForeColor = MUTED,
                Font      = new Font("Courier New", 7.5f),
                AutoSize  = true,
                Location  = new Point(16, 62)
            };

            _selectionCountLabel = new Label
            {
                ForeColor = ACCENT,
                Font      = new Font("Courier New", 8f, FontStyle.Bold),
                AutoSize  = true,
                Location  = new Point(490, 52)
            };

            _clearBtn = new Button
            {
                Text      = "✗ CLEAR",
                Size      = new Size(100, 36),
                Location  = new Point(800, 42),
                BackColor = SURFACE2,
                ForeColor = TEXT_SEC,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Courier New", 7.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            _clearBtn.FlatAppearance.BorderColor = BORDER2;
            _clearBtn.FlatAppearance.BorderSize  = 1;
            _clearBtn.Click += (s, e) => SetAllInView(false);

            _installBtn = new Button
            {
                Text      = "⬇  INSTALL",
                Size      = new Size(160, 36),
                Location  = new Point(910, 42),
                BackColor = ACCENT,
                ForeColor = Color.FromArgb(8, 8, 18),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Courier New", 9f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            _installBtn.FlatAppearance.BorderSize = 0;
            _installBtn.Click += OnInstallClicked;

            // Log toggle
            var logToggle = new Button
            {
                Text      = "// LOG",
                Size      = new Size(65, 24),
                Location  = new Point(914, 11),
                BackColor = Color.Transparent,
                ForeColor = MUTED,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Courier New", 7f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            logToggle.FlatAppearance.BorderColor = BORDER;
            logToggle.FlatAppearance.BorderSize  = 1;
            logToggle.Click += (s, e) => ToggleLog();

            _bottomBar.Controls.AddRange(new Control[] {
                folderLbl, _folderBox, _browseBtn, _preferWingetChk,
                _overallProgress, _statusLabel, _selectionCountLabel,
                _clearBtn, _installBtn, logToggle
            });

            // Log panel — terminal green on near-black, matches website code aesthetic
            _logBox = new RichTextBox
            {
                BackColor   = Color.FromArgb(8, 8, 18),
                ForeColor   = ACCENT,
                BorderStyle = BorderStyle.None,
                ReadOnly    = true,
                Font        = new Font("Courier New", 8.5f),
                Dock        = DockStyle.Fill,
                ScrollBars  = RichTextBoxScrollBars.Vertical
            };

            _logPanel = new Panel
            {
                BackColor = Color.FromArgb(8, 8, 18),
                Visible   = false,
                Height    = 160
            };

            // Close button inside the log panel
            var logClose = new Button
            {
                Text      = "✗ CLOSE",
                Size      = new Size(75, 22),
                BackColor = Color.Transparent,
                ForeColor = MUTED,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Courier New", 6.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            logClose.FlatAppearance.BorderSize = 0;
            logClose.Click += (s, e) => ToggleLog();

            _logBox.Dock = DockStyle.Fill;
            _logPanel.Controls.Add(_logBox);
            _logPanel.Controls.Add(logClose);

            // Add to the form — LayoutPanels positions it between mainArea and bottomBar
            this.Controls.Add(_logPanel);
        }

        private void ToggleLog()
        {
            _logPanel.Visible = !_logPanel.Visible;
            LayoutPanels(); // reflow everything so log gets its own space
            if (_logPanel.Visible)
            {
                foreach (Control c in _logPanel.Controls)
                    if (c is Button) c.Location = new Point(_logPanel.Width - 80, 4);
                _logPanel.BringToFront();
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
                        {
                            tile.SetStatus(status);

                            // Parse a percentage out of winget/download progress lines
                            // e.g. "Downloading ... 47%" or "Downloading: 47 %"
                            int pct = ParsePercent(msg);
                            if (pct >= 0 &&
                                (status == InstallStatus.Installing || status == InstallStatus.Downloading))
                                tile.SetProgress(pct);
                        }
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
            _installBtn.Text    = "⬇  INSTALL";
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
                _installBtn.Text    = "⬇  INSTALL";
            }
        }

        private void UpdateSelectionCount()
        {
            int count = _tiles.Values.Count(t => t.IsChecked);
            if (_selectionCountLabel != null)
            {
                string appPlural = count == 1 ? "" : "s";
                _selectionCountLabel.Text = count == 0
                    ? "no apps selected"
                    : $"{count} app{appPlural} selected";
            }
            UpdateSidebarCounts();
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

        private static string TrimError(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return "Unknown error";
            return msg.Length > 48 ? msg.Substring(0, 45) + "..." : msg;
        }

        private static int ParsePercent(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return -1;
            int idx = msg.IndexOf('%');
            if (idx <= 0) return -1;
            int end = idx - 1;
            while (end >= 0 && msg[end] == ' ') end--;
            int start = end;
            while (start > 0 && char.IsDigit(msg[start - 1])) start--;
            if (start > end) return -1;
            if (int.TryParse(msg.Substring(start, end - start + 1), out int pct))
                return Math.Clamp(pct, 0, 100);
            return -1;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  APP TILE CONTROL
    // ─────────────────────────────────────────────────────────────────────────
    public class AppTile : Panel
    {
        private bool _checked;
        private bool _isInstalled = false;
        private bool _hasUpdate   = false;
        private readonly Color _normalBg;
        private readonly Color _checkedBg;
        private readonly Label       _statusDot;
        private readonly ProgressBar _progressBar;
        private Label _updateBadge;

        public event EventHandler CheckedChanged;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool IsChecked
        {
            get => _checked;
            set
            {
                if (value && _isInstalled) return;
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

            Size      = new Size(230, 125);
            BackColor = normalBg;
            Margin    = new Padding(6);
            Cursor    = Cursors.Hand;

            this.Paint += (s, e) =>
            {
                var g = e.Graphics;

                Color borderCol = _isInstalled
                    ? Color.FromArgb(42, 40, 80)   // BORDER — muted when installed
                    : _checked ? accent : border;

                using (var pen = new System.Drawing.Pen(borderCol, _isInstalled ? 1f : 1.5f))
                    g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);

                // Installed: subtle green top-edge bar
                if (_isInstalled)
                {
                    using (var b = new SolidBrush(Color.FromArgb(60, 76, 175, 80)))
                        g.FillRectangle(b, 1, 1, Width - 2, 3);
                }
                else if (_checked)
                {
                    using (var brush = new SolidBrush(accent))
                        g.FillRectangle(brush, Width - 22, 6, 16, 16);
                    using (var whitePen = new System.Drawing.Pen(Color.White, 2f))
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
                Font      = new Font("Segoe UI Emoji", 15f),
                AutoSize  = false,
                Size      = new Size(34, 34),
                Location  = new Point(8, 8),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var nameLbl = new Label
            {
                Text         = app.Name,
                Font         = new Font("Courier New", 9f, FontStyle.Bold),
                ForeColor    = textPri,
                AutoSize     = false,
                Size         = new Size(152, 34),
                Location     = new Point(48, 6),
                BackColor    = Color.Transparent,
                AutoEllipsis = true,
                UseMnemonic  = false
            };

            var descLbl = new Label
            {
                Text      = app.Description,
                Font      = new Font("Segoe UI", 7f),
                ForeColor = textSec,
                AutoSize  = false,
                Size      = new Size(210, 26),
                Location  = new Point(10, 72),
                BackColor = Color.Transparent
            };

            // Method badge — website "tag" style with border
            string method  = app.WingetId != null ? "winget" : "direct";
            Color  badgeFg = app.WingetId != null
                ? Color.FromArgb(245, 200, 66)   // gold for winget
                : Color.FromArgb(160, 157, 192);  // muted for direct

            var methodBadge = new Label
            {
                Text      = method,
                Font      = new Font("Courier New", 6f),
                ForeColor = badgeFg,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(10, 50),
                Padding   = new Padding(2, 1, 2, 1)
            };
            // Draw border on badge via Paint
            methodBadge.Paint += (s, e) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(61, 58, 112), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, methodBadge.Width - 1, methodBadge.Height - 1);
            };

            var catBadge = new Label
            {
                Text      = app.Category.ToUpper(),
                Font      = new Font("Courier New", 5.5f),
                ForeColor = Color.FromArgb(101, 97, 160),
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(methodBadge.PreferredWidth + 16, 52)
            };

            _statusDot = new Label
            {
                Text      = "",
                AutoSize  = true,
                Location  = new Point(10, 104),
                Font      = new Font("Courier New", 7f),
                BackColor = Color.Transparent
            };

            _progressBar = new ProgressBar
            {
                Size     = new Size(this.Width - 20, 4),
                Location = new Point(10, 118),
                Style    = ProgressBarStyle.Continuous,
                Minimum  = 0,
                Maximum  = 100,
                Value    = 0,
                Visible  = false
            };

            _updateBadge = new Label
            {
                Text      = "⬆ update available",
                AutoSize  = true,
                Location  = new Point(10, 104),
                Font      = new Font("Courier New", 6.5f),
                ForeColor = Color.FromArgb(244, 81, 30),
                BackColor = Color.Transparent,
                Visible   = false
            };

            Controls.AddRange(new Control[] { iconLbl, nameLbl, descLbl, methodBadge, catBadge, _statusDot, _progressBar, _updateBadge });

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

            this.MouseEnter += (s, e) => { if (!_checked) BackColor = Color.FromArgb(19, 18, 45); };
            this.MouseLeave += (s, e) => { if (!_checked) BackColor = _normalBg; };
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
                    _statusDot.Text      = "✔ Done";
                    _statusDot.ForeColor = Color.FromArgb(34, 197, 94);
                    _progressBar.Visible = false;
                    IsChecked = false;
                    break;
                case InstallStatus.Failed:
                    _statusDot.Text      = "✘ Failed";
                    _statusDot.ForeColor = Color.FromArgb(239, 68, 68);
                    _progressBar.Visible = false;
                    break;
            }
        }

        /// <summary>
        /// Updates the per-tile progress bar. Pass -1 to show indeterminate (marquee).
        /// Pass 0–100 for a real percentage. Call with 100 to auto-hide.
        /// </summary>
        public void SetProgress(int percent)
        {
            if (_progressBar == null) return;
            if (percent < 0)
            {
                _progressBar.Style   = ProgressBarStyle.Marquee;
                _progressBar.Visible = true;
            }
            else if (percent >= 100)
            {
                _progressBar.Visible = false;
            }
            else
            {
                _progressBar.Style   = ProgressBarStyle.Continuous;
                _progressBar.Value   = Math.Min(percent, 100);
                _progressBar.Visible = true;
                _statusDot.Text      = $"⏳ {percent}%";
                _statusDot.ForeColor = Color.FromArgb(251, 191, 36);
            }
        }

        public void SetInstalled(bool installed)
        {
            _isInstalled = installed;
            if (installed)
            {
                _statusDot.Text      = "✔ Installed";
                _statusDot.ForeColor = Color.FromArgb(34, 197, 94);
                _statusDot.Visible   = true;
                BackColor            = _normalBg;
                _checked             = false;
                Cursor               = Cursors.Default;
            }
            Invalidate();
        }

        public void SetHasUpdate(bool hasUpdate)
        {
            _hasUpdate = hasUpdate;
            if (_updateBadge != null)
            {
                _updateBadge.Visible = hasUpdate;
                _statusDot.Visible   = !hasUpdate;
            }
            Invalidate();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  SECTION HEADER CONTROL
    // ─────────────────────────────────────────────────────────────────────────
    public class SectionHeader : Panel
    {
        public SectionHeader(string title, string emoji, Color bg, Color textPri, Color textSec, Color accent)
        {
            Height    = 48;
            Margin    = new Padding(6, 18, 6, 4);
            BackColor = Color.Transparent;
            Anchor    = AnchorStyles.Left | AnchorStyles.Right;

            Control _lastParent = null;
            EventHandler syncHandler = null;
            syncHandler = (ps, pe) =>
            {
                if (_lastParent != null)
                    Width = _lastParent.ClientSize.Width - Margin.Horizontal;
            };
            this.ParentChanged += (s, e) =>
            {
                if (_lastParent != null)
                    _lastParent.ClientSizeChanged -= syncHandler;
                _lastParent = Parent;
                if (_lastParent != null)
                {
                    Width = _lastParent.ClientSize.Width - Margin.Horizontal;
                    _lastParent.ClientSizeChanged += syncHandler;
                }
            };

            // Gold left bar — website "section-label" style
            var bar = new Panel
            {
                BackColor = accent,
                Size      = new Size(2, 22),
                Location  = new Point(4, 13)
            };

            // Monospace label — mirrors website's "01 — Projects" style
            var lbl = new Label
            {
                Text      = $"{emoji}  {title.ToUpper()}",
                Font      = new Font("Courier New", 8.5f, FontStyle.Bold),
                ForeColor = accent,
                AutoSize  = true,
                Location  = new Point(12, 14),
                BackColor = Color.Transparent
            };

            this.Paint += (s, e) =>
            {
                // Gradient line after the label — matches website's ::after line
                int lineY = Height / 2 + 2;
                using var pen = new System.Drawing.Pen(Color.FromArgb(42, 40, 80), 1);
                e.Graphics.DrawLine(pen, lbl.Right + 14, lineY, Width - 20, lineY);
            };

            Controls.AddRange(new Control[] { bar, lbl });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  POST-INSTALL SUMMARY FORM
    // ─────────────────────────────────────────────────────────────────────────
    public class SummaryForm : Form
    {
        private static readonly Color BG       = Color.FromArgb(  8,   8,  18);
        private static readonly Color SURFACE  = Color.FromArgb( 19,  18,  42);
        private static readonly Color SURFACE2 = Color.FromArgb( 26,  24,  53);
        private static readonly Color ACCENT   = Color.FromArgb(245, 200,  66);
        private static readonly Color SUCCESS  = Color.FromArgb( 76, 175,  80);
        private static readonly Color DANGER   = Color.FromArgb(239,  68,  68);
        private static readonly Color TEXT_PRI = Color.FromArgb(240, 238, 252);
        private static readonly Color TEXT_SEC = Color.FromArgb(160, 157, 192);
        private static readonly Color BORDER   = Color.FromArgb( 42,  40,  80);

        public List<InstallResult> FailedResults { get; private set; } = new List<InstallResult>();

        public SummaryForm(List<InstallResult> results)
        {
            int ok   = results.Count(r => r.Status == InstallStatus.Success);
            int fail = results.Count(r => r.Status == InstallStatus.Failed);

            Text             = "// INSTALL SUMMARY";
            Size             = new Size(560, 540);
            MinimumSize      = new Size(440, 400);
            BackColor        = BG;
            ForeColor        = TEXT_PRI;
            Font             = new Font("Courier New", 8.5f);
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
            string instPlural = fail == 1 ? "" : "s";
            var titleLbl = new Label
            {
                Text      = allOk ? "✔  All apps installed!" : $"⚠  {fail} installation{instPlural} failed",
                Font      = new Font("Courier New", 11f, FontStyle.Bold),
                ForeColor = allOk ? SUCCESS : DANGER,
                AutoSize  = true,
                Location  = new Point(18, 14)
            };

            var subLbl = new Label
            {
                Text      = $"{ok} succeeded   •   {fail} failed   •   {results.Count} total",
                Font      = new Font("Courier New", 8.5f),
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
                    Font      = new Font("Courier New", 8.5f, FontStyle.Bold),
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
            Font      = new Font("Courier New", 7f, FontStyle.Bold),
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
                Font      = new Font("Courier New", 8.5f),
                ForeColor = TEXT_PRI,
                AutoSize  = true,
                Location  = new Point(36, 8),
                BackColor = Color.Transparent
            };

            var statusLbl = new Label
            {
                Text      = statusText,
                Font      = new Font("Courier New", 7.5f),
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

    // ─────────────────────────────────────────────────────────────────────────
    //  SETTINGS PERSISTENCE
    // ─────────────────────────────────────────────────────────────────────────
    internal class AppSettings
    {
        public string  DownloadFolder  { get; set; } = "";
        public bool    PreferWinget    { get; set; } = true;
        public int     WindowWidth     { get; set; } = 1180;
        public int     WindowHeight    { get; set; } = 760;
    }

    internal static class SettingsManager
    {
        private static readonly string _path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CornStudios", "CornDownloader", "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(_path))
                {
                    string json = File.ReadAllText(_path);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { }
            return new AppSettings();
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                File.WriteAllText(_path,
                    JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }
    }

}