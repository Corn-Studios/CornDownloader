using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CornDownloader
{
    // ─────────────────────────────────────────────────────────────────────────
    //  DPI HELPER — PerMonitorV2 scaling (Corn Studios standard)
    // ─────────────────────────────────────────────────────────────────────────
    public static class Dpi
    {
        private const float BASE_DPI = 96f;
        public static float Current { get; private set; } = BASE_DPI;
        public static float Scale   => Current / BASE_DPI;

        public static int   S(int pixels)   => (int)Math.Round(pixels * Scale);
        public static float S(float pixels) => pixels * Scale;
        public static Size  S(Size sz)      => new Size(S(sz.Width), S(sz.Height));
        public static Point S(Point pt)     => new Point(S(pt.X), S(pt.Y));

        public static void Update(Control ctrl)
        {
            try   { Current = ctrl.DeviceDpi; }
            catch { try { using var g = ctrl.CreateGraphics(); Current = g.DpiX; } catch { } }
        }
        public static void Update(int newDpi) { if (newDpi > 0) Current = newDpi; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  TASKBAR BADGE (ITaskbarList3 COM interop)
    // ─────────────────────────────────────────────────────────────────────────
    internal static class TaskbarBadge
    {
        [ComImport, Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ITaskbarList3
        {
            void HrInit();
            void AddTab(IntPtr hwnd);
            void DeleteTab(IntPtr hwnd);
            void ActivateTab(IntPtr hwnd);
            void SetActiveAlt(IntPtr hwnd);
            void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);
            void SetProgressValue(IntPtr hwnd, ulong ullCompleted, ulong ullTotal);
            void SetProgressState(IntPtr hwnd, int tbpFlags);
            void RegisterTab(IntPtr hwndTab, IntPtr hwndMDI);
            void UnregisterTab(IntPtr hwndTab);
            void SetTabOrder(IntPtr hwndTab, IntPtr hwndInsertBefore);
            void SetTabActive(IntPtr hwndTab, IntPtr hwndMDI, uint dwReserved);
            void ThumbBarAddButtons(IntPtr hwnd, uint cButtons, IntPtr pButton);
            void ThumbBarUpdateButtons(IntPtr hwnd, uint cButtons, IntPtr pButton);
            void ThumbBarSetImageList(IntPtr hwnd, IntPtr himl);
            void SetOverlayIcon(IntPtr hwnd, IntPtr hIcon, [MarshalAs(UnmanagedType.LPWStr)] string pszDescription);
            void SetThumbnailTooltip(IntPtr hwnd, [MarshalAs(UnmanagedType.LPWStr)] string pszTip);
            void SetThumbnailClip(IntPtr hwnd, ref System.Drawing.Rectangle prcClip);
        }

        [ComImport, Guid("56fdf344-fd6d-11d0-958a-006097c9a090"),
         ClassInterface(ClassInterfaceType.None)]
        private class TaskbarList { }

        private static ITaskbarList3 _taskbar;

        static TaskbarBadge()
        {
            try { _taskbar = (ITaskbarList3)new TaskbarList(); _taskbar.HrInit(); }
            catch { _taskbar = null; }
        }

        /// <summary>
        /// Sets a numeric overlay badge on the taskbar button.
        /// Pass 0 to clear the badge.
        /// </summary>
        public static void SetCount(IntPtr hwnd, int count)
        {
            if (_taskbar == null) return;
            try
            {
                if (count <= 0)
                {
                    _taskbar.SetOverlayIcon(hwnd, IntPtr.Zero, null);
                    return;
                }

                // Render a small gold-on-dark circle with the count number
                int sz = 16;
                using var bmp = new Bitmap(sz, sz, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode     = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    g.Clear(Color.Transparent);

                    using var bgBrush = new SolidBrush(Color.FromArgb(245, 200, 66));  // ACCENT gold
                    g.FillEllipse(bgBrush, 0, 0, sz - 1, sz - 1);

                    string label = count > 99 ? "99+" : count.ToString();
                    float  fs    = label.Length > 2 ? 5.5f : 7f;
                    using var font = new Font("Courier New", fs, FontStyle.Bold);
                    using var tb   = new SolidBrush(Color.FromArgb(8, 8, 18));
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(label, font, tb, new RectangleF(0, 0, sz, sz), sf);
                }

                var hIcon = bmp.GetHicon();
                try   { _taskbar.SetOverlayIcon(hwnd, hIcon, $"{count} apps selected"); }
                finally { DestroyIcon(hIcon); }
            }
            catch { }
        }

        [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr hIcon);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  WINDOW ICON GENERATOR  (corn-coloured 🌽 bitmap → Icon)
    // ─────────────────────────────────────────────────────────────────────────
    internal static class AppIconBuilder
    {
        public static Icon Build(int size = 32)
        {
            using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // Dark bg circle
                using var bgBrush = new SolidBrush(Color.FromArgb(19, 18, 42));
                g.FillEllipse(bgBrush, 0, 0, size - 1, size - 1);

                // Gold ring
                using var ring = new Pen(Color.FromArgb(245, 200, 66), size > 24 ? 1.5f : 1f);
                g.DrawEllipse(ring, 1, 1, size - 3, size - 3);

                // Corn emoji centred
                float fs = size * 0.45f;
                using var font = new Font("Segoe UI Emoji", fs, GraphicsUnit.Pixel);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("🌽", font, Brushes.White, new RectangleF(0, 0, size, size), sf);
            }

            return Icon.FromHandle(bmp.GetHicon());
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  SELECTION PACK  (export / import format)
    // ─────────────────────────────────────────────────────────────────────────
    internal class SelectionPack
    {
        public string Version        { get; set; } = "1";
        public string CreatedAt      { get; set; }
        public List<PackedApp> Apps  { get; set; } = new();
    }

    internal class PackedApp
    {
        // Id is the primary match key (stable across renames). Name is kept alongside it
        // purely for human readability when someone opens the .corn file, and as a fallback
        // match for packs exported before Id existed.
        public string Id            { get; set; }
        public string Name          { get; set; }
        public string PinnedVersion { get; set; }   // null = latest
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  MAIN FORM
    // ─────────────────────────────────────────────────────────────────────────
    public class MainForm : Form
    {
        // ── Colours ──────────────────────────────────────────────────────────
        private static readonly Color BG         = Color.FromArgb(  8,   8,  18);
        private static readonly Color BG2        = Color.FromArgb( 13,  13,  32);
        private static readonly Color SURFACE    = Color.FromArgb( 19,  18,  42);
        private static readonly Color SURFACE2   = Color.FromArgb( 26,  24,  53);
        private static readonly Color CARD       = Color.FromArgb( 16,  15,  34);
        private static readonly Color ACCENT     = Color.FromArgb(245, 200,  66);
        private static readonly Color ACCENT_DIM = Color.FromArgb(201, 153,  30);
        private static readonly Color METEOR     = Color.FromArgb(244,  81,  30);
        private static readonly Color SUCCESS    = Color.FromArgb( 76, 175,  80);
        private static readonly Color DANGER     = Color.FromArgb(239,  68,  68);
        private static readonly Color TEXT_PRI   = Color.FromArgb(240, 238, 252);
        private static readonly Color TEXT_SEC   = Color.FromArgb(160, 157, 192);
        private static readonly Color MUTED      = Color.FromArgb(101,  97, 160);
        private static readonly Color BORDER     = Color.FromArgb( 42,  40,  80);
        private static readonly Color BORDER2    = Color.FromArgb( 61,  58, 112);
        private static readonly Color SKY_PURPLE = Color.FromArgb(124,  58, 237);

        // ── State ─────────────────────────────────────────────────────────────
        private readonly DownloadManager _dm;
        private readonly Dictionary<AppEntry, AppTile>   _tiles        = new();
        private readonly Dictionary<string, Button>      _sidebarBtns  = new();
        private readonly Dictionary<AppEntry, bool>      _installedCache = new();
        private readonly Dictionary<AppEntry, bool>      _upgradeCache   = new();
        private string _activeCategory = "All";
        private bool   _isInstalling   = false;
        private bool   _initialized    = false;
        private System.Threading.CancellationTokenSource _cts;
        // Tracks the fire-and-forget startup upgrade scan (see RunStartupAsync). Installs
        // and upgrades await this first so they never call winget concurrently with it —
        // winget only allows one instance against its source DB at a time.
        private Task _upgradeScanTask;

        // ── Controls ──────────────────────────────────────────────────────────
        private Panel           _sidebar;
        private Panel           _mainArea;
        private Panel           _topBar;
        private FlowLayoutPanel _appGrid;
        private Panel           _bottomBar;
        private Label           _statusLabel;
        private Label           _selectionCountLabel;
        private ProgressBar     _overallProgress;
        private Button          _installBtn;
        private Button          _upgradeBtn;
        private Button          _clearBtn;
        private Button          _cancelBtn;
        private Button          _logToggle;
        private TextBox         _searchBox;
        private Label           _wingetBadge;
        private TextBox         _folderBox;
        private Button          _browseBtn;
        private CheckBox        _preferWingetChk;
        private RichTextBox     _logBox;
        private Panel           _logPanel;
        private Label           _scanStatusLabel;
        // Shared across every AppTile instead of one ToolTip component per tile —
        // with 100+ catalog entries that was 100+ native tooltip windows that never
        // got disposed. One shared instance, disposed on form close, is enough.
        private readonly ToolTip _sharedTileTip = new ToolTip
        {
            AutoPopDelay = 8000, InitialDelay = 600, ReshowDelay = 300, ShowAlways = true
        };

        private readonly string[] _categories;
        private AppSettings _settings;

        public MainForm()
        {
            _settings = SettingsManager.Load();
            _dm = new DownloadManager();
            _categories = new[] { "All" }
                .Concat(AppCatalog.All.Select(a => a.Category).Distinct().OrderBy(c => c))
                .ToArray();

            InitializeComponent();
            ValidateCatalog();
            ApplySettings();
            PopulateApps("All");
            UpdateSelectionCount();
            _ = RunStartupAsync();

            this.FormClosing += (s, e) => SaveSettings();
            this.FormClosing += (s, e) => { try { _sharedTileTip.Dispose(); } catch { } };

            // Also save on meaningful state changes so a crash doesn't lose settings.
            this.ResizeEnd          += (s, e) => SaveSettings();
            if (_folderBox        != null) _folderBox.Leave        += (s, e) => SaveSettings();
            if (_preferWingetChk  != null) _preferWingetChk.CheckedChanged += (s, e) => SaveSettings();
        }

        /// <summary>
        /// Checks every catalog entry for the kind of gap that's easy to introduce by hand
        /// (a copy-pasted entry missing a field, a new app with neither a winget ID nor a
        /// direct URL) and logs anything it finds instead of letting it surface later as a
        /// crash or a silently-broken tile. Runs once at startup; cheap enough not to matter.
        /// </summary>
        private void ValidateCatalog()
        {
            var issues  = new List<string>();
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var app in AppCatalog.All)
            {
                string label = string.IsNullOrEmpty(app.Name) ? $"(unnamed, Id='{app.Id}')" : app.Name;

                if (string.IsNullOrEmpty(app.Name))     issues.Add($"{label}: missing Name.");
                if (string.IsNullOrEmpty(app.Description)) issues.Add($"{label}: missing Description.");
                if (string.IsNullOrEmpty(app.Category)) issues.Add($"{label}: missing Category.");

                if (string.IsNullOrEmpty(app.Id))
                    issues.Add($"{label}: missing Id (export/import packs won't survive a rename).");
                else if (!seenIds.Add(app.Id))
                    issues.Add($"{label}: duplicate Id '{app.Id}'.");

                if (!app.HasInstallMethod)
                    issues.Add($"{label}: no install method — needs WingetId, DirectUrl+FileName, or IsBundledWith.");
            }

            if (issues.Count > 0)
            {
                Log($"[CATALOG] {issues.Count} issue(s) found at startup:");
                foreach (var issue in issues) Log($"  - {issue}");
            }

            System.Diagnostics.Debug.Assert(issues.Count == 0,
                $"AppCatalog has {issues.Count} integrity issue(s) — see the log panel for details.");
        }

        private async Task RunStartupAsync()
        {
            await RefreshWingetSourcesAsync();
            await ScanInstalledAsync();
            _upgradeScanTask = ScanUpgradesAsync();
            _initialized = true;
            UpdateSelectionCount();   // now safe to push the real badge
        }

        // ─────────────────────────────────────────────────────────────────────
        //  SETTINGS
        // ─────────────────────────────────────────────────────────────────────
        private void ApplySettings()
        {
            if (_settings.WindowState == "Maximized")
                this.WindowState = FormWindowState.Maximized;
            else if (_settings.WindowWidth > 0 && _settings.WindowHeight > 0)
                this.Size = new Size(_settings.WindowWidth, _settings.WindowHeight);

            string folder = _settings.DownloadFolder;
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                folder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
            if (_folderBox != null) _folderBox.Text = folder;

            if (_preferWingetChk != null)
                _preferWingetChk.Checked = _settings.PreferWinget && _dm.WingetAvailable;
        }

        private void SaveSettings()
        {
            _settings.DownloadFolder = _folderBox?.Text.Trim() ?? "";
            _settings.PreferWinget   = _preferWingetChk?.Checked ?? true;
            _settings.WindowState    = this.WindowState == FormWindowState.Maximized ? "Maximized" : "Normal";
            if (this.WindowState == FormWindowState.Normal)
            {
                _settings.WindowWidth  = this.Width;
                _settings.WindowHeight = this.Height;
            }
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
                { _scanStatusLabel.Text = "🔄 Refreshing winget sources..."; _scanStatusLabel.ForeColor = TEXT_SEC; }
            }));
            await _dm.RefreshSourcesAsync();
            this.Invoke((Action)(() =>
            {
                if (_scanStatusLabel != null)
                { _scanStatusLabel.Text = "🔍 Scanning installed apps..."; _scanStatusLabel.ForeColor = TEXT_SEC; }
            }));
        }

        // ─────────────────────────────────────────────────────────────────────
        //  INSTALLED / UPGRADE SCAN
        // ─────────────────────────────────────────────────────────────────────
        private async Task ScanInstalledAsync()
        {
            if (!_dm.WingetAvailable) return;
            var installedIds = await _dm.GetAllInstalledIdsAsync();
            foreach (var app in AppCatalog.All)
                _installedCache[app] = !string.IsNullOrEmpty(app.WingetId) && installedIds.Contains(app.WingetId);

            this.Invoke((Action)(() =>
            {
                foreach (var kv in _tiles)
                    if (_installedCache.TryGetValue(kv.Key, out bool inst)) kv.Value.SetInstalled(inst);

                if (_scanStatusLabel != null)
                {
                    int ic = _installedCache.Values.Count(v => v);
                    _scanStatusLabel.Text      = $"✔ {ic}/{AppCatalog.All.Count} apps installed";
                    _scanStatusLabel.ForeColor = SUCCESS;
                }
                UpdateSelectionCount();
            }));
        }

        private async Task ScanUpgradesAsync()
        {
            if (!_dm.WingetAvailable) return;
            var updatableIds = await _dm.GetAvailableUpdatesAsync();
            foreach (var app in AppCatalog.All)
                _upgradeCache[app] = !string.IsNullOrEmpty(app.WingetId) && updatableIds.Contains(app.WingetId);

            this.Invoke((Action)(() =>
            {
                int count = _upgradeCache.Values.Count(v => v);
                if (_upgradeBtn != null)
                {
                    _upgradeBtn.Visible = count > 0;
                    _upgradeBtn.Text    = $"⬆  Update {count} App{(count == 1 ? "" : "s")}";
                }
                foreach (var kv in _tiles)
                    if (_upgradeCache.TryGetValue(kv.Key, out bool u)) kv.Value.SetHasUpdate(u);
            }));
        }

        /// <summary>
        /// If the startup upgrade scan is still running, wait for it before issuing any
        /// other winget command. Winget only allows one instance against its source DB at
        /// a time, so an install or upgrade fired while the scan is mid-flight can fail
        /// with a spurious "another WinGet process is running" error.
        /// </summary>
        private async Task WaitForBackgroundScanAsync()
        {
            if (_upgradeScanTask != null && !_upgradeScanTask.IsCompleted)
            {
                if (_scanStatusLabel != null)
                {
                    _scanStatusLabel.Text      = "⏳ Waiting for background scan to finish...";
                    _scanStatusLabel.ForeColor = TEXT_SEC;
                }
                try { await _upgradeScanTask; } catch { /* scan already handles/logs its own failures */ }
            }
        }

        private async void OnUpgradeClicked(object sender, EventArgs e)
        {
            if (_isInstalling) return;
            var toUpgrade = _upgradeCache.Where(kv => kv.Value).Select(kv => kv.Key).ToList();
            if (toUpgrade.Count == 0) return;

            await WaitForBackgroundScanAsync();

            _cts = new System.Threading.CancellationTokenSource();
            var token = _cts.Token;
            _isInstalling = true; _upgradeBtn.Enabled = false; _upgradeBtn.Text = "⏳ Updating...";
            _installBtn.Enabled = false;
            _cancelBtn.Visible = true;
            _overallProgress.Maximum = toUpgrade.Count; _overallProgress.Value = 0;
            int done = 0;
            Log($"[UPGRADE] Upgrading {toUpgrade.Count} app(s) — {DateTime.Now:HH:mm:ss}");

            var results = new List<InstallResult>();
            foreach (var app in toUpgrade)
            {
                if (token.IsCancellationRequested) break;
                var result = await _dm.UpgradeAsync(app,
                    msg => this.Invoke((Action)(() =>
                    { _statusLabel.Text = $"{app.Name}: {msg}"; Log($"[{app.Name}] {msg}"); _tiles[app].AppendLog(msg); })),
                    token);
                results.Add(result);
                done++;
                this.Invoke((Action)(() =>
                {
                    _overallProgress.Value = done;
                    if (_tiles.TryGetValue(app, out var tile)) tile.SetStatus(result.Status);
                    if (result.Status == InstallStatus.Success) _upgradeCache[app] = false;
                }));
            }

            _isInstalling = false; _installBtn.Enabled = true;
            _cancelBtn.Visible = false;
            _cts.Dispose(); _cts = null;
            int ok = results.Count(r => r.Status == InstallStatus.Success);
            int fail = results.Count(r => r.Status == InstallStatus.Failed);
            _statusLabel.Text = $"Updates done — {ok} succeeded, {fail} failed.";
            Log($"[UPGRADE DONE] {ok}/{toUpgrade.Count} — {DateTime.Now:HH:mm:ss}");

            int remaining = _upgradeCache.Values.Count(v => v);
            _upgradeBtn.Visible = remaining > 0;
            _upgradeBtn.Text    = remaining > 0 ? $"⬆  Update {remaining} App{(remaining == 1 ? "" : "s")}" : "";
            _upgradeBtn.Enabled = remaining > 0;

            if (!token.IsCancellationRequested)
            {
                using var summary = new SummaryForm(results);
                summary.ShowDialog(this);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  EXPORT / IMPORT SELECTIONS
        // ─────────────────────────────────────────────────────────────────────
        private void ExportSelections()
        {
            var selected = _tiles.Where(kv => kv.Value.IsChecked).Select(kv => kv.Key).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("No apps selected to export.", "Nothing to export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new SaveFileDialog
            {
                Title      = "Export app selection",
                Filter     = "CornDownloader pack (*.corn)|*.corn|JSON (*.json)|*.json",
                DefaultExt = "corn",
                FileName   = $"corn-pack-{DateTime.Now:yyyy-MM-dd}"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            var pack = new SelectionPack
            {
                CreatedAt = DateTime.Now.ToString("o"),
                Apps = selected.Select(a => new PackedApp
                {
                    Id            = a.Id,
                    Name          = a.Name,
                    PinnedVersion = a.PinnedVersion
                }).ToList()
            };

            try
            {
                File.WriteAllText(dlg.FileName,
                    JsonSerializer.Serialize(pack, new JsonSerializerOptions { WriteIndented = true }));
                _statusLabel.Text = $"✔ Exported {selected.Count} apps.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImportSelections()
        {
            using var dlg = new OpenFileDialog
            {
                Title  = "Import app selection",
                Filter = "CornDownloader pack (*.corn)|*.corn|JSON (*.json)|*.json|All files (*.*)|*.*"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                string json = File.ReadAllText(dlg.FileName);
                var pack = JsonSerializer.Deserialize<SelectionPack>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (pack?.Apps == null || pack.Apps.Count == 0)
                {
                    MessageBox.Show("The file contains no app selections.", "Empty pack",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Deselect everything first
                foreach (var tile in _tiles.Values) tile.IsChecked = false;

                int matched = 0;
                foreach (var packed in pack.Apps)
                {
                    // Prefer the stable Id (survives a Name rename in a later catalog update).
                    // Fall back to Name for packs exported before Id existed.
                    var entry = !string.IsNullOrEmpty(packed.Id)
                        ? AppCatalog.All.FirstOrDefault(a => string.Equals(a.Id, packed.Id, StringComparison.OrdinalIgnoreCase))
                        : null;
                    entry ??= AppCatalog.All.FirstOrDefault(a =>
                        string.Equals(a.Name, packed.Name, StringComparison.OrdinalIgnoreCase));
                    if (entry == null) continue;

                    if (_tiles.TryGetValue(entry, out var tile))
                    {
                        tile.IsChecked = true;
                        // Write PinnedVersion onto the catalog entry (it's a runtime-only field —
                        // AppCatalog.All is now a static readonly list so the instances persist for
                        // the session, but PinnedVersion resets to null on next launch). This is
                        // intentional: the pinned version is session-scoped, not catalog-baked.
                        if (!string.IsNullOrEmpty(packed.PinnedVersion))
                        {
                            entry.PinnedVersion = packed.PinnedVersion;
                            tile.SetPinnedVersion(packed.PinnedVersion);
                        }
                        matched++;
                    }
                }

                UpdateSelectionCount();
                _statusLabel.Text = $"✔ Imported {matched}/{pack.Apps.Count} apps from pack.";

                if (matched < pack.Apps.Count)
                {
                    int missing = pack.Apps.Count - matched;
                    MessageBox.Show(
                        $"{missing} app{(missing == 1 ? "" : "s")} in the pack weren't found in the catalog " +
                        $"(may have been removed or renamed).",
                        "Partial import", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Import failed:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  UI CONSTRUCTION
        // ─────────────────────────────────────────────────────────────────────
        private void InitializeComponent()
        {
            Dpi.Update(this);
            AutoScaleMode = AutoScaleMode.None;

            // Window icon — corn on dark circle
            try { this.Icon = AppIconBuilder.Build(32); } catch { }

            int appCount = AppCatalog.All.Count;
            int catCount = AppCatalog.All.Select(a => a.Category).Distinct().Count();
            this.Text = $"Corn Downloader — {appCount} apps • {catCount} categories";
            this.Size            = new Size(Dpi.S(1180), Dpi.S(760));
            this.MinimumSize     = new Size(Dpi.S(900),  Dpi.S(600));
            this.BackColor       = BG;
            this.ForeColor       = TEXT_PRI;
            this.Font            = new Font("Courier New", 8.5f, FontStyle.Regular);
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;

            BuildTopBar();
            BuildSidebar();
            BuildMainArea();
            BuildBottomBar();

            this.Controls.AddRange(new Control[] { _topBar, _sidebar, _mainArea, _bottomBar });
            this.Resize += (s, e) => LayoutPanels();
            LayoutPanels();

            DpiChanged += (s, e) =>
            {
                Dpi.Update(e.DeviceDpiNew);
                if (e.SuggestedRectangle != Rectangle.Empty)
                    SetBounds(e.SuggestedRectangle.X, e.SuggestedRectangle.Y,
                              e.SuggestedRectangle.Width, e.SuggestedRectangle.Height);
                this.MinimumSize = new Size(Dpi.S(900), Dpi.S(600));
                RescalePanels();
            };
        }

        private void RescalePanels() => LayoutPanels();

        private void LayoutPanels()
        {
            int w        = ClientSize.Width;
            int h        = ClientSize.Height;
            int topH     = Dpi.S(60);
            int botH     = Dpi.S(120);
            int sideW    = Dpi.S(210);
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
                        if (c is Button) c.Location = new Point(_logPanel.Width - Dpi.S(80), Dpi.S(4));
            }

            _bottomBar.SetBounds(0, h - botH, w, botH);

            if (_clearBtn != null && _installBtn != null && _logToggle != null)
            {
                int bw = _bottomBar.Width;
                _installBtn.Location = new Point(bw - Dpi.S(16) - _installBtn.Width, Dpi.S(42));
                _clearBtn.Location   = new Point(_installBtn.Left - Dpi.S(8) - _clearBtn.Width, Dpi.S(42));
                if (_cancelBtn != null)
                    _cancelBtn.Location = new Point(_clearBtn.Left - Dpi.S(8) - _cancelBtn.Width, Dpi.S(42));
                _logToggle.Location  = new Point(bw - Dpi.S(16) - _logToggle.Width, Dpi.S(11));
            }
        }

        // ── TOP BAR ──────────────────────────────────────────────────────────
        private void BuildTopBar()
        {
            _topBar = new Panel { BackColor = SURFACE, Dock = DockStyle.None };

            var accentBar = new Panel
            {
                BackColor = ACCENT,
                Size      = new Size(Dpi.S(3), Dpi.S(34)),
                Location  = new Point(Dpi.S(14), Dpi.S(13))
            };

            var titleLbl = new Label
            {
                Text      = "🌽  CORN_DOWNLOADER",
                Font      = new Font("Courier New", 9.5f, FontStyle.Bold),
                ForeColor = ACCENT,
                AutoSize  = true,
                Location  = new Point(Dpi.S(24), Dpi.S(19))
            };

            _searchBox = new TextBox
            {
                PlaceholderText = "  search apps...",
                BackColor       = CARD,
                ForeColor       = TEXT_PRI,
                BorderStyle     = BorderStyle.FixedSingle,
                Font            = new Font("Courier New", 9f),
                Size            = new Size(Dpi.S(240), Dpi.S(28)),
                Location        = new Point(Dpi.S(255), Dpi.S(16))
            };
            _searchBox.TextChanged += (s, e) => FilterApps(_searchBox.Text);

            _wingetBadge = new Label
            {
                AutoSize = true,
                Font     = new Font("Courier New", 7.5f),
                Location = new Point(Dpi.S(510), Dpi.S(21))
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
                Location  = new Point(Dpi.S(700), Dpi.S(14)),
                Height    = Dpi.S(30),
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
            { _wingetBadge.Text = "✦  winget detected";       _wingetBadge.ForeColor = SUCCESS; }
            else
            { _wingetBadge.Text = "⚠  winget not found — direct URLs only"; _wingetBadge.ForeColor = METEOR; }
        }

        // ── SIDEBAR ──────────────────────────────────────────────────────────
        private void BuildSidebar()
        {
            _sidebar = new Panel { BackColor = SURFACE };

            var sideHeader = new Label
            {
                Text      = "// CATEGORIES",
                Font      = new Font("Courier New", 6.5f, FontStyle.Bold),
                ForeColor = MUTED,
                AutoSize  = true,
                Location  = new Point(Dpi.S(12), Dpi.S(12)),
                BackColor = Color.Transparent
            };
            _sidebar.Controls.Add(sideHeader);

            int y = Dpi.S(34);
            foreach (var cat in _categories)
            {
                var btn = CreateSidebarBtn(cat);
                btn.Location = new Point(Dpi.S(8), y);
                btn.Width    = Dpi.S(194);
                _sidebar.Controls.Add(btn);
                y += Dpi.S(38);
            }

            var divider = new Panel
            {
                BackColor = BORDER,
                Size      = new Size(Dpi.S(178), 1),
                Location  = new Point(Dpi.S(12), y + Dpi.S(6))
            };
            _sidebar.Controls.Add(divider);
            y += Dpi.S(14);

            var selAll = CreateSmallBtn("✦ ALL", ACCENT);
            selAll.Location  = new Point(Dpi.S(8), y + Dpi.S(6));
            selAll.Width     = Dpi.S(92);
            selAll.ForeColor = Color.FromArgb(8, 8, 18);
            selAll.Click    += (s, e) => SetAllInView(true);

            var deselAll = CreateSmallBtn("✗ NONE", SURFACE2);
            deselAll.Location  = new Point(Dpi.S(106), y + Dpi.S(6));
            deselAll.Width     = Dpi.S(96);
            deselAll.ForeColor = TEXT_SEC;
            deselAll.Click    += (s, e) => SetAllInView(false);

            var recBtn = new Button
            {
                Text      = "★  RECOMMENDED",
                Size      = new Size(Dpi.S(194), Dpi.S(32)),
                Location  = new Point(Dpi.S(8), y + Dpi.S(42)),
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

            // ── Export / Import buttons ──────────────────────────────────────
            var exportBtn = new Button
            {
                Text      = "⬆ EXPORT",
                Size      = new Size(Dpi.S(92), Dpi.S(26)),
                Location  = new Point(Dpi.S(8), y + Dpi.S(82)),
                BackColor = SURFACE2,
                ForeColor = ACCENT,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Courier New", 6.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            exportBtn.FlatAppearance.BorderColor = BORDER2;
            exportBtn.FlatAppearance.BorderSize  = 1;
            exportBtn.Click += (s, e) => ExportSelections();

            var importBtn = new Button
            {
                Text      = "⬇ IMPORT",
                Size      = new Size(Dpi.S(96), Dpi.S(26)),
                Location  = new Point(Dpi.S(106), y + Dpi.S(82)),
                BackColor = SURFACE2,
                ForeColor = TEXT_SEC,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Courier New", 6.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            importBtn.FlatAppearance.BorderColor = BORDER2;
            importBtn.FlatAppearance.BorderSize  = 1;
            importBtn.Click += (s, e) => ImportSelections();

            _scanStatusLabel = new Label
            {
                Text      = _dm.WingetAvailable ? "🔍 scanning..." : "",
                ForeColor = MUTED,
                Font      = new Font("Courier New", 6.5f),
                AutoSize  = false,
                Size      = new Size(Dpi.S(194), Dpi.S(24)),
                Location  = new Point(Dpi.S(8), y + Dpi.S(116)),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _sidebar.Controls.AddRange(new Control[] { selAll, deselAll, recBtn, exportBtn, importBtn, _scanStatusLabel });
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
                Height        = Dpi.S(32),
                Padding       = new Padding(0, 0, Dpi.S(36), 0),
                AutoEllipsis  = true,
                AutoSize      = false,
                Cursor        = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize         = 0;
            btn.FlatAppearance.BorderColor        = active ? ACCENT : Color.FromArgb(1, 8, 8, 18);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 245, 200, 66);

            var badge = new Label
            {
                Text      = total.ToString(),
                AutoSize  = false,
                Size      = new Size(Dpi.S(28), Dpi.S(14)),
                Font      = new Font("Courier New", 6f, FontStyle.Bold),
                ForeColor = MUTED,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.Controls.Add(badge);
            void PositionBadge() => badge.Location = new Point(btn.Width - Dpi.S(32), (btn.Height - Dpi.S(14)) / 2);
            btn.SizeChanged   += (s, e) => PositionBadge();
            btn.HandleCreated += (s, e) => PositionBadge();

            btn.Paint += (s, e) =>
            {
                PositionBadge();
                if (_activeCategory == category)
                {
                    using var b = new SolidBrush(ACCENT);
                    e.Graphics.FillRectangle(b, 0, Dpi.S(4), Dpi.S(2), btn.Height - Dpi.S(8));
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

        private void UpdateSidebarCounts()
        {
            foreach (var kv in _sidebarBtns)
            {
                string cat   = kv.Key;
                var    btn   = kv.Value;
                int    total = cat == "All" ? AppCatalog.All.Count : AppCatalog.All.Count(a => a.Category == cat);
                int    sel   = _tiles.Where(t => (cat == "All" || t.Key.Category == cat) && t.Value.IsChecked).Count();

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
                Height    = Dpi.S(28),
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
            foreach (var kv in _sidebarBtns)
            {
                string cat    = kv.Key;
                var    btn    = kv.Value;
                bool   active = cat == _activeCategory;

                btn.BackColor = active ? Color.FromArgb(40, 245, 200, 66) : Color.Transparent;
                btn.ForeColor = active ? ACCENT : TEXT_SEC;
                btn.Font      = new Font("Courier New", 7f, active ? FontStyle.Bold : FontStyle.Regular);
                btn.Invalidate();
            }
        }

        // ── MAIN APP GRID ────────────────────────────────────────────────────
        private void BuildMainArea()
        {
            _mainArea = new Panel { BackColor = BG };
            _appGrid  = new FlowLayoutPanel
            {
                AutoScroll   = true,
                WrapContents = true,
                BackColor    = BG,
                Padding      = new Padding(Dpi.S(12)),
                Dock         = DockStyle.Fill
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
                apps = apps.Where(a => (a.Name ?? "").ToLowerInvariant().Contains(search) ||
                                       (a.Description ?? "").ToLowerInvariant().Contains(search));

            foreach (var group in apps.ToList().GroupBy(a => a.Category).OrderBy(g => g.Key))
            {
                var header = new SectionHeader(group.Key, CategoryEmoji(group.Key), CARD, TEXT_PRI, MUTED, ACCENT);
                _appGrid.Controls.Add(header);
                _appGrid.SetFlowBreak(header, true);

                foreach (var app in group)
                {
                    if (!_tiles.TryGetValue(app, out var tile))
                    {
                        tile = new AppTile(app, CARD, SURFACE2, ACCENT, TEXT_PRI, TEXT_SEC, BORDER, _dm, _sharedTileTip);
                        tile.IsChecked      = false;
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

        private void FilterApps(string query) => PopulateApps(_activeCategory);

        private void SetAllInView(bool check)
        {
            foreach (var tile in _tiles.Values) tile.IsChecked = check;
            UpdateSelectionCount();
        }

        // ── BOTTOM BAR ───────────────────────────────────────────────────────
        private void BuildBottomBar()
        {
            _bottomBar = new Panel { BackColor = SURFACE };
            _bottomBar.Paint += (s, e) =>
            {
                using var pen = new System.Drawing.Pen(BORDER, 1);
                e.Graphics.DrawLine(pen, 0, 0, _bottomBar.Width, 0);
            };

            var folderLbl = new Label
            {
                Text      = "// SAVE TO",
                ForeColor = MUTED,
                Font      = new Font("Courier New", 7f, FontStyle.Bold),
                AutoSize  = true,
                Location  = new Point(Dpi.S(16), Dpi.S(14))
            };

            _folderBox = new TextBox
            {
                Text        = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads",
                BackColor   = CARD,
                ForeColor   = TEXT_PRI,
                BorderStyle = BorderStyle.FixedSingle,
                Font        = new Font("Courier New", 8.5f),
                Size        = new Size(Dpi.S(320), Dpi.S(24)),
                Location    = new Point(Dpi.S(110), Dpi.S(11))
            };

            _browseBtn = new Button
            {
                Text      = "BROWSE",
                Size      = new Size(Dpi.S(70), Dpi.S(24)),
                Location  = new Point(Dpi.S(438), Dpi.S(11)),
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
                if (dlg.ShowDialog() == DialogResult.OK) _folderBox.Text = dlg.SelectedPath;
            };

            _preferWingetChk = new CheckBox
            {
                Text      = "prefer winget",
                ForeColor = MUTED,
                Font      = new Font("Courier New", 7.5f),
                Checked   = _dm.WingetAvailable,
                Enabled   = _dm.WingetAvailable,
                AutoSize  = true,
                Location  = new Point(Dpi.S(524), Dpi.S(14))
            };

            _overallProgress = new ProgressBar
            {
                Size     = new Size(Dpi.S(460), Dpi.S(6)),
                Location = new Point(Dpi.S(16), Dpi.S(50)),
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
                Location  = new Point(Dpi.S(16), Dpi.S(62))
            };

            _selectionCountLabel = new Label
            {
                ForeColor = ACCENT,
                Font      = new Font("Courier New", 8f, FontStyle.Bold),
                AutoSize  = true,
                Location  = new Point(Dpi.S(490), Dpi.S(52))
            };

            _clearBtn = new Button
            {
                Text      = "✗ CLEAR",
                Size      = new Size(Dpi.S(100), Dpi.S(36)),
                Location  = new Point(Dpi.S(800), Dpi.S(42)),
                BackColor = SURFACE2,
                ForeColor = TEXT_SEC,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Courier New", 7.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            _clearBtn.FlatAppearance.BorderColor = BORDER2;
            _clearBtn.FlatAppearance.BorderSize  = 1;
            // Clear ALL selections across all categories, not just the currently visible grid.
            _clearBtn.Click += (s, e) =>
            {
                foreach (var tile in _tiles.Values) tile.IsChecked = false;
                UpdateSelectionCount();
            };

            _installBtn = new Button
            {
                Text      = "⬇  INSTALL",
                Size      = new Size(Dpi.S(160), Dpi.S(36)),
                Location  = new Point(Dpi.S(910), Dpi.S(42)),
                BackColor = ACCENT,
                ForeColor = Color.FromArgb(8, 8, 18),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Courier New", 9f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            _installBtn.FlatAppearance.BorderSize = 0;
            _installBtn.Click += OnInstallClicked;

            _cancelBtn = new Button
            {
                Text      = "✗ CANCEL",
                Size      = new Size(Dpi.S(110), Dpi.S(36)),
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Courier New", 7.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Visible   = false
            };
            _cancelBtn.FlatAppearance.BorderSize = 0;
            _cancelBtn.Click += (s, e) => _cts?.Cancel();

            _logToggle = new Button
            {
                Text      = "// LOG",
                Size      = new Size(Dpi.S(65), Dpi.S(24)),
                Location  = new Point(Dpi.S(914), Dpi.S(11)),
                BackColor = Color.Transparent,
                ForeColor = MUTED,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Courier New", 7f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            _logToggle.FlatAppearance.BorderColor = BORDER;
            _logToggle.FlatAppearance.BorderSize  = 1;
            _logToggle.Click += (s, e) => ToggleLog();

            _bottomBar.Controls.AddRange(new Control[] {
                folderLbl, _folderBox, _browseBtn, _preferWingetChk,
                _overallProgress, _statusLabel, _selectionCountLabel,
                _clearBtn, _cancelBtn, _installBtn, _logToggle
            });

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
                Height    = Dpi.S(160)
            };

            var logClose = new Button
            {
                Text      = "✗ CLOSE",
                Size      = new Size(Dpi.S(75), Dpi.S(22)),
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
            this.Controls.Add(_logPanel);
        }

        private void ToggleLog()
        {
            _logPanel.Visible = !_logPanel.Visible;
            LayoutPanels();
            if (_logPanel.Visible)
            {
                foreach (Control c in _logPanel.Controls)
                    if (c is Button) c.Location = new Point(_logPanel.Width - Dpi.S(80), Dpi.S(4));
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
            await WaitForBackgroundScanAsync();

            _cts = new System.Threading.CancellationTokenSource();
            var token = _cts.Token;
            _isInstalling            = true;
            _installBtn.Enabled      = false;
            _installBtn.Text         = "⏳ Installing...";
            _cancelBtn.Visible       = true;
            _overallProgress.Value   = 0;
            _overallProgress.Maximum = selected.Count;

            Log($"[START] Installing {selected.Count} app(s) — {DateTime.Now:HH:mm:ss}");

            var results = await _dm.InstallAllAsync(
                selected, folder, preferWinget,
                (app, status, msg) =>
                {
                    this.Invoke((Action)(() =>
                    {
                        if (_tiles.TryGetValue(app, out var tile))
                        {
                            tile.SetStatus(status);
                            tile.AppendLog(msg);
                            int pct = ParsePercent(msg);
                            if (pct >= 0 && (status == InstallStatus.Installing || status == InstallStatus.Downloading))
                                tile.SetProgress(pct);
                        }
                        _statusLabel.Text = $"{app.Name}: {msg}";
                        Log($"[{app.Name}] {msg}");
                    }));
                },
                (done, total) => this.Invoke((Action)(() => _overallProgress.Value = done)),
                token
            );

            int ok      = results.Count(r => r.Status == InstallStatus.Success);
            int fail    = results.Count(r => r.Status == InstallStatus.Failed);
            int skipped = results.Count(r => r.Status == InstallStatus.Skipped);

            string doneMsg = $"Done — {ok} succeeded, {fail} failed";
            if (skipped > 0) doneMsg += $", {skipped} skipped";
            _statusLabel.Text   = doneMsg + ".";
            _installBtn.Text    = "⬇  INSTALL";
            _installBtn.Enabled = true;
            _cancelBtn.Visible  = false;
            _isInstalling       = false;
            _cts.Dispose(); _cts = null;

            Log($"[DONE] {ok}/{selected.Count} succeeded — {DateTime.Now:HH:mm:ss}");

            if (token.IsCancellationRequested) return;

            var pendingResults = results;
            while (true)
            {
                using var summary = new SummaryForm(pendingResults);
                var dr = summary.ShowDialog(this);
                if (dr != DialogResult.Retry || summary.FailedResults.Count == 0) break;

                var retryApps = summary.FailedResults.Select(r => r.App).ToList();
                Log($"[RETRY] Retrying {retryApps.Count} failed app(s)...");

                _cts = new System.Threading.CancellationTokenSource();
                _isInstalling = true; _installBtn.Enabled = false; _installBtn.Text = "⏳ Retrying...";
                _cancelBtn.Visible = true;
                _overallProgress.Maximum = retryApps.Count; _overallProgress.Value = 0;

                pendingResults = await _dm.InstallAllAsync(
                    retryApps, folder, preferWinget,
                    (app, status, msg) => this.Invoke((Action)(() =>
                    {
                        if (_tiles.TryGetValue(app, out var tile)) { tile.SetStatus(status); tile.AppendLog(msg); }
                        _statusLabel.Text = $"{app.Name}: {msg}";
                        Log($"[{app.Name}] {msg}");
                    })),
                    (done2, total2) => this.Invoke((Action)(() => _overallProgress.Value = done2)),
                    _cts.Token);

                _isInstalling = false; _installBtn.Enabled = true; _installBtn.Text = "⬇  INSTALL";
                _cancelBtn.Visible = false;
                _cts.Dispose(); _cts = null;
            }
        }

        private void UpdateSelectionCount()
        {
            int count = _tiles.Values.Count(t => t.IsChecked);
            if (_selectionCountLabel != null)
            {
                _selectionCountLabel.Text = count == 0
                    ? "no apps selected"
                    : $"{count} app{(count == 1 ? "" : "s")} selected";
            }
            UpdateSidebarCounts();

            // Update taskbar badge — only after startup so pre-checked defaults don't show
            if (_initialized)
                try { TaskbarBadge.SetCount(this.Handle, count); } catch { }
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
    //  APP TILE CONTROL  (with version picker + collapsible per-app log)
    // ─────────────────────────────────────────────────────────────────────────
    public class AppTile : Panel
    {
        // ── Colours (mirrored locally for sub-control painters) ───────────────
        private static readonly Color _accent  = Color.FromArgb(245, 200,  66);
        private static readonly Color _muted   = Color.FromArgb(101,  97, 160);
        private static readonly Color _border2 = Color.FromArgb( 61,  58, 112);
        private static readonly Color _bg      = Color.FromArgb(  8,   8,  18);
        private static readonly Color _surface = Color.FromArgb( 19,  18,  42);
        private static readonly Color _meteor  = Color.FromArgb(244,  81,  30);

        private bool   _checked;
        private bool   _isInstalled    = false;
        private bool   _hasUpdate      = false;
        private bool   _logExpanded    = false;
        private bool   _forceReinstall = false;

        private readonly Color _normalBg;
        private readonly Color _checkedBg;
        private readonly AppEntry   _app;
        private readonly DownloadManager _dm;
        private readonly ToolTip _sharedTip;

        // Sub-controls
        private readonly Label       _statusDot;
        private readonly ProgressBar _progressBar;
        private          Label       _updateBadge;
        private          ComboBox    _versionPicker;
        private          Button      _versionToggle;
        private          RichTextBox _tileLog;
        private          Panel       _logDrawer;
        private          Button      _logToggleBtn;

        private const int BASE_HEIGHT     = 125;   // collapsed tile height (logical px)
        private const int LOG_HEIGHT      = 90;    // log drawer height (logical px)
        private const int VERSION_OFFSET  = 22;    // extra height when version picker visible

        public event EventHandler CheckedChanged;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool IsChecked
        {
            get => _checked;
            set
            {
                if (value && !string.IsNullOrEmpty(_app.IsBundledWith)) return;
                if (value && !_app.HasInstallMethod) return;
                if (value && _isInstalled && !_forceReinstall) return;
                _checked  = value;
                if (!value) { _forceReinstall = false; _app.ForceReinstall = false; }
                BackColor = value
                    ? (_forceReinstall ? Color.FromArgb(20, 244, 81, 30) : _checkedBg)
                    : _normalBg;
                Invalidate();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public AppTile(AppEntry app, Color normalBg, Color checkedBg, Color accent,
                       Color textPri, Color textSec, Color border, DownloadManager dm, ToolTip sharedTip)
        {
            _app       = app;
            _dm        = dm;
            _normalBg  = normalBg;
            _checkedBg = checkedBg;
            _sharedTip = sharedTip;

            Size      = new Size(Dpi.S(230), Dpi.S(BASE_HEIGHT));
            BackColor = normalBg;
            Margin    = new Padding(Dpi.S(6));
            Cursor    = _app.HasInstallMethod ? Cursors.Hand : Cursors.No;

            // ── Border + checkmark paint ──────────────────────────────────────
            this.Paint += (s, e) =>
            {
                var g = e.Graphics;
                Color borderCol = _isInstalled && !_forceReinstall ? Color.FromArgb(42, 40, 80)
                    : _checked ? (_forceReinstall ? _meteor : accent) : border;
                float borderW = (_isInstalled && !_forceReinstall) ? 1f : 1.5f;
                using (var pen = new System.Drawing.Pen(borderCol, borderW))
                    g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);

                if (_isInstalled && !_forceReinstall)
                {
                    using var b = new SolidBrush(Color.FromArgb(60, 76, 175, 80));
                    g.FillRectangle(b, 1, 1, Width - 2, Dpi.S(3));
                }
                else if (_checked)
                {
                    Color checkFill = _forceReinstall ? _meteor : accent;
                    using var brush = new SolidBrush(checkFill);
                    g.FillRectangle(brush, Width - Dpi.S(22), Dpi.S(6), Dpi.S(16), Dpi.S(16));
                    using var whitePen = new System.Drawing.Pen(Color.White, 2f);
                    g.DrawLines(whitePen, new[]
                    {
                        new Point(Width - Dpi.S(19), Dpi.S(14)),
                        new Point(Width - Dpi.S(15), Dpi.S(18)),
                        new Point(Width - Dpi.S(9),  Dpi.S(9))
                    });
                }
            };

            // ── Icon ─────────────────────────────────────────────────────────
            var iconLbl = new Label
            {
                Text      = app.IconChar,
                Font      = new Font("Segoe UI Emoji", 15f),
                AutoSize  = false,
                Size      = new Size(Dpi.S(34), Dpi.S(34)),
                Location  = new Point(Dpi.S(8), Dpi.S(8)),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // ── Name ─────────────────────────────────────────────────────────
            var nameLbl = new Label
            {
                Text         = app.Name,
                Font         = new Font("Courier New", 9f, FontStyle.Bold),
                ForeColor    = textPri,
                AutoSize     = false,
                Size         = new Size(Dpi.S(152), Dpi.S(34)),
                Location     = new Point(Dpi.S(48), Dpi.S(6)),
                BackColor    = Color.Transparent,
                AutoEllipsis = true,
                UseMnemonic  = false
            };

            // ── Description ──────────────────────────────────────────────────
            var descLbl = new Label
            {
                Text      = app.Description,
                Font      = new Font("Segoe UI", 7f),
                ForeColor = textSec,
                AutoSize  = false,
                Size      = new Size(Dpi.S(210), Dpi.S(26)),
                Location  = new Point(Dpi.S(10), Dpi.S(72)),
                BackColor = Color.Transparent
            };

            // ── Method badge ─────────────────────────────────────────────────
            string method;
            Color  badgeFg;
            if (!string.IsNullOrEmpty(app.IsBundledWith))
            {
                method  = "bundled";
                badgeFg = Color.FromArgb(101, 97, 160);   // muted — not installable standalone
            }
            else if (app.WingetId != null)
            {
                method  = "winget";
                badgeFg = Color.FromArgb(245, 200, 66);
            }
            else if (!string.IsNullOrEmpty(app.DirectUrl) && !string.IsNullOrEmpty(app.FileName))
            {
                method  = "direct";
                badgeFg = Color.FromArgb(160, 157, 192);
            }
            else
            {
                // Neither winget nor a direct URL is configured — this entry can't actually
                // be installed yet. Say so instead of silently claiming "direct".
                method  = "⚠ no install";
                badgeFg = Color.FromArgb(244, 81, 30);
            }

            var methodBadge = new Label
            {
                Text      = method,
                Font      = new Font("Courier New", 6f),
                ForeColor = badgeFg,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(Dpi.S(10), Dpi.S(50)),
                Padding   = new Padding(Dpi.S(2), Dpi.S(1), Dpi.S(2), Dpi.S(1))
            };
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
                Location  = new Point(methodBadge.PreferredWidth + Dpi.S(16), Dpi.S(52))
            };

            // ── Status dot ───────────────────────────────────────────────────
            _statusDot = new Label
            {
                Text      = "",
                AutoSize  = true,
                Location  = new Point(Dpi.S(10), Dpi.S(104)),
                Font      = new Font("Courier New", 7f),
                BackColor = Color.Transparent
            };

            // ── Progress bar ─────────────────────────────────────────────────
            _progressBar = new ProgressBar
            {
                Size     = new Size(this.Width - Dpi.S(20), Dpi.S(4)),
                Location = new Point(Dpi.S(10), Dpi.S(118)),
                Style    = ProgressBarStyle.Continuous,
                Minimum  = 0, Maximum = 100, Value = 0,
                Visible  = false
            };

            // ── Update badge ─────────────────────────────────────────────────
            _updateBadge = new Label
            {
                Text      = "⬆ update available",
                AutoSize  = true,
                Location  = new Point(Dpi.S(10), Dpi.S(104)),
                Font      = new Font("Courier New", 6.5f),
                ForeColor = Color.FromArgb(244, 81, 30),
                BackColor = Color.Transparent,
                Visible   = false
            };

            // ── Version picker (winget only) ──────────────────────────────────
            if (app.WingetId != null)
            {
                _versionToggle = new Button
                {
                    Text      = "ver ▾",
                    Size      = new Size(Dpi.S(44), Dpi.S(16)),
                    Location  = new Point(Width - Dpi.S(50), Dpi.S(50)),
                    BackColor = Color.Transparent,
                    ForeColor = _muted,
                    FlatStyle = FlatStyle.Flat,
                    Font      = new Font("Courier New", 5.5f),
                    Cursor    = Cursors.Hand,
                    Anchor    = AnchorStyles.Top | AnchorStyles.Right
                };
                _versionToggle.FlatAppearance.BorderColor = _border2;
                _versionToggle.FlatAppearance.BorderSize  = 1;
                _versionToggle.Click += OnVersionToggleClicked;

                _versionPicker = new ComboBox
                {
                    DropDownStyle  = ComboBoxStyle.DropDownList,
                    BackColor      = _bg,
                    ForeColor      = _accent,
                    Font           = new Font("Courier New", 6.5f),
                    Size           = new Size(Dpi.S(210), Dpi.S(20)),
                    Location       = new Point(Dpi.S(10), Dpi.S(BASE_HEIGHT - 4)),
                    Visible        = false,
                    Anchor         = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                _versionPicker.Items.Add("latest (default)");
                if (!string.IsNullOrEmpty(app.PinnedVersion))
                    _versionPicker.Items.Add(app.PinnedVersion);
                _versionPicker.SelectedIndex = 0;

                _versionPicker.SelectedIndexChanged += (s, e) =>
                {
                    int idx = _versionPicker.SelectedIndex;
                    _app.PinnedVersion = (idx == 0 || _versionPicker.Items[idx].ToString() == "latest (default)")
                        ? null
                        : _versionPicker.Items[idx].ToString();
                    _versionToggle.ForeColor = _app.PinnedVersion != null ? _accent : _muted;
                    _versionToggle.Text      = _app.PinnedVersion != null
                        ? $"v{_app.PinnedVersion.Split('.')[0]}▾"
                        : "ver ▾";
                };
            }

            // ── Per-app log drawer ────────────────────────────────────────────
            _logToggleBtn = new Button
            {
                Text      = "log ▸",
                Size      = new Size(Dpi.S(38), Dpi.S(16)),
                Location  = new Point(Dpi.S(10), Dpi.S(104)),
                BackColor = Color.Transparent,
                ForeColor = _muted,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Courier New", 5.5f),
                Cursor    = Cursors.Hand,
                Visible   = false    // shown once there's log output
            };
            _logToggleBtn.FlatAppearance.BorderSize = 0;
            _logToggleBtn.Click += OnLogToggleClicked;

            _tileLog = new RichTextBox
            {
                BackColor   = _bg,
                ForeColor   = Color.FromArgb(100, 245, 200, 66),   // dim gold
                BorderStyle = BorderStyle.None,
                ReadOnly    = true,
                Font        = new Font("Courier New", 6f),
                ScrollBars  = RichTextBoxScrollBars.Vertical,
                Visible     = false
            };

            _logDrawer = new Panel
            {
                BackColor = Color.FromArgb(12, 11, 28),
                Visible   = false,
                Location  = new Point(0, Dpi.S(BASE_HEIGHT)),
                Size      = new Size(this.Width, Dpi.S(LOG_HEIGHT))
            };
            // Top border on drawer
            _logDrawer.Paint += (s, e) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(42, 40, 80), 1);
                e.Graphics.DrawLine(pen, 0, 0, _logDrawer.Width, 0);
            };
            _tileLog.Dock = DockStyle.Fill;
            _logDrawer.Controls.Add(_tileLog);

            // ── Assemble ──────────────────────────────────────────────────────
            var ctrls = new List<Control> { iconLbl, nameLbl, descLbl, methodBadge, catBadge,
                                            _statusDot, _progressBar, _updateBadge, _logToggleBtn, _logDrawer };
            if (_versionToggle != null)  ctrls.Add(_versionToggle);
            if (_versionPicker != null)  ctrls.Add(_versionPicker);
            Controls.AddRange(ctrls.ToArray());

            // ── Click-to-toggle ───────────────────────────────────────────────
            void Toggle(object s, EventArgs e)
            {
                if (!string.IsNullOrEmpty(_app.IsBundledWith)) return;
                if (_isInstalled && !_forceReinstall) return;
                IsChecked = !_checked;
            }
            this.Click     += Toggle;
            iconLbl.Click  += Toggle;
            nameLbl.Click  += Toggle;
            descLbl.Click  += Toggle;
            catBadge.Click += Toggle;

            this.MouseEnter += (s, e) => { if (!_checked) BackColor = Color.FromArgb(19, 18, 45); };
            this.MouseLeave += (s, e) => { if (!_checked) BackColor = _normalBg; };

            // ── Right-click context menu ──────────────────────────────────────
            var cms = new ContextMenuStrip();
            cms.Opening += (s, e) =>
            {
                cms.Items.Clear();
                if (!string.IsNullOrEmpty(_app.IsBundledWith))
                {
                    var bundledItem = new ToolStripMenuItem($"Bundled with {_app.IsBundledWith}") { Enabled = false };
                    cms.Items.Add(bundledItem);
                    return;
                }
                if (_isInstalled)
                {
                    if (_forceReinstall && _checked)
                    {
                        var cancelItem = new ToolStripMenuItem("Cancel Reinstall");
                        cancelItem.Click += (cs, ce) => { IsChecked = false; };
                        cms.Items.Add(cancelItem);
                    }
                    else
                    {
                        var forceItem = new ToolStripMenuItem("Force Reinstall");
                        forceItem.Click += (cs, ce) =>
                        {
                            _forceReinstall    = true;
                            _app.ForceReinstall = true;
                            _checked           = false;  // reset so setter logic runs cleanly
                            IsChecked          = true;
                            Cursor             = Cursors.Hand;
                        };
                        cms.Items.Add(forceItem);
                    }
                }
                else
                {
                    var selItem = new ToolStripMenuItem(_checked ? "Deselect" : "Select");
                    selItem.Click += (cs, ce) => IsChecked = !_checked;
                    cms.Items.Add(selItem);
                }
                if (!string.IsNullOrEmpty(_app.WingetId))
                {
                    cms.Items.Add(new ToolStripSeparator());
                    var idItem = new ToolStripMenuItem($"winget id: {_app.WingetId}") { Enabled = false };
                    cms.Items.Add(idItem);
                }
            };
            this.ContextMenuStrip = cms;

            // ── Tooltip (full description + install method) ───────────────────
            string tipMethod = !string.IsNullOrEmpty(_app.WingetId) ? $"winget: {_app.WingetId}"
                             : !string.IsNullOrEmpty(_app.IsBundledWith) ? $"bundled with: {_app.IsBundledWith}"
                             : (!string.IsNullOrEmpty(_app.DirectUrl) && !string.IsNullOrEmpty(_app.FileName))
                                 ? "direct download"
                                 : "⚠ no install method configured yet";
            string tipText = $"{app.Description}\n\n{tipMethod}";
            _sharedTip.SetToolTip(this,        tipText);
            _sharedTip.SetToolTip(iconLbl,     tipText);
            _sharedTip.SetToolTip(nameLbl,     tipText);
            _sharedTip.SetToolTip(descLbl,     tipText);
            _sharedTip.SetToolTip(methodBadge, tipText);
        }

        // ── Version picker toggle ─────────────────────────────────────────────
        private async void OnVersionToggleClicked(object sender, EventArgs e)
        {
            if (_versionPicker == null) return;

            bool show = !_versionPicker.Visible;

            if (show && _versionPicker.Items.Count <= 1)
            {
                // Lazy-load versions from winget on first open
                _versionToggle.Text    = "...";
                _versionToggle.Enabled = false;
                var versions = await _dm.GetAvailableVersionsAsync(_app);
                _versionPicker.Items.Clear();
                _versionPicker.Items.Add("latest (default)");
                foreach (var v in versions) _versionPicker.Items.Add(v);
                // Re-select pinned version if it exists
                if (!string.IsNullOrEmpty(_app.PinnedVersion))
                {
                    int idx = _versionPicker.Items.IndexOf(_app.PinnedVersion);
                    _versionPicker.SelectedIndex = idx >= 0 ? idx : 0;
                }
                else _versionPicker.SelectedIndex = 0;
                _versionToggle.Enabled = true;
                _versionToggle.Text    = _app.PinnedVersion != null ? $"v{_app.PinnedVersion.Split('.')[0]}▾" : "ver ▾";
            }

            _versionPicker.Visible = show;
            int extraH = show ? Dpi.S(VERSION_OFFSET) : 0;
            int logH   = _logExpanded ? Dpi.S(LOG_HEIGHT) : 0;
            Height = Dpi.S(BASE_HEIGHT) + extraH + logH;
            _logDrawer.Location = new Point(0, Dpi.S(BASE_HEIGHT) + extraH);
            _versionPicker.Location = new Point(Dpi.S(10), Dpi.S(BASE_HEIGHT) - Dpi.S(4));
        }

        // ── Per-app log toggle ────────────────────────────────────────────────
        private void OnLogToggleClicked(object sender, EventArgs e)
        {
            _logExpanded = !_logExpanded;
            _logDrawer.Visible    = _logExpanded;
            _logToggleBtn.Text    = _logExpanded ? "log ▾" : "log ▸";

            int verH = (_versionPicker != null && _versionPicker.Visible) ? Dpi.S(VERSION_OFFSET) : 0;
            Height = Dpi.S(BASE_HEIGHT) + verH + (_logExpanded ? Dpi.S(LOG_HEIGHT) : 0);
            _logDrawer.Location = new Point(0, Dpi.S(BASE_HEIGHT) + verH);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Appends a line to this tile's per-app log and makes the toggle visible.</summary>
        public void AppendLog(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg)) return;
            if (_tileLog.InvokeRequired)
            {
                _tileLog.Invoke((Action)(() => AppendLog(msg)));
                return;
            }
            _tileLog.AppendText(msg + "\n");
            _tileLog.ScrollToCaret();
            if (!_logToggleBtn.Visible)
            {
                _logToggleBtn.Visible   = true;
                _statusDot.Visible      = false;   // log button takes the status-dot row
                _updateBadge.Visible    = false;
            }
        }

        public void SetStatus(InstallStatus status)
        {
            switch (status)
            {
                case InstallStatus.Installing:
                case InstallStatus.Downloading:
                    if (!_logToggleBtn.Visible) { _statusDot.Text = "⏳ Installing..."; _statusDot.ForeColor = Color.FromArgb(251, 191, 36); }
                    break;
                case InstallStatus.Success:
                    _logToggleBtn.ForeColor  = Color.FromArgb(34, 197, 94);
                    _logToggleBtn.Text       = _logExpanded ? "log ▾" : "log ▸";
                    _statusDot.Text          = "✔ Done";
                    _statusDot.ForeColor     = Color.FromArgb(34, 197, 94);
                    _statusDot.Visible       = !_logToggleBtn.Visible;
                    _progressBar.Visible     = false;
                    IsChecked = false;
                    break;
                case InstallStatus.Failed:
                    _logToggleBtn.ForeColor  = Color.FromArgb(239, 68, 68);
                    _statusDot.Text          = "✘ Failed";
                    _statusDot.ForeColor     = Color.FromArgb(239, 68, 68);
                    _statusDot.Visible       = !_logToggleBtn.Visible;
                    _progressBar.Visible     = false;
                    break;
            }
        }

        public void SetProgress(int percent)
        {
            if (_progressBar == null) return;
            if (percent < 0)
            { _progressBar.Style = ProgressBarStyle.Marquee; _progressBar.Visible = true; }
            else if (percent >= 100)
            { _progressBar.Visible = false; }
            else
            {
                _progressBar.Style   = ProgressBarStyle.Continuous;
                _progressBar.Value   = Math.Min(percent, 100);
                _progressBar.Visible = true;
                if (!_logToggleBtn.Visible) { _statusDot.Text = $"⏳ {percent}%"; _statusDot.ForeColor = Color.FromArgb(251, 191, 36); }
            }
        }

        public void SetInstalled(bool installed)
        {
            _isInstalled = installed;
            if (installed)
            {
                _forceReinstall    = false;
                _app.ForceReinstall = false;
                _statusDot.Text    = "✔ Installed";
                _statusDot.ForeColor = Color.FromArgb(34, 197, 94);
                _statusDot.Visible = true;
                BackColor          = _normalBg;
                _checked           = false;
                Cursor             = Cursors.Hand;
            }
            Invalidate();
        }

        public void SetHasUpdate(bool hasUpdate)
        {
            _hasUpdate = hasUpdate;
            if (_updateBadge != null)
            {
                _updateBadge.Visible = hasUpdate && !_logToggleBtn.Visible;
                if (!hasUpdate) _statusDot.Visible = !_logToggleBtn.Visible;
            }
            Invalidate();
        }

        /// <summary>Called by import to reflect an externally-pinned version in the picker UI.</summary>
        public void SetPinnedVersion(string version)
        {
            if (_versionPicker == null) return;
            int idx = _versionPicker.Items.IndexOf(version);
            if (idx < 0) { _versionPicker.Items.Add(version); idx = _versionPicker.Items.Count - 1; }
            _versionPicker.SelectedIndex = idx;
            if (_versionToggle != null)
            {
                _versionToggle.Text      = $"v{version.Split('.')[0]}▾";
                _versionToggle.ForeColor = _accent;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  SECTION HEADER CONTROL
    // ─────────────────────────────────────────────────────────────────────────
    public class SectionHeader : Panel
    {
        public SectionHeader(string title, string emoji, Color bg, Color textPri, Color textSec, Color accent)
        {
            Height    = Dpi.S(48);
            Margin    = new Padding(Dpi.S(6), Dpi.S(18), Dpi.S(6), Dpi.S(4));
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
                if (_lastParent != null) _lastParent.ClientSizeChanged -= syncHandler;
                _lastParent = Parent;
                if (_lastParent != null)
                {
                    Width = _lastParent.ClientSize.Width - Margin.Horizontal;
                    _lastParent.ClientSizeChanged += syncHandler;
                }
            };

            var bar = new Panel
            {
                BackColor = accent,
                Size      = new Size(Dpi.S(2), Dpi.S(22)),
                Location  = new Point(Dpi.S(4), Dpi.S(13))
            };

            var lbl = new Label
            {
                Text      = $"{emoji}  {title.ToUpper()}",
                Font      = new Font("Courier New", 8.5f, FontStyle.Bold),
                ForeColor = accent,
                AutoSize  = true,
                Location  = new Point(Dpi.S(12), Dpi.S(14)),
                BackColor = Color.Transparent
            };

            this.Paint += (s, e) =>
            {
                int lineY = Height / 2 + 2;
                using var pen = new System.Drawing.Pen(Color.FromArgb(42, 40, 80), 1);
                e.Graphics.DrawLine(pen, lbl.Right + Dpi.S(14), lineY, Width - Dpi.S(20), lineY);
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

        public List<InstallResult> FailedResults { get; private set; } = new();

        public SummaryForm(List<InstallResult> results)
        {
            int ok   = results.Count(r => r.Status == InstallStatus.Success);
            int fail = results.Count(r => r.Status == InstallStatus.Failed);

            Text            = "// INSTALL SUMMARY";
            Size            = new Size(Dpi.S(560), Dpi.S(540));
            MinimumSize     = new Size(Dpi.S(440), Dpi.S(400));
            BackColor       = BG;
            ForeColor       = TEXT_PRI;
            Font            = new Font("Courier New", 8.5f);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;

            try { this.Icon = AppIconBuilder.Build(32); } catch { }

            var header = new Panel { BackColor = SURFACE, Dock = DockStyle.Top, Height = Dpi.S(70) };
            bool allOk = fail == 0;
            header.Controls.Add(new Label
            {
                Text      = allOk ? "✔  All apps installed!" : $"⚠  {fail} installation{(fail == 1 ? "" : "s")} failed",
                Font      = new Font("Courier New", 11f, FontStyle.Bold),
                ForeColor = allOk ? SUCCESS : DANGER,
                AutoSize  = true,
                Location  = new Point(Dpi.S(18), Dpi.S(14))
            });
            header.Controls.Add(new Label
            {
                Text      = $"{ok} succeeded   •   {fail} failed   •   {results.Count} total",
                Font      = new Font("Courier New", 8.5f),
                ForeColor = TEXT_SEC,
                AutoSize  = true,
                Location  = new Point(Dpi.S(20), Dpi.S(42))
            });

            var scroll = new Panel
            {
                AutoScroll = true, BackColor = BG, Dock = DockStyle.Fill,
                Padding    = new Padding(Dpi.S(14), Dpi.S(10), Dpi.S(14), Dpi.S(10))
            };

            int y = Dpi.S(10);
            if (ok > 0)
            {
                scroll.Controls.Add(MakeSectionLabel("Installed successfully", SUCCESS, y)); y += Dpi.S(28);
                foreach (var r in results.Where(r => r.Status == InstallStatus.Success))
                { scroll.Controls.Add(MakeResultRow(r.App.IconChar, r.App.Name, "✔", SUCCESS, y)); y += Dpi.S(38); }
                y += Dpi.S(8);
            }
            if (fail > 0)
            {
                scroll.Controls.Add(MakeSectionLabel("Failed", DANGER, y)); y += Dpi.S(28);
                foreach (var r in results.Where(r => r.Status == InstallStatus.Failed))
                {
                    FailedResults.Add(r);
                    scroll.Controls.Add(MakeResultRow(r.App.IconChar, r.App.Name,
                        $"✘  {TrimError(r.Message)}", DANGER, y));
                    y += Dpi.S(38);
                }
            }
            scroll.Controls.Add(new Panel { Height = Dpi.S(10), Top = y, BackColor = Color.Transparent });

            var footer = new Panel { BackColor = SURFACE, Dock = DockStyle.Bottom, Height = Dpi.S(58) };
            var closeBtn = new Button
            {
                Text      = "Close",
                Size      = new Size(Dpi.S(100), Dpi.S(34)),
                BackColor = SURFACE2, ForeColor = TEXT_PRI,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                Anchor    = AnchorStyles.Right | AnchorStyles.Top
            };
            closeBtn.FlatAppearance.BorderColor = BORDER;
            closeBtn.Location = new Point(this.ClientSize.Width - Dpi.S(118), Dpi.S(12));
            closeBtn.Click   += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            footer.Controls.Add(closeBtn);

            if (fail > 0)
            {
                var retryBtn = new Button
                {
                    Text      = $"↺  Retry {fail} Failed",
                    Size      = new Size(Dpi.S(140), Dpi.S(34)),
                    BackColor = DANGER, ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font      = new Font("Courier New", 8.5f, FontStyle.Bold),
                    Cursor    = Cursors.Hand
                };
                retryBtn.FlatAppearance.BorderSize = 0;
                retryBtn.Location = new Point(this.ClientSize.Width - Dpi.S(268), Dpi.S(12));
                retryBtn.Click   += (s, e) => { DialogResult = DialogResult.Retry; Close(); };
                footer.Controls.Add(retryBtn);
            }

            Controls.AddRange(new Control[] { scroll, header, footer });
        }

        private Label MakeSectionLabel(string text, Color color, int y) => new Label
        {
            Text = text.ToUpperInvariant(), Font = new Font("Courier New", 7f, FontStyle.Bold),
            ForeColor = color, AutoSize = true, Top = y, Left = Dpi.S(2), BackColor = Color.Transparent
        };

        private Panel MakeResultRow(string icon, string name, string statusText, Color statusColor, int y)
        {
            var row = new Panel
            {
                BackColor = SURFACE,
                Size      = new Size(Dpi.S(490), Dpi.S(32)),
                Top = y, Left = 0
            };
            row.Paint += (s, e) =>
            {
                using var pen = new System.Drawing.Pen(BORDER, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, row.Width - 1, row.Height - 1);
            };
            row.Controls.Add(new Label
            {
                Text = icon, Font = new Font("Segoe UI Emoji", 11f),
                AutoSize = false, Size = new Size(Dpi.S(28), Dpi.S(28)),
                Location = new Point(Dpi.S(4), Dpi.S(2)), BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            });
            row.Controls.Add(new Label
            {
                Text = name, Font = new Font("Courier New", 8.5f),
                ForeColor = TEXT_PRI, AutoSize = true,
                Location = new Point(Dpi.S(36), Dpi.S(8)), BackColor = Color.Transparent
            });
            var statusLbl = new Label
            {
                Text = statusText, Font = new Font("Courier New", 7.5f),
                ForeColor = statusColor, AutoSize = true, BackColor = Color.Transparent
            };
            statusLbl.Location = new Point(row.Width - statusLbl.PreferredWidth - Dpi.S(10), Dpi.S(9));
            statusLbl.Anchor   = AnchorStyles.Right | AnchorStyles.Top;
            row.Controls.Add(statusLbl);
            return row;
        }

        private static string TrimError(string msg) =>
            string.IsNullOrEmpty(msg) ? "Unknown error"
            : msg.Length > 48 ? msg.Substring(0, 45) + "..." : msg;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  SETTINGS PERSISTENCE
    // ─────────────────────────────────────────────────────────────────────────
    internal class AppSettings
    {
        public string DownloadFolder { get; set; } = "";
        public bool   PreferWinget   { get; set; } = true;
        public int    WindowWidth    { get; set; } = 1180;
        public int    WindowHeight   { get; set; } = 760;
        public string WindowState    { get; set; } = "Maximized";
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
                    return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path)) ?? new AppSettings();
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