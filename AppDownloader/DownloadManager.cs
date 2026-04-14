using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            var results = new List<InstallResult>();
            int total = apps.Count;
            int done = 0;

            foreach (var app in apps)
            {
                onAppProgress?.Invoke(app, InstallStatus.Installing, $"Starting {app.Name}...");

                var result = await InstallAsync(app, downloadFolder, preferWinget,
                    msg => onAppProgress?.Invoke(app, InstallStatus.Installing, msg));

                results.Add(result);
                done++;
                onOverallProgress?.Invoke(done, total);
                onAppProgress?.Invoke(app, result.Status, result.Message);
            }

            return results;
        }
    }
}
