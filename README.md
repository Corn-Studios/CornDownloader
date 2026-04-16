# ⬇ App Downloader

A **zero-dependency** Windows app that lets you browse, select, and install popular software in one click — inspired by Ninite, built with the same clean C#/WinForms approach as [Win11 Optimizer](https://github.com/ConnorCorn07/win11op).

---

## ✨ Features

- **60+ curated apps** across 7 categories
- **Dual install method**: winget (silent, preferred) with automatic fallback to direct download URLs
- **Dark UI** with category sidebar, search, and per-tile status feedback
- **Folder picker** — choose where downloaded installers are saved
- **Zero dependencies** — targets .NET 8 (built into Windows 11, free download for Windows 10), no NuGet packages
- **Activity log panel** — toggle to see real-time install output
- **Per-app status** — tiles show ⏳ Installing → ✔ Done / ✘ Failed

---

## 📂 Categories

| Category | Example Apps |
|---|---|
| 🌐 Browsers | Chrome, Firefox, Brave, Opera GX, Vivaldi |
| 💻 Dev Tools | VS Code, Git, Node.js, Python, Docker, PowerShell 7 |
| 🎬 Media & Entertainment | VLC, Spotify, OBS, Audacity, HandBrake |
| 📋 Productivity | Notion, Obsidian, Zoom, LibreOffice, ShareX |
| 🎮 Gaming | Steam, Epic, Discord, GOG Galaxy, Playnite |
| 🔧 Utilities & System Tools | 7-Zip, PowerToys, Everything Search, HWiNFO, Malwarebytes |
| 🎨 Customization | Rainmeter, Lively Wallpaper, TranslucentTB, Windhawk, ModernFlyouts |

---

## 🚀 Build & Run

### Prerequisites
- Visual Studio 2022 (or `dotnet` CLI)
- .NET 8 SDK — [download here](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

### Build via CLI

```bash
dotnet build AppDownloader.sln -c Release
```

The output `.exe` will be in:
```
AppDownloader\bin\Release\net8.0-windows\AppDownloader.exe
```

### Or open in Visual Studio
1. Open `AppDownloader.sln`
2. Press `Ctrl+F5` to build and run

---

## ⚙️ How It Works

### winget (preferred)
When winget is detected on the machine, apps install silently:
```
winget install --id <WingetId> --silent --accept-source-agreements --accept-package-agreements
```

### Direct URL fallback
For apps without a winget ID, or if winget is unavailable, the app downloads the installer directly to your chosen folder and launches it with silent flags (`/S`, `/passive`). Each `AppEntry` in the catalog can carry both a `WingetId` and a `DirectUrl` — the preferred method is configurable per-app.

---

## 🖥️ Requirements
- Windows 10 or 11
- .NET 8 Runtime *(pre-installed on most Windows 11 machines; [downloadable](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) for Windows 10)*
- Administrator rights (required to launch installers)

---

## 🔧 Changelog

### v1.0.0
- Initial release with 60+ apps across 7 categories
- winget-first install with direct URL fallback
- Dark WinForms UI: category sidebar, app grid tiles, search, folder picker
- Per-tile install status feedback (⏳ / ✔ / ✘)
- Toggleable activity log panel
- Retargeted from .NET Framework 4.8 → **.NET 8** to avoid missing targeting pack errors (`NU1100`)
- Fixed `PlaceholderText` build error (`CS0117`) — `.NET 4.8` doesn't support this property; resolved by upgrading to .NET 8 where it works natively

---

## 📄 License
MIT — free to use, modify, and distribute.

---

## 🙏 Credits
Built by ConnorCorn07 · Companion to [Win11 Optimizer](https://github.com/ConnorCorn07/win11op)
