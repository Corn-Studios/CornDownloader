using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CornDownloader
{
    public enum InstallStatus
    {
        Pending,
        Downloading,
        Installing,
        Success,
        Failed,
        Skipped
    }

    public class InstallResult
    {
        public AppEntry App { get; set; }
        public InstallStatus Status { get; set; }
        public string Message { get; set; }
    }

    public class DownloadManager
    {
        public bool WingetAvailable { get; private set; }

        public DownloadManager()
        {
            WingetAvailable = CheckWinget();
        }

        private bool CheckWinget()
        {
            try
            {
                var psi = new ProcessStartInfo("winget", "--version")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var p = Process.Start(psi))
                {
                    p.WaitForExit(3000);
                    return p.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Runs 'winget source update' silently in the background so subsequent
        /// installs and upgrade checks use fresh package data.
        /// </summary>
        public async Task RefreshSourcesAsync()
        {
            if (!WingetAvailable) return;
            try
            {
                var psi = new ProcessStartInfo("winget", "source update")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                };
                using var proc = Process.Start(psi);
                await Task.Run(() => proc.WaitForExit(30_000)); // 30s timeout
            }
            catch { }
        }

        /// <summary>
        /// Uses 'winget export' to get a clean JSON list of all installed apps,
        /// then matches against the catalog. JSON is far more reliable than parsing
        /// the formatted table output of 'winget list'.
        /// </summary>
        public async Task<HashSet<string>> GetAllInstalledIdsAsync()
        {
            var installed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!WingetAvailable) return installed;

            string tempFile = Path.Combine(Path.GetTempPath(), $"corndownloader_{Guid.NewGuid():N}.json");

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName        = "winget",
                    Arguments       = $"export -o \"{tempFile}\" --accept-source-agreements --include-versions",
                    UseShellExecute = false,
                    CreateNoWindow  = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true
                };

                var tcs  = new TaskCompletionSource<bool>();
                var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
                proc.Exited += (s, e) => tcs.TrySetResult(true);
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                await Task.WhenAny(tcs.Task, Task.Delay(20000));

                if (!File.Exists(tempFile)) return installed;

                string json = File.ReadAllText(tempFile, System.Text.Encoding.UTF8);

                // The JSON structure is:
                // { "Sources": [ { "Packages": [ { "PackageIdentifier": "Brave.Brave" }, ... ] } ] }
                // Parse properly to avoid false positives (e.g. "Git.Git" matching "Git.GitLFS").
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("Sources", out var sources))
                    {
                        foreach (var source in sources.EnumerateArray())
                        {
                            if (!source.TryGetProperty("Packages", out var packages)) continue;
                            foreach (var pkg in packages.EnumerateArray())
                            {
                                if (pkg.TryGetProperty("PackageIdentifier", out var idProp))
                                    installed.Add(idProp.GetString() ?? "");
                            }
                        }
                    }
                }
                catch { /* malformed JSON — fall back to empty set */ }
            }
            catch { }
            finally
            {
                try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
            }

            return installed;
        }

        /// <summary>
        /// Runs 'winget upgrade' and returns a HashSet of winget IDs that have updates available.
        /// </summary>
        public async Task<HashSet<string>> GetAvailableUpdatesAsync()
        {
            var updatable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!WingetAvailable) return updatable;

            try
            {
                // Export upgradeable packages to JSON — same reliable approach as installed scan
                var psi = new ProcessStartInfo
                {
                    FileName               = "winget",
                    Arguments              = $"upgrade --accept-source-agreements",
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                var tcs    = new TaskCompletionSource<bool>();
                var proc   = new Process { StartInfo = psi, EnableRaisingEvents = true };
                var output = new System.Text.StringBuilder();

                proc.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null) output.AppendLine(e.Data);
                };
                proc.Exited += (s, e) => tcs.TrySetResult(true);
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                await Task.WhenAny(tcs.Task, Task.Delay(20000));

                // Strip non-ASCII chars and match catalog IDs
                var clean = new System.Text.StringBuilder();
                foreach (char c in output.ToString())
                    if (c == '\n' || c == '\r' || (c >= 32 && c <= 126))
                        clean.Append(c);

                string cleanOutput = clean.ToString();

                // Build a set of all whitespace-delimited tokens in the output so we
                // can do exact-match lookups rather than substring IndexOf, which would
                // cause "Git.Git" to match lines that contain "Git.GitLFS", etc.
                var tokens = new HashSet<string>(
                    cleanOutput.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var app in AppCatalog.All)
                {
                    if (string.IsNullOrEmpty(app.WingetId)) continue;
                    if (tokens.Contains(app.WingetId))
                        updatable.Add(app.WingetId);
                }
            }
            catch { }

            return updatable;
        }

        /// <summary>
        /// Queries 'winget show --id X' and returns the list of available versions,
        /// most-recent first. Returns an empty list if winget is unavailable or the
        /// app has no winget ID.
        /// </summary>
        public async Task<List<string>> GetAvailableVersionsAsync(AppEntry app)
        {
            var versions = new List<string>();
            if (!WingetAvailable || string.IsNullOrEmpty(app.WingetId)) return versions;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName               = "winget",
                    Arguments              = $"show --id {app.WingetId} --versions --accept-source-agreements",
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                var tcs    = new TaskCompletionSource<bool>();
                var proc   = new Process { StartInfo = psi, EnableRaisingEvents = true };
                var lines  = new List<string>();

                proc.OutputDataReceived += (s, e) => { if (e.Data != null) lines.Add(e.Data); };
                proc.Exited += (s, e) => tcs.TrySetResult(true);
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                await Task.WhenAny(tcs.Task, Task.Delay(15_000));

                // winget --versions output looks like:
                //   Version
                //   -------
                //   129.0.0.0
                //   128.0.0.0
                // Skip header rows; version lines are all digits and dots.
                bool pastHeader = false;
                foreach (var line in lines)
                {
                    string t = line.Trim();
                    if (!pastHeader)
                    {
                        if (t.StartsWith("---")) pastHeader = true;
                        continue;
                    }
                    if (t.Length > 0 && (char.IsDigit(t[0]) || t[0] == 'v'))
                        versions.Add(t);
                }
            }
            catch { }

            return versions;
        }

        /// <summary>
        /// Upgrades an already-installed app via winget upgrade.
        /// </summary>
        public async Task<InstallResult> UpgradeAsync(AppEntry app, Action<string> onProgress)
        {
            var result = new InstallResult { App = app, Status = InstallStatus.Installing };
            onProgress?.Invoke($"Upgrading {app.Name}...");

            try
            {
                var tcs = new TaskCompletionSource<bool>();
                var psi = new ProcessStartInfo
                {
                    FileName               = "winget",
                    Arguments              = $"upgrade --id {app.WingetId} --silent --accept-source-agreements --accept-package-agreements",
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                };

                var proc   = new Process { StartInfo = psi, EnableRaisingEvents = true };
                var output = new System.Text.StringBuilder();

                proc.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        output.AppendLine(e.Data);
                        onProgress?.Invoke(e.Data);
                    }
                };
                proc.Exited += (s, e) => tcs.TrySetResult(true);
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                await tcs.Task;

                if (proc.ExitCode == 0 || proc.ExitCode == -1978335189)
                {
                    result.Status  = InstallStatus.Success;
                    result.Message = $"{app.Name} upgraded successfully.";
                }
                else
                {
                    result.Status  = InstallStatus.Failed;
                    result.Message = $"winget upgrade exited with code {proc.ExitCode}.";
                }
            }
            catch (Exception ex)
            {
                result.Status  = InstallStatus.Failed;
                result.Message = ex.Message;
            }

            return result;
        }

        public async Task<InstallResult> InstallAsync(
            AppEntry app,
            string downloadFolder,
            bool preferWinget,
            Action<string> onProgress)
        {
            var result = new InstallResult { App = app, Status = InstallStatus.Pending };

            // Bundled apps (e.g. FancyZones inside PowerToys) can't be installed standalone.
            if (!string.IsNullOrEmpty(app.IsBundledWith))
            {
                result.Status  = InstallStatus.Skipped;
                result.Message = $"{app.Name} is included with {app.IsBundledWith} — install that instead.";
                return result;
            }

            bool useWinget = preferWinget && WingetAvailable && !string.IsNullOrEmpty(app.WingetId);
            bool hasDirect = !string.IsNullOrEmpty(app.DirectUrl) && !string.IsNullOrEmpty(app.FileName);

            // Fallback logic
            if (!useWinget && !hasDirect)
            {
                result.Status = InstallStatus.Failed;
                result.Message = "No installation method available.";
                return result;
            }

            if (useWinget)
            {
                return await InstallViaWinget(app, result, onProgress);
            }
            else
            {
                return await InstallViaDirectUrl(app, downloadFolder, result, onProgress);
            }
        }

        private async Task<InstallResult> InstallViaWinget(
            AppEntry app,
            InstallResult result,
            Action<string> onProgress)
        {
            result.Status = InstallStatus.Installing;
            onProgress?.Invoke($"Installing {app.Name} via winget...");

            try
            {
                var tcs = new TaskCompletionSource<bool>();
                var psi = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = string.IsNullOrEmpty(app.PinnedVersion)
                        ? $"install --id {app.WingetId} --silent --accept-source-agreements --accept-package-agreements"
                        : $"install --id {app.WingetId} --version \"{app.PinnedVersion}\" --silent --accept-source-agreements --accept-package-agreements",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
                var output = new System.Text.StringBuilder();

                proc.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        output.AppendLine(e.Data);
                        onProgress?.Invoke(e.Data);
                    }
                };
                proc.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        output.AppendLine("[ERR] " + e.Data);
                };
                proc.Exited += (s, e) => tcs.TrySetResult(true);

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                await tcs.Task;

                if (proc.ExitCode == 0 || proc.ExitCode == -1978335189) // already installed
                {
                    result.Status = InstallStatus.Success;
                    result.Message = "Installed successfully via winget.";
                }
                else
                {
                    result.Status = InstallStatus.Failed;
                    result.Message = $"winget exited with code {proc.ExitCode}.";
                }
            }
            catch (Exception ex)
            {
                result.Status = InstallStatus.Failed;
                result.Message = ex.Message;
            }

            return result;
        }

        private static readonly HttpClient _httpClient = new HttpClient();

        static DownloadManager()
        {
            // Set the user-agent once. Calling ParseAdd on every request throws if
            // the header already exists from a previous download.
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CornDownloader/1.1");
        }

        private async Task<InstallResult> InstallViaDirectUrl(
            AppEntry app,
            string downloadFolder,
            InstallResult result,
            Action<string> onProgress)
        {
            result.Status = InstallStatus.Downloading;
            onProgress?.Invoke($"Downloading {app.Name}...");

            string destPath = Path.Combine(downloadFolder, app.FileName);

            try
            {
                using (var response = await _httpClient.GetAsync(app.DirectUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    long? totalBytes = response.Content.Headers.ContentLength;

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var file   = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer    = new byte[81920];
                        long read     = 0;
                        int  bytes;
                        while ((bytes = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await file.WriteAsync(buffer, 0, bytes);
                            read += bytes;
                            if (totalBytes > 0)
                            {
                                int pct = (int)(read * 100 / totalBytes.Value);
                                onProgress?.Invoke($"Downloading {app.Name}: {pct}%");
                            }
                        }
                    }
                }

                onProgress?.Invoke($"Download complete. Launching installer for {app.Name}...");
                result.Status = InstallStatus.Installing;

                string ext = Path.GetExtension(app.FileName).ToLowerInvariant();
                string args = ext == ".msi" ? $"/i \"{destPath}\" /passive /norestart" : "/S /silent /quiet /passive /norestart";

                var psi = ext == ".msi"
                    ? new ProcessStartInfo("msiexec", $"/i \"{destPath}\" /passive /norestart")
                    {
                        UseShellExecute = true,
                        Verb = "runas"
                    }
                    : new ProcessStartInfo(destPath, "/S /silent /quiet /passive /norestart")
                    {
                        UseShellExecute = true,
                        Verb = "runas"
                    };

                var proc = Process.Start(psi);
                await Task.Run(() => proc.WaitForExit());

                // Exit code 0 = success; many silent installers also use 1641 (reboot needed)
                // or 3010 (reboot scheduled) as non-error codes.
                int exitCode = proc.ExitCode;
                if (exitCode == 0 || exitCode == 1641 || exitCode == 3010)
                {
                    result.Status  = InstallStatus.Success;
                    result.Message = "Installer ran successfully.";
                }
                else
                {
                    result.Status  = InstallStatus.Failed;
                    result.Message = $"Installer exited with code {exitCode}.";
                }
            }
            catch (Exception ex)
            {
                result.Status = InstallStatus.Failed;
                result.Message = ex.Message;
            }
            finally
            {
                // Always clean up the downloaded installer file regardless of outcome.
                try { if (File.Exists(destPath)) File.Delete(destPath); } catch { }
            }

            return result;
        }

        public async Task<List<InstallResult>> InstallAllAsync(
            List<AppEntry> apps,
            string downloadFolder,
            bool preferWinget,
            Action<AppEntry, InstallStatus, string> onAppProgress,
            Action<int, int> onOverallProgress)
        {
            var results     = new List<InstallResult>();
            var resultsLock = new object();
            int total       = apps.Count;
            int done        = 0;

            // Allow up to 3 concurrent installs — enough to saturate bandwidth
            // without hammering the system or causing installer conflicts.
            var semaphore = new System.Threading.SemaphoreSlim(3, 3);

            var tasks = apps.Select(async app =>
            {
                await semaphore.WaitAsync();
                try
                {
                    onAppProgress?.Invoke(app, InstallStatus.Installing, $"Starting {app.Name}...");

                    var result = await InstallAsync(app, downloadFolder, preferWinget,
                        msg => onAppProgress?.Invoke(app, InstallStatus.Installing, msg));

                    lock (resultsLock)
                    {
                        results.Add(result);
                        done++;
                    }

                    onOverallProgress?.Invoke(done, total);
                    onAppProgress?.Invoke(app, result.Status, result.Message);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            return results;
        }
    }
}