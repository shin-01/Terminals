using System.Globalization;
using System.Text;
using RdpPortable.Core.Models;

namespace RdpPortable.Core.Export
{
    /// <summary>
    /// Generates a standard .rdp file for a connection, usable with mstsc.exe.
    /// </summary>
    public static class RdpFileExporter
    {
        public static string BuildRdpContent(RdpConnection c)
        {
            var sb = new StringBuilder();
            sb.AppendLine("screen mode id:i:" + (c.DesktopSize == DesktopSizeMode.FullScreen ? "2" : "1"));
            sb.AppendLine("desktopwidth:i:" + ResolveWidth(c).ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("desktopheight:i:" + ResolveHeight(c).ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("session bpp:i:" + ((int)c.ColorDepth).ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("full address:s:" + FullAddress(c));
            sb.AppendLine("username:s:" + c.UserName);
            if (!string.IsNullOrEmpty(c.Domain))
                sb.AppendLine("domain:s:" + c.Domain);
            sb.AppendLine("connect to console:i:" + (c.ConnectToConsole ? "1" : "0"));
            sb.AppendLine("administrative session:i:" + (c.ConnectToConsole ? "1" : "0"));
            sb.AppendLine("allow font smoothing:i:" + (c.EnableFontSmoothing ? "1" : "0"));
            sb.AppendLine("allow desktop composition:i:" + (c.EnableDesktopComposition ? "1" : "0"));
            sb.AppendLine("disable wallpaper:i:" + (c.AllowWallpaper ? "0" : "1"));
            sb.AppendLine("disable full window drag:i:" + (c.AllowFullWindowDrag ? "0" : "1"));
            sb.AppendLine("disable menu anims:i:" + (c.AllowMenuAnimations ? "0" : "1"));
            sb.AppendLine("disable themes:i:" + (c.AllowTheming ? "0" : "1"));
            sb.AppendLine("redirectclipboard:i:" + (c.RedirectClipboard ? "1" : "0"));
            sb.AppendLine("drivestoredirect:s:" + (string.IsNullOrEmpty(c.RedirectDrivesList) ? "*" : c.RedirectDrivesList));
            sb.AppendLine("redirectprinters:i:" + (c.RedirectPrinters ? "1" : "0"));
            sb.AppendLine("redirectcomports:i:" + (c.RedirectPorts ? "1" : "0"));
            sb.AppendLine("redirectsmartcards:i:" + (c.RedirectSmartCards ? "1" : "0"));
            sb.AppendLine("redirectposdevices:i:" + (c.RedirectDevices ? "1" : "0"));
            sb.AppendLine("audiomode:i:" + ((int)c.Sounds).ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("connection type:i:2");
            sb.AppendLine("authentication level:i:2");
            sb.AppendLine("enablecredsspsupport:i:" + (c.EnableNlaAuthentication ? "1" : "0"));
            sb.AppendLine("displayconnectionbar:i:" + (c.DisplayConnectionBar ? "1" : "0"));

            if (c.Gateway.UsageMethod != GatewayUsageMode.Never)
            {
                sb.AppendLine("gatewayusagemethod:i:" + ((int)c.Gateway.UsageMethod).ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("gatewayhostname:s:" + c.Gateway.HostName);
                sb.AppendLine("gatewaycredentialssource:i:" + ((int)c.Gateway.CredentialSource).ToString(CultureInfo.InvariantCulture));
                if (c.Gateway.SeparateLogin && !string.IsNullOrEmpty(c.Gateway.UserName))
                {
                    sb.AppendLine("gatewayusername:s:" + c.Gateway.UserName);
                    if (!string.IsNullOrEmpty(c.Gateway.Domain))
                        sb.AppendLine("gatewaydomain:s:" + c.Gateway.Domain);
                }
            }

            if (!string.IsNullOrEmpty(c.StartProgram))
                sb.AppendLine("alternate shell:s:" + c.StartProgram);
            if (!string.IsNullOrEmpty(c.WorkingFolder))
                sb.AppendLine("shell working directory:s:" + c.WorkingFolder);

            return sb.ToString();
        }

        public static string ExportToFile(RdpConnection connection, string fileName)
        {
            string content = BuildRdpContent(connection);
            System.IO.File.WriteAllText(fileName, content, Encoding.Unicode);
            return fileName;
        }

        public static string FullAddress(RdpConnection c)
        {
            return c.Port == 3389 ? c.Server : c.Server + ":" + c.Port.ToString(CultureInfo.InvariantCulture);
        }

        public static int ResolveWidth(RdpConnection c)
        {
            return c.DesktopSize switch
            {
                DesktopSizeMode.Size640X480 => 640,
                DesktopSizeMode.Size800X600 => 800,
                DesktopSizeMode.Size1024X768 => 1024,
                DesktopSizeMode.Size1152X864 => 1152,
                DesktopSizeMode.Size1280X1024 => 1280,
                _ => c.CustomWidth,
            };
        }

        public static int ResolveHeight(RdpConnection c)
        {
            return c.DesktopSize switch
            {
                DesktopSizeMode.Size640X480 => 480,
                DesktopSizeMode.Size800X600 => 600,
                DesktopSizeMode.Size1024X768 => 768,
                DesktopSizeMode.Size1152X864 => 864,
                DesktopSizeMode.Size1280X1024 => 1024,
                _ => c.CustomHeight,
            };
        }
    }
}
