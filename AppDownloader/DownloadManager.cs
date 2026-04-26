using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace AppDownloader
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
        /// Runs a single 'winget list' and returns a HashSet of all installed winget IDs.
        /// Writes to a temp file to avoid encoding corruption from winget's progress spinner.
        /// </summary>
        public async Task<HashSet<string>> GetAllInstalledIdsAsync()
        {
            var installed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!WingetAvailable) return installed;

            string tempFile = Path.Combine(Path.GetTempPath(), $"corndownloader_winget_{Guid.NewGuid():N}.txt");

            try
            {
                // Redirect winget output directly — bypass the progress-spinner encoding issue
                // by setting TERM=dumb and using UTF-8 code page
                var psi = new ProcessStartInfo
                {
                    FileName               = "winget",
                    Arguments              = "list --accept-source-agreements --disable-interactivity",
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    StandardOutputEncoding = System.Text.Encoding.Unicode
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

                // Timeout after 15 seconds to avoid hanging on slow machines
                await Task.WhenAny(tcs.Task, Task.Delay(15000));

                // Strip non-printable characters (box-drawing, escape codes, etc.)
                var clean = new System.Text.StringBuilder();
                foreach (char c in output.ToString())
                {
                    if (c == '\n' || c == '\r' || (c >= 32 && c <= 126))
                        clean.Append(c);
                }
                string cleanOutput = clean.ToString();

                foreach (var app in AppCatalog.All)
                {
                    if (string.IsNullOrEmpty(app.WingetId)) continue;
                    if (cleanOutput.IndexOf(app.WingetId, StringComparison.OrdinalIgnoreCase) >= 0)
                        installed.Add(app.WingetId);
                }
            }
            catch { }
            finally
            {
                try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
            }

            return installed;
        }

        public async Task<InstallResult> InstallAsync(
            AppEntry app,
            string downloadFolder,
            bool preferWinget,
            Action<string> onProgress)
        {
            var result = new InstallResult { App = app, Status = InstallStatus.Pending };

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
                    Arguments = $"install --id {app.WingetId} --silent --accept-source-agreements --accept-package-agreements",
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
                using (var wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "AppDownloader/1.0");
                    wc.DownloadProgressChanged += (s, e) =>
                    {
                        onProgress?.Invoke($"Downloading {app.Name}: {e.ProgressPercentage}%");
                    };
                    await wc.DownloadFileTaskAsync(new Uri(app.DirectUrl), destPath);
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

                result.Status = InstallStatus.Success;
                result.Message = $"Installer ran. File saved to: {destPath}";
            }
            catch (Exception ex)
            {
                result.Status = InstallStatus.Failed;
                result.Message = ex.Message;
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