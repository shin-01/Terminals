using System;
using System.Collections.Generic;

namespace RdpPortable.Core.Models
{
    public class RdpConnection
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        public string Server { get; set; } = string.Empty;

        public int Port { get; set; } = 3389;

        public string UserName { get; set; } = string.Empty;

        public string Domain { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public List<string> Tags { get; set; } = new List<string>();

        public string Notes { get; set; } = string.Empty;

        public bool ConnectToConsole { get; set; }

        public DesktopSizeMode DesktopSize { get; set; } = DesktopSizeMode.FitToWindow;

        public int CustomWidth { get; set; } = 1280;

        public int CustomHeight { get; set; } = 1024;

        public ColorDepth ColorDepth { get; set; } = ColorDepth.Depth32;

        public bool AllowFullWindowDrag { get; set; } = true;

        public bool AllowMenuAnimations { get; set; } = true;

        public bool AllowTheming { get; set; } = true;

        public bool AllowWallpaper { get; set; } = true;

        public bool EnableFontSmoothing { get; set; } = true;

        public bool EnableDesktopComposition { get; set; } = true;

        public bool RedirectClipboard { get; set; } = true;

        public bool RedirectDrives { get; set; }

        public string? RedirectDrivesList { get; set; }

        public bool RedirectPrinters { get; set; }

        public bool RedirectPorts { get; set; }

        public bool RedirectSmartCards { get; set; }

        public bool RedirectDevices { get; set; }

        public SoundRedirection Sounds { get; set; } = SoundRedirection.PlayOnThisComputer;

        public bool DisplayConnectionBar { get; set; } = true;

        public bool EnableTlsAuthentication { get; set; } = true;

        public bool EnableNlaAuthentication { get; set; } = true;

        public TsGatewaySettings Gateway { get; set; } = new TsGatewaySettings();

        public string StartProgram { get; set; } = string.Empty;

        public string WorkingFolder { get; set; } = string.Empty;

        public override string ToString()
        {
            return this.Name + " (" + this.Server + ")";
        }
    }

    public enum DesktopSizeMode
    {
        Size640X480 = 0,
        Size800X600 = 1,
        Size1024X768 = 2,
        Size1152X864 = 3,
        Size1280X1024 = 4,
        FitToWindow = 5,
        FullScreen = 6,
        AutoScale = 7,
        Custom = 8,
    }

    public enum ColorDepth
    {
        Depth8 = 8,
        Depth16 = 16,
        Depth24 = 24,
        Depth32 = 32,
    }

    public enum SoundRedirection
    {
        PlayOnThisComputer = 0,
        PlayOnRemoteComputer = 1,
        DoNotPlay = 2,
    }
}
