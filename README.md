# 🌽 CornDownloader

> A fast, open-source Windows app installer built in C# / WinForms by Corn Studios.  
> Drop it on a fresh Windows install, pick your apps, and hit install — winget handles the rest.

**Version:** `1.2.0`  
**Platform:** Windows 10 / 11 (64-bit)  
**Runtime:** .NET 10 Desktop Runtime  
**License:** MIT

---

## Features

### ⬇ Dual Install Methods
Every app in the catalog supports either **winget** (preferred, silent, automatic) or a **direct URL** download as a fallback. CornDownloader detects whether winget is available at launch and switches modes automatically — direct URL installs are still fully silent, using `/S /passive /norestart` flags and UAC elevation where needed.

### 🔍 Installed App Detection
On startup, CornDownloader scans your system via `winget export` and automatically marks any catalog apps you already have installed. The scan is a single fast JSON parse — no per-app queries.

### ⬆ One-Click Upgrade
After the installed scan, CornDownloader runs `winget upgrade` in the background and surfaces a **⬆ Update N Apps** button in the top bar if any catalog apps have updates available. Clicking it upgrades everything in one pass, with per-app progress and a summary dialog.

### 🔄 Winget Source Refresh
Sources are refreshed silently on startup via `winget source update` so installs and upgrade checks always use fresh package data.

### ⚡ Parallel Installs
Up to **3 apps install concurrently** — enough to saturate most connections without causing installer conflicts. A progress bar and live status label track the queue in real time.

### ✗ Cancel at Any Time
A **Cancel** button appears in the bottom bar as soon as an install or upgrade run starts. Clicking it gracefully aborts the remaining queue — in-flight processes are killed, and anything not yet started is skipped. The summary still reports what succeeded before the cancellation.

### 🔁 Force Reinstall
Already-installed tiles are grayed out and unselectable by default. **Right-clicking** any installed tile reveals a **Force Reinstall** option, which re-queues it with `--force` and marks it with an orange border so it stands out from a normal selection. Right-click again to cancel the force before running.

### 💬 Tile Tooltips
Hovering over any app tile shows a tooltip with the **full description** and the **winget package ID** — useful for apps whose names get truncated in the tile view.

### 📋 Summary Dialog
After every install or upgrade run, a summary dialog breaks results into **Installed successfully** and **Failed** sections, with a **↺ Retry Failed** button to immediately retry any apps that errored.

### 🔎 Search & Category Filtering
A live search box filters the app grid by name or description as you type. The sidebar lets you jump to any category — Browsers, Dev Tools, Media & Entertainment, Productivity, Gaming, Utilities & System Tools, and Customization — with a count badge showing selected vs. total for each.

### 📁 Download Folder Picker
For direct URL installs, you can set any download folder via the bottom bar. The selection is remembered between sessions via `settings.json`.

### 📟 Live Log Panel
A toggleable terminal-style log panel at the bottom of the window streams real-time output from winget and installer processes — useful for diagnosing failures without leaving the app.

### 💾 Persistent Settings
Window size, window state, download folder, and winget preference are saved automatically to `%AppData%\CornStudios\CornDownloader\settings.json` and restored on next launch. The app opens **maximized** by default.

---

## App Catalog

**125 apps** across 7 categories:

| Category | Apps |
|---|---|
| 🌐 Browsers | Firefox, Chrome, Brave, Chromium, Opera GX, Tor Browser, Waterfox, LibreWolf, Min, Zen |
| 💻 Dev Tools | VS Code, Visual Studio 2022, Git, Node.js, Python, Windows Terminal, GitHub Desktop, Postman, Docker, PowerShell 7, JetBrains Toolbox, Neovim, WSL, Insomnia, FileZilla, HeidiSQL, Wireshark, Blockbench, PyPy, Rust, Go, Android Studio, and more |
| 🎬 Media & Entertainment | VLC, Spotify, OBS, Audacity, HandBrake, MPC-HC, iTunes, Plex, Stremio, foobar2000, ImageGlass, FreeTube, GIMP, DaVinci Resolve, Streamlink Twitch GUI, Blender, and more |
| 📋 Productivity | Notion, Obsidian, Slack, Zoom, LibreOffice, Notepad++, ShareX, Bitwarden, Thunderbird, Stretchly, Greenshot, WhatsApp Desktop, Ferdium, Claude Desktop, and more |
| 🎮 Gaming | Steam, Epic Games, GOG Galaxy, EA App, Ubisoft Connect, Discord, MSI Afterburner, Playnite, Minecraft, Prism Launcher, Heroic, Xbox App, Sunshine, Parsec, Overwolf, Medal, Itch.io, Battle.net, Rockstar Games Launcher, Vortex, Nexus Mod Manager, Mod Organizer 2, CapFrameX, and more |
| 🔧 Utilities & System Tools | 7-Zip, NanaZip, Everything Search, CPU-Z, HWiNFO, HWMonitor, CrystalDiskInfo, WinDirStat, Autoruns, Malwarebytes, PowerToys, GPU-Z, Revo Uninstaller, OpenVPN, ProtonVPN, WireGuard, Microsoft PC Manager, BleachBit, O&O ShutUp10++, Bulk Rename Utility, Process Hacker, Ventoy, Rufus, EqualizerAPO, and more |
| 🎨 Customization | Rainmeter, Lively Wallpaper, TranslucentTB, StartAllBack, EarTrumpet, Windhawk, YASB, ModernFlyouts, Komorebi, GlazeWM, ExplorerPatcher, FancyZones (PowerToys) |

---

## Requirements

- Windows 10 or 11 (64-bit)
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)
- [winget](https://aka.ms/getwinget) *(recommended — direct URL fallback available without it)*

---

## Installation

1. Go to [Releases](https://github.com/ConnorCorn07/CornDownloader/releases) and download the latest `.exe`
2. Run it — no installation required, it's a single portable executable

## Build from Source

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
2. Clone the repo:
   ```
   git clone https://github.com/ConnorCorn07/CornDownloader.git
   ```
3. Build:
   ```
   cd CornDownloader
   dotnet build -c Release
   ```
4. Run:
   ```
   bin\Release\net10.0-windows\CornDownloader.exe
   ```

---

## Notes

- Winget installs are fully silent — no installer windows appear
- Direct URL installs launch the installer with silent flags and request elevation via UAC once
- Installer files downloaded via direct URL are deleted automatically after a successful run
- The "prefer winget" checkbox is disabled automatically if winget is not detected on your system
- Settings are stored in `%AppData%\CornStudios\CornDownloader\settings.json`

---

## License

MIT — see [LICENSE](LICENSE)

---

## AI Disclosure

> ⚠ This project contains code written with the assistance of **Claude by Anthropic** (claude.ai).  
> Portions of the UI, download logic, and app catalog were developed with Claude Sonnet. All code has been reviewed and tested by the project maintainer.
