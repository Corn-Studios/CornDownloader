using System.Collections.Generic;

namespace CornDownloader
{
    public class AppEntry
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string WingetId { get; set; }          // null if not available
        public string DirectUrl { get; set; }          // null if not available
        public string FileName { get; set; }           // for direct downloads
        public string IconChar { get; set; }           // emoji icon
        public bool IsRecommended { get; set; }        // included in Recommended preset
        public string PinnedVersion { get; set; }      // null = latest; set by user at runtime
        /// <summary>
        /// When non-null, this entry is bundled inside another app (e.g. FancyZones inside PowerToys).
        /// The value is the WingetId of the parent app. This entry cannot be installed standalone.
        /// </summary>
        public string IsBundledWith { get; set; }
        public bool   ForceReinstall { get; set; }   // runtime-only; set by user via right-click
    }

    public static class AppCatalog
    {
        public static readonly List<AppEntry> All = new List<AppEntry>
        {
            // ── BROWSERS ──────────────────────────────────────────────────────

            new AppEntry {
                Name = "Mozilla Firefox", Category = "Browsers", IconChar = "🦊",
                Description = "Privacy-focused open-source browser",
                WingetId = "Mozilla.Firefox",
                DirectUrl = "https://download.mozilla.org/?product=firefox-latest&os=win64&lang=en-US",
                FileName = "FirefoxSetup.exe",
                IsRecommended = true
            },
            new AppEntry {
                Name = "Chromium", Category = "Browsers", IconChar = "🔵",
                Description = "Open-source base browser behind Chrome",
                WingetId = "Hibbiki.Chromium",
                DirectUrl = null,
                FileName = null,
            },

            new AppEntry {
                Name = "Brave Browser", Category = "Browsers", IconChar = "🦁",
                Description = "Privacy-first browser with ad-blocking",
                WingetId = "Brave.Brave",
                DirectUrl = "https://laptop-updates.brave.com/latest/winx64",
                FileName = "BraveSetup.exe",
            },



            // ── DEV TOOLS ─────────────────────────────────────────────────────
            new AppEntry {
                Name = "Visual Studio Code", Category = "Dev Tools", IconChar = "💙",
                Description = "Lightweight code editor by Microsoft",
                WingetId = "Microsoft.VisualStudioCode",
                DirectUrl = "https://code.visualstudio.com/sha/download?build=stable&os=win32-x64-user",
                FileName = "VSCodeSetup.exe",
                IsRecommended = true
            },
            new AppEntry {
                Name = "Git", Category = "Dev Tools", IconChar = "🔀",
                Description = "Distributed version control system",
                WingetId = "Git.Git",
                DirectUrl = null,
                FileName = null,
                IsRecommended = true
            },
            new AppEntry {
                Name = "Node.js (LTS)", Category = "Dev Tools", IconChar = "🟢",
                Description = "JavaScript runtime for server-side development",
                WingetId = "OpenJS.NodeJS.LTS",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Python 3", Category = "Dev Tools", IconChar = "🐍",
                Description = "Popular general-purpose scripting language",
                WingetId = "Python.Python.3.12",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Windows Terminal", Category = "Dev Tools", IconChar = "⬛",
                Description = "Modern terminal with tabs, GPU acceleration",
                WingetId = "Microsoft.WindowsTerminal",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "GitHub Desktop", Category = "Dev Tools", IconChar = "🐙",
                Description = "GUI client for GitHub repositories",
                WingetId = "GitHub.GitHubDesktop",
                DirectUrl = "https://central.github.com/deployments/desktop/desktop/latest/win32",
                FileName = "GitHubDesktopSetup.exe",
            },
            new AppEntry {
                Name = "Postman", Category = "Dev Tools", IconChar = "📮",
                Description = "API testing and development platform",
                WingetId = "Postman.Postman",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Docker Desktop", Category = "Dev Tools", IconChar = "🐳",
                Description = "Container platform for developers",
                WingetId = "Docker.DockerDesktop",
                DirectUrl = null,
                FileName = null,
            },

            new AppEntry {
                Name = "PowerShell 7", Category = "Dev Tools", IconChar = "🔷",
                Description = "Cross-platform task automation shell",
                WingetId = "Microsoft.PowerShell",
                DirectUrl = null,
                FileName = null,
            },

            // ── MEDIA & ENTERTAINMENT ─────────────────────────────────────────
            new AppEntry {
                Name = "VLC Media Player", Category = "Media & Entertainment", IconChar = "🎬",
                Description = "Universal media player, plays everything",
                WingetId = "VideoLAN.VLC",
                DirectUrl = "https://get.videolan.org/vlc/last/win64/",
                FileName = "VLCSetup.exe",
                IsRecommended = true
            },
            new AppEntry {
                Name = "Spotify", Category = "Media & Entertainment", IconChar = "🎵",
                Description = "Music and podcast streaming service",
                WingetId = "Spotify.Spotify",
                DirectUrl = "https://download.scdn.co/SpotifySetup.exe",
                FileName = "SpotifySetup.exe",
            },
            new AppEntry {
                Name = "OBS Studio", Category = "Media & Entertainment", IconChar = "📹",
                Description = "Free streaming and screen recording software",
                WingetId = "OBSProject.OBSStudio",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Audacity", Category = "Media & Entertainment", IconChar = "🎙️",
                Description = "Free multi-track audio editor and recorder",
                WingetId = "Audacity.Audacity",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "HandBrake", Category = "Media & Entertainment", IconChar = "📼",
                Description = "Open-source video transcoder",
                WingetId = "HandBrake.HandBrake",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "MPC-HC", Category = "Media & Entertainment", IconChar = "▶️",
                Description = "Lightweight, open-source media player",
                WingetId = "clsid2.mpc-hc",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "K-Lite Codec Pack", Category = "Media & Entertainment", IconChar = "🎞️",
                Description = "Complete codec pack for media playback",
                WingetId = null,
                DirectUrl = "https://files2.codecguide.com/K-Lite_Codec_Pack_1865_Basic.exe",
                FileName = "KLiteCodecPack.exe",
            },
            new AppEntry {
                Name = "iTunes", Category = "Media & Entertainment", IconChar = "🎶",
                Description = "Apple's media player and device manager",
                WingetId = "Apple.iTunes",
                DirectUrl = null,
                FileName = null,
            },

            // ── PRODUCTIVITY ──────────────────────────────────────────────────
            new AppEntry {
                Name = "Notion", Category = "Productivity", IconChar = "📓",
                Description = "All-in-one notes, docs, and project management",
                WingetId = "Notion.Notion",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Obsidian", Category = "Productivity", IconChar = "🔮",
                Description = "Markdown-based knowledge management app",
                WingetId = "Obsidian.Obsidian",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Slack", Category = "Productivity", IconChar = "💬",
                Description = "Team messaging and collaboration platform",
                WingetId = "SlackTechnologies.Slack",
                DirectUrl = "https://slack.com/downloads/windows",
                FileName = "SlackSetup.exe",
            },

            new AppEntry {
                Name = "Zoom", Category = "Productivity", IconChar = "📞",
                Description = "Video conferencing and online meetings",
                WingetId = "Zoom.Zoom",
                DirectUrl = "https://zoom.us/client/latest/ZoomInstallerFull.exe",
                FileName = "ZoomSetup.exe",
            },
            new AppEntry {
                Name = "LibreOffice", Category = "Productivity", IconChar = "📄",
                Description = "Free open-source Office suite alternative",
                WingetId = "TheDocumentFoundation.LibreOffice",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Notepad++", Category = "Productivity", IconChar = "📝",
                Description = "Advanced text editor for Windows",
                WingetId = "Notepad++.Notepad++",
                DirectUrl = null,
                FileName = null,
                IsRecommended = true
            },
            new AppEntry {
                Name = "ShareX", Category = "Productivity", IconChar = "📸",
                Description = "Powerful screenshot and screen recording tool",
                WingetId = "ShareX.ShareX",
                DirectUrl = null,
                FileName = null,
            },

            // ── GAMING ────────────────────────────────────────────────────────
            new AppEntry {
                Name = "Steam", Category = "Gaming", IconChar = "🎮",
                Description = "Valve's PC gaming platform and storefront",
                WingetId = "Valve.Steam",
                DirectUrl = "https://cdn.akamai.steamstatic.com/client/installer/SteamSetup.exe",
                FileName = "SteamSetup.exe",
                IsRecommended = true
            },
            new AppEntry {
                Name = "Epic Games Launcher", Category = "Gaming", IconChar = "🚀",
                Description = "Epic Games store and launcher",
                WingetId = "EpicGames.EpicGamesLauncher",
                DirectUrl = "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/installer/download/EpicGamesLauncherInstaller.msi",
                FileName = "EpicGamesSetup.msi",
            },
            new AppEntry {
                Name = "GOG Galaxy", Category = "Gaming", IconChar = "⭐",
                Description = "DRM-free game platform by CD Projekt",
                WingetId = "GOG.Galaxy",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "EA App", Category = "Gaming", IconChar = "🕹️",
                Description = "EA's game launcher (replaces Origin)",
                WingetId = "ElectronicArts.EADesktop",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Ubisoft Connect", Category = "Gaming", IconChar = "🟦",
                Description = "Ubisoft's game launcher and storefront",
                WingetId = "Ubisoft.Connect",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Discord", Category = "Gaming", IconChar = "🎧",
                Description = "Voice, video, and text chat for gamers",
                WingetId = "Discord.Discord",
                DirectUrl = "https://discord.com/api/downloads/distributions/app/installers/latest?channel=stable&platform=win&arch=x64",
                FileName = "DiscordSetup.exe",
                IsRecommended = true
            },
            new AppEntry {
                Name = "MSI Afterburner", Category = "Gaming", IconChar = "🔥",
                Description = "GPU overclocking and monitoring utility",
                WingetId = "Guru3D.Afterburner",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Playnite", Category = "Gaming", IconChar = "📚",
                Description = "Unified game library manager",
                WingetId = "Playnite.Playnite",
                DirectUrl = null,
                FileName = null,
            },

            // ── UTILITIES & SYSTEM TOOLS ──────────────────────────────────────
            new AppEntry {
                Name = "7-Zip", Category = "Utilities & System Tools", IconChar = "🗜️",
                Description = "Free, high-compression archive manager",
                WingetId = "7zip.7zip",
                DirectUrl = null,
                FileName = null,
                IsRecommended = true
            },
            new AppEntry {
                Name = "Everything Search", Category = "Utilities & System Tools", IconChar = "🔍",
                Description = "Instant file search across your entire drive",
                WingetId = "voidtools.Everything",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "CPU-Z", Category = "Utilities & System Tools", IconChar = "🖥️",
                Description = "System hardware information tool",
                WingetId = "CPUID.CPU-Z",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "HWiNFO", Category = "Utilities & System Tools", IconChar = "📊",
                Description = "Comprehensive hardware diagnostics and monitoring",
                WingetId = "REALiX.HWiNFO",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "CrystalDiskInfo", Category = "Utilities & System Tools", IconChar = "💾",
                Description = "HDD/SSD health monitoring utility",
                WingetId = "CrystalDewWorld.CrystalDiskInfo",
                DirectUrl = null,
                FileName = null,
            },


            new AppEntry {
                Name = "WinDirStat", Category = "Utilities & System Tools", IconChar = "📂",
                Description = "Graphical disk usage analyzer",
                WingetId = "WinDirStat.WinDirStat",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Autoruns", Category = "Utilities & System Tools", IconChar = "⚙️",
                Description = "Microsoft Sysinternals startup manager",
                WingetId = "Microsoft.Sysinternals.Autoruns",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Malwarebytes", Category = "Utilities & System Tools", IconChar = "🛡️",
                Description = "Anti-malware and threat protection",
                WingetId = "Malwarebytes.Malwarebytes",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "PowerToys", Category = "Utilities & System Tools", IconChar = "🔧",
                Description = "Microsoft utilities for power users",
                WingetId = "Microsoft.PowerToys",
                DirectUrl = null,
                FileName = null,
                IsRecommended = true
            },

            // ── CUSTOMIZATION / PERSONALIZATION ───────────────────────────────
            new AppEntry {
                Name = "Rainmeter", Category = "Customization", IconChar = "🌦️",
                Description = "Desktop customization with skins and widgets",
                WingetId = "Rainmeter.Rainmeter",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Lively Wallpaper", Category = "Customization", IconChar = "🖼️",
                Description = "Animated live wallpapers for Windows",
                WingetId = "rocksdanister.LivelyWallpaper",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "TranslucentTB", Category = "Customization", IconChar = "🔲",
                Description = "Make your taskbar transparent or blurred",
                WingetId = "CharlesMilette.TranslucentTB",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "StartAllBack", Category = "Customization", IconChar = "🪟",
                Description = "Restore classic Windows taskbar and Start menu",
                WingetId = "StartIsBack.StartAllBack",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "EarTrumpet", Category = "Customization", IconChar = "🔊",
                Description = "Per-app audio volume control for taskbar",
                WingetId = "File-New-Project.EarTrumpet",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Windhawk", Category = "Customization", IconChar = "🦅",
                Description = "Mod manager for Windows system tweaks",
                WingetId = "RamenSoftware.Windhawk",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "YASB (Yet Another Status Bar)", Category = "Customization", IconChar = "📌",
                Description = "Customizable Windows status bar replacement",
                WingetId = "AmN.yasb",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "ModernFlyouts", Category = "Customization", IconChar = "🎨",
                Description = "Modern-styled volume/media overlay for Windows",
                WingetId = "ModernFlyouts.ModernFlyouts",
                DirectUrl = null,
                FileName = null,
            },

            // ── BROWSERS (additions) ──────────────────────────────────────────
            new AppEntry {
                Name = "Waterfox", Category = "Browsers", IconChar = "🌊",
                Description = "Privacy-focused Firefox-based browser",
                WingetId = "Waterfox.Waterfox",
                DirectUrl = null,
                FileName = null,
            },

            // ── DEV TOOLS (additions) ─────────────────────────────────────────
            new AppEntry {
                Name = "PyPy", Category = "Dev Tools", IconChar = "🐇",
                Description = "Fast, JIT-compiled Python interpreter",
                WingetId = "PyPy.PyPy",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "JetBrains Toolbox", Category = "Dev Tools", IconChar = "🧰",
                Description = "Manage all JetBrains IDEs in one place",
                WingetId = "JetBrains.Toolbox",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Wireshark", Category = "Dev Tools", IconChar = "🦈",
                Description = "Network protocol analyser and packet capture",
                WingetId = "WiresharkFoundation.Wireshark",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "BlockBench", Category = "Dev Tools", IconChar = "🟫",
                Description = "3D model editor for Minecraft and low-poly art",
                WingetId = "JannisX11.Blockbench",
                DirectUrl = null,
                FileName = null,
            },

            // ── GAMING (additions) ────────────────────────────────────────────
            new AppEntry {
                Name = "Overwolf", Category = "Gaming", IconChar = "🐺",
                Description = "In-game overlay platform for apps and mods",
                WingetId = "Overwolf.Overwolf",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Medal", Category = "Gaming", IconChar = "🥇",
                Description = "Clip and share your best gaming moments",
                WingetId = "Medal.Medal",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Valorant Tracker", Category = "Gaming", IconChar = "🎯",
                Description = "Stats tracker and overlay for Valorant",
                WingetId = "TrackerNetwork.ValorantTracker",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Prism Launcher", Category = "Gaming", IconChar = "🟩",
                Description = "Open-source Minecraft launcher with mod support",
                WingetId = "PrismLauncher.PrismLauncher",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Minecraft Launcher", Category = "Gaming", IconChar = "⛏️",
                Description = "Official Minecraft Java & Bedrock launcher",
                WingetId = "Mojang.MinecraftLauncher",
                DirectUrl = null,
                FileName = null,
            },

            // ── UTILITIES & SYSTEM TOOLS (additions) ──────────────────────────
            new AppEntry {
                Name = "GPU-Z", Category = "Utilities & System Tools", IconChar = "🔬",
                Description = "GPU hardware information and diagnostics",
                WingetId = "TechPowerUp.GPU-Z",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Revo Uninstaller", Category = "Utilities & System Tools", IconChar = "🗑️",
                Description = "Deep uninstaller that removes leftover files",
                WingetId = "RevoUninstaller.RevoUninstaller",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "CDBurnerXP", Category = "Utilities & System Tools", IconChar = "💿",
                Description = "Free CD/DVD/Blu-ray burning application",
                WingetId = "CDBurnerXP.CDBurnerXP",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "OpenVPN", Category = "Utilities & System Tools", IconChar = "🔒",
                Description = "Open-source VPN client and server",
                WingetId = "OpenVPNTechnologies.OpenVPN",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "ProtonVPN", Category = "Utilities & System Tools", IconChar = "🔏",
                Description = "Secure, privacy-first VPN by Proton",
                WingetId = "ProtonTechnologies.ProtonVPN",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "WireGuard", Category = "Utilities & System Tools", IconChar = "🔐",
                Description = "Fast, modern, secure VPN tunnel",
                WingetId = "WireGuard.WireGuard",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Microsoft PC Manager", Category = "Utilities & System Tools", IconChar = "🫧",
                Description = "Microsoft's official PC cleanup and boost tool",
                WingetId = "Microsoft.PCManager",
                DirectUrl = null,
                FileName = null,
            },

            // ── PRODUCTIVITY (additions) ──────────────────────────────────────
            new AppEntry {
                Name = "FxSound", Category = "Productivity", IconChar = "🎚️",
                Description = "Audio enhancer and equalizer for Windows",
                WingetId = "FxSound.FxSound",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Claude Desktop", Category = "Productivity", IconChar = "🧠",
                Description = "Anthropic's Claude AI assistant desktop app",
                WingetId = "Anthropic.Claude",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Google Chrome", Category = "Browsers", IconChar = "🌐",
                Description = "Fast, secure web browser by Google",
                WingetId = "Google.Chrome",
                DirectUrl = "https://dl.google.com/chrome/install/ChromeStandaloneSetup64.exe",
                FileName = "ChromeSetup.exe",
            },
            new AppEntry {
                Name = "Opera GX", Category = "Browsers", IconChar = "🎲",
                Description = "Gaming browser with CPU/RAM limiters",
                WingetId = "Opera.OperaGX",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Ferdium", Category = "Productivity", IconChar = "📬",
                Description = "All-in-one messaging app (Slack, WhatsApp, etc.)",
                WingetId = "Ferdium.Ferdium",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Parsec", Category = "Gaming", IconChar = "💻",
                Description = "Low-latency remote desktop for gaming",
                WingetId = "Parsec.Parsec",
                DirectUrl = null,
                FileName = null,
            },

            // ── BROWSERS (new) ────────────────────────────────────────────────
            new AppEntry {
                Name = "Tor Browser", Category = "Browsers", IconChar = "🧅",
                Description = "Privacy browser that routes traffic through the Tor network",
                WingetId = "TorProject.TorBrowser",
                DirectUrl = null,
                FileName = null,
            },

            // ── DEV TOOLS (new) ───────────────────────────────────────────────
            new AppEntry {
                Name = "Neovim", Category = "Dev Tools", IconChar = "🖊️",
                Description = "Hyperextensible Vim-based text editor",
                WingetId = "Neovim.Neovim",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "WSL (Windows Subsystem for Linux)", Category = "Dev Tools", IconChar = "🐧",
                Description = "Run Linux distributions natively on Windows",
                WingetId = "Microsoft.WSL",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Insomnia", Category = "Dev Tools", IconChar = "😴",
                Description = "Open source API client and design platform",
                WingetId = "Insomnia.Insomnia",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "FileZilla", Category = "Dev Tools", IconChar = "📁",
                Description = "Fast and reliable FTP, FTPS and SFTP client",
                WingetId = "TimKosse.FileZilla.Client",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "HeidiSQL", Category = "Dev Tools", IconChar = "🗄️",
                Description = "Lightweight GUI for MySQL, MariaDB, PostgreSQL and more",
                WingetId = "HeidiSQL.HeidiSQL",
                DirectUrl = null,
                FileName = null,
            },

            // ── MEDIA & ENTERTAINMENT (new) ───────────────────────────────────
            new AppEntry {
                Name = "Plex", Category = "Media & Entertainment", IconChar = "📺",
                Description = "Media server and player for your personal collection",
                WingetId = "Plex.Plex",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Stremio", Category = "Media & Entertainment", IconChar = "📡",
                Description = "Streaming aggregator for movies, shows and web channels",
                WingetId = "Stremio.Stremio",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "foobar2000", Category = "Media & Entertainment", IconChar = "🎛️",
                Description = "Highly customizable audiophile music player",
                WingetId = "PeterPawlowski.foobar2000",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "ImageGlass", Category = "Media & Entertainment", IconChar = "🪞",
                Description = "Lightweight, versatile image viewer for Windows",
                WingetId = "DuongDieuPhap.ImageGlass",
                DirectUrl = null,
                FileName = null,
            },

            // ── PRODUCTIVITY (new) ────────────────────────────────────────────
            new AppEntry {
                Name = "Bitwarden", Category = "Productivity", IconChar = "🔑",
                Description = "Free and open source password manager",
                WingetId = "Bitwarden.Bitwarden",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Thunderbird", Category = "Productivity", IconChar = "⚡",
                Description = "Free and open source email client by Mozilla",
                WingetId = "Mozilla.Thunderbird",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Stretchly", Category = "Productivity", IconChar = "🧘",
                Description = "Break time reminder app to reduce eye strain",
                WingetId = "Stretchly.Stretchly",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Greenshot", Category = "Productivity", IconChar = "📷",
                Description = "Lightweight screenshot tool with annotation support",
                WingetId = "Greenshot.Greenshot",
                DirectUrl = null,
                FileName = null,
            },

            // ── GAMING (new) ──────────────────────────────────────────────────
            new AppEntry {
                Name = "Sunshine", Category = "Gaming", IconChar = "☀️",
                Description = "Self-hosted game streaming host (pairs with Moonlight/Parsec)",
                WingetId = "LizardByte.Sunshine",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Xbox App", Category = "Gaming", IconChar = "🏆",
                Description = "Microsoft's official Xbox PC gaming app",
                WingetId = "Microsoft.GamingApp",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Heroic Games Launcher", Category = "Gaming", IconChar = "🦸",
                Description = "Open source Epic Games and GOG launcher alternative",
                WingetId = "HeroicGamesLauncher.HeroicGamesLauncher",
                DirectUrl = null,
                FileName = null,
            },

            // ── UTILITIES & SYSTEM TOOLS (new) ────────────────────────────────
            new AppEntry {
                Name = "Bulk Rename Utility", Category = "Utilities & System Tools", IconChar = "✏️",
                Description = "Powerful batch file renaming tool for power users",
                WingetId = "TGRMN.BulkRenameUtility",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Process Hacker", Category = "Utilities & System Tools", IconChar = "🪛",
                Description = "Advanced process viewer and system monitor",
                WingetId = "ProcessHacker.ProcessHacker",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Ventoy", Category = "Utilities & System Tools", IconChar = "💽",
                Description = "Create bootable USB drives for multiple ISOs at once",
                WingetId = "Ventoy.Ventoy",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Rufus", Category = "Utilities & System Tools", IconChar = "📀",
                Description = "Create bootable USB drives from ISO files",
                WingetId = "Rufus.Rufus",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "EqualizerAPO", Category = "Utilities & System Tools", IconChar = "📻",
                Description = "System-wide parametric audio equalizer for Windows",
                WingetId = "EqualizerAPO.EqualizerAPO",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "NanaZip", Category = "Utilities & System Tools", IconChar = "🗃️",
                Description = "Modern 7-Zip fork with Windows 11 context menu integration",
                WingetId = "M2Team.NanaZip",
                DirectUrl = null,
                FileName = null,
            },

            // ── CUSTOMIZATION (new) ───────────────────────────────────────────
            new AppEntry {
                Name = "Komorebi", Category = "Customization", IconChar = "🌿",
                Description = "Tiling window manager for Windows",
                WingetId = "LGUG2Z.komorebi",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "GlazeWM", Category = "Customization", IconChar = "✨",
                Description = "Tiling window manager inspired by i3wm",
                WingetId = "glzr-io.glazewm",
                DirectUrl = null,
                FileName = null,
            },

            // ── BROWSERS (batch 2) ────────────────────────────────────────────
            new AppEntry {
                Name = "LibreWolf", Category = "Browsers", IconChar = "🐾",
                Description = "Hardened Firefox fork with enhanced privacy and security",
                WingetId = "LibreWolf.LibreWolf",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Min Browser", Category = "Browsers", IconChar = "◻️",
                Description = "Minimal, distraction-free web browser",
                WingetId = "minbrowser.min",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Zen Browser", Category = "Browsers", IconChar = "☯️",
                Description = "Firefox-based browser with a clean, modern UI",
                WingetId = "Zen-Team.Zen-Browser",
                DirectUrl = null,
                FileName = null,
            },

            // ── DEV TOOLS (batch 2) ───────────────────────────────────────────
            new AppEntry {
                Name = "Visual Studio 2022 Community", Category = "Dev Tools", IconChar = "🟣",
                Description = "Microsoft's full-featured IDE for .NET, C++, and more",
                WingetId = "Microsoft.VisualStudio.2022.Community",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Rust (rustup)", Category = "Dev Tools", IconChar = "🦀",
                Description = "Rust language toolchain installer and version manager",
                WingetId = "Rustlang.Rustup",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Go (Golang)", Category = "Dev Tools", IconChar = "🐹",
                Description = "Google's fast, statically typed compiled language",
                WingetId = "GoLang.Go",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Android Studio", Category = "Dev Tools", IconChar = "🤖",
                Description = "Google's official IDE for Android development",
                WingetId = "Google.AndroidStudio",
                DirectUrl = null,
                FileName = null,
            },

            // ── MEDIA & ENTERTAINMENT (batch 2) ───────────────────────────────
            new AppEntry {
                Name = "FreeTube", Category = "Media & Entertainment", IconChar = "🔴",
                Description = "Private, open-source YouTube desktop client",
                WingetId = "FreeTubeApp.FreeTube",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "GIMP", Category = "Media & Entertainment", IconChar = "🖌️",
                Description = "GNU Image Manipulation Program — free Photoshop alternative",
                WingetId = "GIMP.GIMP",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "DaVinci Resolve", Category = "Media & Entertainment", IconChar = "✂️",
                Description = "Professional-grade video editor with free tier",
                WingetId = "Blackmagic.DaVinciResolve",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Streamlink Twitch GUI", Category = "Media & Entertainment", IconChar = "💜",
                Description = "Watch Twitch streams natively without a browser",
                WingetId = "streamlink.streamlink-twitch-gui",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Blender", Category = "Media & Entertainment", IconChar = "🧊",
                Description = "Open-source 3D modeling, animation, and rendering suite",
                WingetId = "BlenderFoundation.Blender",
                DirectUrl = null,
                FileName = null,
            },

            // ── PRODUCTIVITY (batch 2) ────────────────────────────────────────
            new AppEntry {
                Name = "WhatsApp Desktop", Category = "Productivity", IconChar = "💚",
                Description = "Official WhatsApp client for Windows",
                WingetId = "WhatsApp.WhatsApp",
                DirectUrl = null,
                FileName = null,
            },

            // ── GAMING (batch 2) ──────────────────────────────────────────────
            new AppEntry {
                Name = "Itch.io", Category = "Gaming", IconChar = "🎪",
                Description = "Indie game store and launcher",
                WingetId = "itch.itch",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Battle.net", Category = "Gaming", IconChar = "⚔️",
                Description = "Blizzard's game launcher for WoW, Overwatch, and more",
                WingetId = "Blizzard.BattleNet",
                DirectUrl = "https://www.battle.net/download/getInstallerForGame?os=win&locale=enUS&version=LIVE&gameProgram=BATTLENET_APP",
                FileName = "BattleNetSetup.exe",
            },
            new AppEntry {
                Name = "Rockstar Games Launcher", Category = "Gaming", IconChar = "🌟",
                Description = "Rockstar's launcher for GTA, RDR2, and more",
                WingetId = "Rockstar.RockstarGamesLauncher",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Vortex Mod Manager", Category = "Gaming", IconChar = "🌀",
                Description = "Nexus Mods' official mod manager for hundreds of games",
                WingetId = "NexusMods.Vortex",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "Nexus Mod Manager", Category = "Gaming", IconChar = "📦",
                Description = "Classic mod manager for Nexus Mods (legacy)",
                WingetId = null,
                DirectUrl = "https://github.com/Nexus-Mods/Nexus-Mod-Manager/releases/latest/download/NexusModManager.exe",
                FileName = "NexusModManager.exe",
            },
            new AppEntry {
                Name = "Mod Organizer 2", Category = "Gaming", IconChar = "🗂️",
                Description = "Advanced mod manager for Bethesda games (Skyrim, Fallout, etc.)",
                WingetId = "ModOrganizer2.ModOrganizer2",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "CapFrameX", Category = "Gaming", IconChar = "📈",
                Description = "Frame time analysis and GPU benchmarking tool",
                WingetId = "CXWorld.CapFrameX",
                DirectUrl = null,
                FileName = null,
            },

            // ── UTILITIES & SYSTEM TOOLS (batch 2) ────────────────────────────
            new AppEntry {
                Name = "HWMonitor", Category = "Utilities & System Tools", IconChar = "🌡️",
                Description = "Hardware temperature, voltage, and fan speed monitor",
                WingetId = "CPUID.HWMonitor",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "O&O ShutUp10++", Category = "Utilities & System Tools", IconChar = "🔕",
                Description = "Windows 10/11 privacy and telemetry control tool",
                WingetId = "OO-Software.ShutUp10",
                DirectUrl = null,
                FileName = null,
            },
            new AppEntry {
                Name = "BleachBit", Category = "Utilities & System Tools", IconChar = "🧹",
                Description = "Open-source system cleaner and privacy tool",
                WingetId = "BleachBit.BleachBit",
                DirectUrl = null,
                FileName = null,
            },

            // ── CUSTOMIZATION (batch 2) ───────────────────────────────────────
            new AppEntry {
                Name = "FancyZones (PowerToys)", Category = "Customization", IconChar = "📐",
                Description = "Advanced window snapping layouts — part of Microsoft PowerToys",
                WingetId = null,
                DirectUrl = null,
                FileName = null,
                IsBundledWith = "Microsoft.PowerToys"
            },
            new AppEntry {
                Name = "ExplorerPatcher", Category = "Customization", IconChar = "🛠️",
                Description = "Restore classic Windows 10 taskbar and UI elements on Windows 11",
                WingetId = "valinet.ExplorerPatcher",
                DirectUrl = null,
                FileName = null,
            },

            // ── GAMING (batch 3) ──────────────────────────────────────────────
            new AppEntry {
                Name = "Genshin Impact", Category = "Gaming", IconChar = "⚔️",
                Description = "Open-world gacha action RPG by HoYoverse",
                WingetId = "miHoYo.GenshinImpact",
                DirectUrl = null,
                FileName = null,
            },

            // ── UTILITIES & SYSTEM TOOLS (batch 3) ─────────────────────────────
            new AppEntry {
                Name = "Logitech G HUB", Category = "Utilities & System Tools", IconChar = "🖱️",
                Description = "Configure and customize Logitech G gaming peripherals",
                WingetId = "Logitech.GHUB",
                DirectUrl = "https://download01.logi.com/web/ftp/pub/techsupport/gaming/lghub_installer.exe",
                FileName = "lghub_installer.exe",
            },
            new AppEntry {
                // NOTE: NVIDIA App is NOT on winget — Microsoft's validation pipeline
                // requires physical NVIDIA hardware to test against, which has blocked
                // the community package (see microsoft/winget-pkgs discussion #200910).
                // DirectUrl below is version-pinned (11.0.8.299) since NVIDIA does not
                // publish a stable "latest" redirect link like Firefox/Brave/VS Code do.
                // This WILL go stale — revisit periodically or replace with a small
                // scraper against nvidia.com/en-us/software/nvidia-app/ if that's worth it.
                Name = "NVIDIA App", Category = "Utilities & System Tools", IconChar = "🟩",
                Description = "GPU driver updates, game optimization, and overlay tools",
                WingetId = null,
                DirectUrl = "https://us.download.nvidia.com/nvapp/client/11.0.8.299/NVIDIA_app_v11.0.8.299.exe",
                FileName = "NVIDIA_app_setup.exe",
            },
        };
    }
}