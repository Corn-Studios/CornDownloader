# ⬇ App Downloader

A **zero-dependency** Windows app that lets you browse, select, and install popular software in one click — inspired by Ninite, built with the same clean C#/WinForms approach as Win11 Optimizer.

---

## ✨ Features

- **60+ curated apps** across 7 categories
- **Dual install method**: winget (silent, preferred) with automatic fallback to direct download URLs
- **Dark UI** with category sidebar, search, and per-tile status feedback
- **Folder picker** — choose where downloaded installers are saved
- **Zero dependencies** — targets .NET Framework 4.8 (built into Windows 10/11), no NuGet packages
- **Activity log panel** — toggle to see real-time install output
- **Per-app status** — tiles show ⏳ Installing → ✔ Done / ✘ Failed

---

## 📂 Categories

| Category | Example Apps |
|---|---|
| 🌐 Browsers | Chrome, Firefox, Brave, Opera GX |
| 💻 Dev Tools | VS Code, Git, Node.js, Python, Docker |
| 🎬 Media & Entertainment | VLC, Spotify, OBS, Audacity |
| 📋 Productivity | Notion, Obsidian, Zoom, LibreOffice |
| 🎮 Gaming | Steam, Epic, Discord, GOG Galaxy |
| 🔧 Utilities | 7-Zip, PowerToys, Everything Search |
| 🎨 Customization | Rainmeter, Lively Wallpaper, TranslucentTB |

---

## 🚀 Build & Run

### Prerequisites
- Visual Studio 2022 (or `dotnet` CLI)
- .NET Framework 4.8 SDK (included with VS, or downloadable)

### Build

```bash
dotnet build AppDownloader.sln -c Release
```

The output `.exe` will be in:
```
AppDownloader\bin\Release\net48\AppDownloader.exe
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
For apps without winget IDs (or if winget is unavailable), the app downloads the installer to your chosen folder and launches it with silent flags (`/S`, `/passive`).

---

## 🖥️ Requirements
- Windows 10 or 11
- .NET Framework 4.8 *(already included on Win10 1903+ and all Win11)*
- Administrator rights (for installing apps)

---

## 📄 License
MIT — free to use, modify, and distribute.

---

## 🙏 Credits
Built by ConnorCorn07 · Companion to [Win11 Optimizer](https://github.com/ConnorCorn07/win11op)
