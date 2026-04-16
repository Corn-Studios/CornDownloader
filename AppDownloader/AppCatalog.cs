using System.Collections.Generic;

namespace AppDownloader
{
    public enum DownloadMethod
    {
        Winget,
        DirectUrl
    }

    public class AppEntry
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string WingetId { get; set; }          // null if not available
        public string DirectUrl { get; set; }          // null if not available
        public string FileName { get; set; }           // for direct downloads
        public DownloadMethod PreferredMethod { get; set; }
        public string IconChar { get; set; }           // emoji icon
    }

    public static class AppCatalog
    {
        public static List<AppEntry> All => new List<AppEntry>
        {
            // ── BROWSERS ──────────────────────────────────────────────────────

            new AppEntry {
                Name = "Mozilla Firefox", Category = "Browsers", IconChar = "🦊",
                Description = "Privacy-focused open-source browser",
                WingetId = "Mozilla.Firefox",
                DirectUrl = "https://download.mozilla.org/?product=firefox-latest&os=win64&lang=en-US",
                FileName = "FirefoxSetup.exe",
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Chromium", Category = "Browsers", IconChar = "🔵",
                Description = "Open-source base browser behind Chrome",
                WingetId = "Hibbiki.Chromium",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },

            new AppEntry {
                Name = "Brave Browser", Category = "Browsers", IconChar = "🦁",
                Description = "Privacy-first browser with ad-blocking",
                WingetId = "Brave.Brave",
                DirectUrl = "https://laptop-updates.brave.com/latest/winx64",
                FileName = "BraveSetup.exe",
                PreferredMethod = DownloadMethod.Winget
            },



            // ── DEV TOOLS ─────────────────────────────────────────────────────
            new AppEntry {
                Name = "Visual Studio Code", Category = "Dev Tools", IconChar = "💙",
                Description = "Lightweight code editor by Microsoft",
                WingetId = "Microsoft.VisualStudioCode",
                DirectUrl = "https://code.visualstudio.com/sha/download?build=stable&os=win32-x64-user",
                FileName = "VSCodeSetup.exe",
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Git", Category = "Dev Tools", IconChar = "🔀",
                Description = "Distributed version control system",
                WingetId = "Git.Git",
                DirectUrl = "https://github.com/git-for-windows/git/releases/latest/download/Git-2.44.0-64-bit.exe",
                FileName = "GitSetup.exe",
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Node.js (LTS)", Category = "Dev Tools", IconChar = "🟢",
                Description = "JavaScript runtime for server-side development",
                WingetId = "OpenJS.NodeJS.LTS",
                DirectUrl = "https://nodejs.org/dist/latest-v20.x/node-v20.11.1-x64.msi",
                FileName = "NodeJS.msi",
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Python 3", Category = "Dev Tools", IconChar = "🐍",
                Description = "Popular general-purpose scripting language",
                WingetId = "Python.Python.3.12",
                DirectUrl = "https://www.python.org/ftp/python/3.12.2/python-3.12.2-amd64.exe",
                FileName = "PythonSetup.exe",
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Windows Terminal", Category = "Dev Tools", IconChar = "⬛",
                Description = "Modern terminal with tabs, GPU acceleration",
                WingetId = "Microsoft.WindowsTerminal",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "GitHub Desktop", Category = "Dev Tools", IconChar = "🐙",
                Description = "GUI client for GitHub repositories",
                WingetId = "GitHub.GitHubDesktop",
                DirectUrl = "https://central.github.com/deployments/desktop/desktop/latest/win32",
                FileName = "GitHubDesktopSetup.exe",
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Postman", Category = "Dev Tools", IconChar = "📮",
                Description = "API testing and development platform",
                WingetId = "Postman.Postman",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Docker Desktop", Category = "Dev Tools", IconChar = "🐳",
                Description = "Container platform for developers",
                WingetId = "Docker.DockerDesktop",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },

            new AppEntry {
                Name = "PowerShell 7", Category = "Dev Tools", IconChar = "🔷",
                Description = "Cross-platform task automation shell",
                WingetId = "Microsoft.PowerShell",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },

            // ── MEDIA & ENTERTAINMENT ─────────────────────────────────────────
            new AppEntry {
                Name = "VLC Media Player", Category = "Media & Entertainment", IconChar = "🎬",
                Description = "Universal media player, plays everything",
                WingetId = "VideoLAN.VLC",
                DirectUrl = "https://get.videolan.org/vlc/last/win64/",
                FileName = "VLCSetup.exe",
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Spotify", Category = "Media & Entertainment", IconChar = "🎵",
                Description = "Music and podcast streaming service",
                WingetId = "Spotify.Spotify",
                DirectUrl = "https://download.scdn.co/SpotifySetup.exe",
                FileName = "SpotifySetup.exe",
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "OBS Studio", Category = "Media & Entertainment", IconChar = "📹",
                Description = "Free streaming and screen recording software",
                WingetId = "OBSProject.OBSStudio",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Audacity", Category = "Media & Entertainment", IconChar = "🎙️",
                Description = "Free multi-track audio editor and recorder",
                WingetId = "Audacity.Audacity",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "HandBrake", Category = "Media & Entertainment", IconChar = "📼",
                Description = "Open-source video transcoder",
                WingetId = "HandBrake.HandBrake",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "MPC-HC", Category = "Media & Entertainment", IconChar = "▶️",
                Description = "Lightweight, open-source media player",
                WingetId = "clsid2.mpc-hc",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "K-Lite Codec Pack", Category = "Media & Entertainment", IconChar = "🎞️",
                Description = "Complete codec pack for media playback",
                WingetId = null,
                DirectUrl = "https://files2.codecguide.com/K-Lite_Codec_Pack_1865_Basic.exe",
                FileName = "KLiteCodecPack.exe",
                PreferredMethod = DownloadMethod.DirectUrl
            },
            new AppEntry {
                Name = "iTunes", Category = "Media & Entertainment", IconChar = "🎶",
                Description = "Apple's media player and device manager",
                WingetId = "Apple.iTunes",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },

            // ── PRODUCTIVITY ──────────────────────────────────────────────────
            new AppEntry {
                Name = "Notion", Category = "Productivity", IconChar = "📓",
                Description = "All-in-one notes, docs, and project management",
                WingetId = "Notion.Notion",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Obsidian", Category = "Productivity", IconChar = "🔮",
                Description = "Markdown-based knowledge management app",
                WingetId = "Obsidian.Obsidian",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Slack", Category = "Productivity", IconChar = "💬",
                Description = "Team messaging and collaboration platform",
                WingetId = "SlackTechnologies.Slack",
                DirectUrl = "https://slack.com/downloads/windows",
                FileName = "SlackSetup.exe",
                PreferredMethod = DownloadMethod.Winget
            },

            new AppEntry {
                Name = "Zoom", Category = "Productivity", IconChar = "📞",
                Description = "Video conferencing and online meetings",
                WingetId = "Zoom.Zoom",
                DirectUrl = "https://zoom.us/client/latest/ZoomInstallerFull.exe",
                FileName = "ZoomSetup.exe",
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "LibreOffice", Category = "Productivity", IconChar = "📄",
                Description = "Free open-source Office suite alternative",
                WingetId = "TheDocumentFoundation.LibreOffice",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Notepad++", Category = "Productivity", IconChar = "📝",
                Description = "Advanced text editor for Windows",
                WingetId = "Notepad++.Notepad++",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "ShareX", Category = "Productivity", IconChar = "📸",
                Description = "Powerful screenshot and screen recording tool",
                WingetId = "ShareX.ShareX",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },

            // ── GAMING ────────────────────────────────────────────────────────
            new AppEntry {
                Name = "Steam", Category = "Gaming", IconChar = "🎮",
                Description = "Valve's PC gaming platform and storefront",
                WingetId = "Valve.Steam",
                DirectUrl = "https://cdn.akamai.steamstatic.com/client/installer/SteamSetup.exe",
                FileName = "SteamSetup.exe",
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Epic Games Launcher", Category = "Gaming", IconChar = "🚀",
                Description = "Epic Games store and launcher",
                WingetId = "EpicGames.EpicGamesLauncher",
                DirectUrl = "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/installer/download/EpicGamesLauncherInstaller.msi",
                FileName = "EpicGamesSetup.msi",
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "GOG Galaxy", Category = "Gaming", IconChar = "⭐",
                Description = "DRM-free game platform by CD Projekt",
                WingetId = "GOG.Galaxy",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "EA App", Category = "Gaming", IconChar = "🕹️",
                Description = "EA's game launcher (replaces Origin)",
                WingetId = "ElectronicArts.EADesktop",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Ubisoft Connect", Category = "Gaming", IconChar = "🔵",
                Description = "Ubisoft's game launcher and storefront",
                WingetId = "Ubisoft.Connect",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Discord", Category = "Gaming", IconChar = "🎧",
                Description = "Voice, video, and text chat for gamers",
                WingetId = "Discord.Discord",
                DirectUrl = "https://discord.com/api/downloads/distributions/app/installers/latest?channel=stable&platform=win&arch=x64",
                FileName = "DiscordSetup.exe",
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "MSI Afterburner", Category = "Gaming", IconChar = "🔥",
                Description = "GPU overclocking and monitoring utility",
                WingetId = "Guru3D.Afterburner",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Playnite", Category = "Gaming", IconChar = "📚",
                Description = "Unified game library manager",
                WingetId = "Playnite.Playnite",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },

            // ── UTILITIES & SYSTEM TOOLS ──────────────────────────────────────
            new AppEntry {
                Name = "7-Zip", Category = "Utilities & System Tools", IconChar = "🗜️",
                Description = "Free, high-compression archive manager",
                WingetId = "7zip.7zip",
                DirectUrl = "https://www.7-zip.org/a/7z2401-x64.exe",
                FileName = "7ZipSetup.exe",
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Everything Search", Category = "Utilities & System Tools", IconChar = "🔍",
                Description = "Instant file search across your entire drive",
                WingetId = "voidtools.Everything",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "CPU-Z", Category = "Utilities & System Tools", IconChar = "🖥️",
                Description = "System hardware information tool",
                WingetId = "CPUID.CPU-Z",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "HWiNFO", Category = "Utilities & System Tools", IconChar = "📊",
                Description = "Comprehensive hardware diagnostics and monitoring",
                WingetId = "REALiX.HWiNFO",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "CrystalDiskInfo", Category = "Utilities & System Tools", IconChar = "💾",
                Description = "HDD/SSD health monitoring utility",
                WingetId = "CrystalDewWorld.CrystalDiskInfo",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },


            new AppEntry {
                Name = "WinDirStat", Category = "Utilities & System Tools", IconChar = "📂",
                Description = "Graphical disk usage analyzer",
                WingetId = "WinDirStat.WinDirStat",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Autoruns", Category = "Utilities & System Tools", IconChar = "⚙️",
                Description = "Microsoft Sysinternals startup manager",
                WingetId = "Microsoft.Sysinternals.Autoruns",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Malwarebytes", Category = "Utilities & System Tools", IconChar = "🛡️",
                Description = "Anti-malware and threat protection",
                WingetId = "Malwarebytes.Malwarebytes",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "PowerToys", Category = "Utilities & System Tools", IconChar = "🔧",
                Description = "Microsoft utilities for power users",
                WingetId = "Microsoft.PowerToys",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },

            // ── CUSTOMIZATION / PERSONALIZATION ───────────────────────────────
            new AppEntry {
                Name = "Rainmeter", Category = "Customization", IconChar = "🌦️",
                Description = "Desktop customization with skins and widgets",
                WingetId = "Rainmeter.Rainmeter",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Lively Wallpaper", Category = "Customization", IconChar = "🖼️",
                Description = "Animated live wallpapers for Windows",
                WingetId = "rocksdanister.LivelyWallpaper",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "TranslucentTB", Category = "Customization", IconChar = "🔲",
                Description = "Make your taskbar transparent or blurred",
                WingetId = "CharlesMilette.TranslucentTB",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "StartAllBack", Category = "Customization", IconChar = "🪟",
                Description = "Restore classic Windows taskbar and Start menu",
                WingetId = "StartIsBack.StartAllBack",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "EarTrumpet", Category = "Customization", IconChar = "🔊",
                Description = "Per-app audio volume control for taskbar",
                WingetId = "File-New-Project.EarTrumpet",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Windhawk", Category = "Customization", IconChar = "🦅",
                Description = "Mod manager for Windows system tweaks",
                WingetId = "RamenSoftware.Windhawk",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "YASB (Yet Another Status Bar)", Category = "Customization", IconChar = "📊",
                Description = "Customizable Windows status bar replacement",
                WingetId = "AmN.yasb",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "ModernFlyouts", Category = "Customization", IconChar = "🎨",
                Description = "Modern-styled volume/media overlay for Windows",
                WingetId = "ModernFlyouts.ModernFlyouts",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },

            // ── BROWSERS (additions) ──────────────────────────────────────────
            new AppEntry {
                Name = "Waterfox", Category = "Browsers", IconChar = "🌊",
                Description = "Privacy-focused Firefox-based browser",
                WingetId = "Waterfox.Waterfox",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },

            // ── DEV TOOLS (additions) ─────────────────────────────────────────
            new AppEntry {
                Name = "PyPy", Category = "Dev Tools", IconChar = "🐍",
                Description = "Fast, JIT-compiled Python interpreter",
                WingetId = "PyPy.PyPy",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "JetBrains Toolbox", Category = "Dev Tools", IconChar = "🧰",
                Description = "Manage all JetBrains IDEs in one place",
                WingetId = "JetBrains.Toolbox",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Wireshark", Category = "Dev Tools", IconChar = "🦈",
                Description = "Network protocol analyser and packet capture",
                WingetId = "WiresharkFoundation.Wireshark",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "BlockBench", Category = "Dev Tools", IconChar = "🟫",
                Description = "3D model editor for Minecraft and low-poly art",
                WingetId = "JannisX11.Blockbench",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },

            // ── GAMING (additions) ────────────────────────────────────────────
            new AppEntry {
                Name = "Overwolf", Category = "Gaming", IconChar = "🐺",
                Description = "In-game overlay platform for apps and mods",
                WingetId = "Overwolf.Overwolf",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Medal", Category = "Gaming", IconChar = "🥇",
                Description = "Clip and share your best gaming moments",
                WingetId = "Medal.Medal",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Valorant Tracker", Category = "Gaming", IconChar = "🎯",
                Description = "Stats tracker and overlay for Valorant",
                WingetId = "TrackerNetwork.ValorantTracker",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Prism Launcher", Category = "Gaming", IconChar = "🟩",
                Description = "Open-source Minecraft launcher with mod support",
                WingetId = "PrismLauncher.PrismLauncher",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Minecraft Launcher", Category = "Gaming", IconChar = "⛏️",
                Description = "Official Minecraft Java & Bedrock launcher",
                WingetId = "Mojang.MinecraftLauncher",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },

            // ── UTILITIES & SYSTEM TOOLS (additions) ──────────────────────────
            new AppEntry {
                Name = "GPU-Z", Category = "Utilities & System Tools", IconChar = "🎮",
                Description = "GPU hardware information and diagnostics",
                WingetId = "TechPowerUp.GPU-Z",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Revo Uninstaller", Category = "Utilities & System Tools", IconChar = "🗑️",
                Description = "Deep uninstaller that removes leftover files",
                WingetId = "RevoUninstaller.RevoUninstaller",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "CDBurnerXP", Category = "Utilities & System Tools", IconChar = "💿",
                Description = "Free CD/DVD/Blu-ray burning application",
                WingetId = "CDBurnerXP.CDBurnerXP",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "OpenVPN", Category = "Utilities & System Tools", IconChar = "🔒",
                Description = "Open-source VPN client and server",
                WingetId = "OpenVPNTechnologies.OpenVPN",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "ProtonVPN", Category = "Utilities & System Tools", IconChar = "🛡️",
                Description = "Secure, privacy-first VPN by Proton",
                WingetId = "ProtonTechnologies.ProtonVPN",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "WireGuard", Category = "Utilities & System Tools", IconChar = "🔐",
                Description = "Fast, modern, secure VPN tunnel",
                WingetId = "WireGuard.WireGuard",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Microsoft PC Manager", Category = "Utilities & System Tools", IconChar = "🖥️",
                Description = "Microsoft's official PC cleanup and boost tool",
                WingetId = "Microsoft.PCManager",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },

            // ── PRODUCTIVITY (additions) ──────────────────────────────────────
            new AppEntry {
                Name = "FxSound", Category = "Productivity", IconChar = "🎚️",
                Description = "Audio enhancer and equalizer for Windows",
                WingetId = "FxSound.FxSound",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Claude Desktop", Category = "Productivity", IconChar = "🤖",
                Description = "Anthropic's Claude AI assistant desktop app",
                WingetId = "Anthropic.Claude",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Google Chrome", Category = "Browsers", IconChar = "🌐",
                Description = "Fast, secure web browser by Google",
                WingetId = "Google.Chrome",
                DirectUrl = "https://dl.google.com/chrome/install/ChromeStandaloneSetup64.exe",
                FileName = "ChromeSetup.exe",
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Opera GX", Category = "Browsers", IconChar = "🎮",
                Description = "Gaming browser with CPU/RAM limiters",
                WingetId = "Opera.OperaGX",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Ferdium", Category = "Productivity", IconChar = "💬",
                Description = "All-in-one messaging app (Slack, WhatsApp, etc.)",
                WingetId = "Ferdium.Ferdium",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
            new AppEntry {
                Name = "Parsec", Category = "Gaming", IconChar = "🖥️",
                Description = "Low-latency remote desktop for gaming",
                WingetId = "Parsec.Parsec",
                DirectUrl = null,
                FileName = null,
                PreferredMethod = DownloadMethod.Winget
            },
        };
    }
}