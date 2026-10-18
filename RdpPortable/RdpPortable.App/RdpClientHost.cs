using System;
using System.Drawing;
using System.Windows.Forms;
using AxMSTSCLib;
using MSTSCLib;
using RdpPortable.Core.Models;

namespace RdpPortable.App
{
    /// <summary>
    /// Hosts the MSTSC ActiveX control (mstscax.dll) and applies all
    /// connection settings, including RD Gateway, redirects and NLA.
    /// </summary>
    public class RdpClientHost : IDisposable
    {
        private AxMsRdpClient6NotSafeForScripting? client;

        public Control Control => this.client ?? throw new InvalidOperationException("Client not initialized.");

        public event Action<string>? Disconnected;

        public void Initialize()
        {
            if (this.client != null)
                return;

            this.client = new AxMsRdpClient6NotSafeForScripting
            {
                Dock = DockStyle.Fill,
                Enabled = true,
            };
            this.client.OnDisconnected += this.OnDisconnected;
        }

        public void Connect(RdpConnection c)
        {
            this.Initialize();

            var client = this.client!;
            client.CreateControl();

            client.Server = c.Port == 3389 ? c.Server : c.Server + ":" + c.Port;
            client.UserName = c.UserName;
            client.Domain = c.Domain;

            if (!string.IsNullOrEmpty(c.Password))
                client.AdvancedSettings2.ClearTextPassword = c.Password;

            client.AdvancedSettings2.DisplayConnectionBar = c.DisplayConnectionBar;
            client.AdvancedSettings2.AllowBackgroundInput = -1;
            client.AdvancedSettings2.BitmapPeristence = -1;
            client.AdvancedSettings2.Compress = -1;
            client.AdvancedSettings2.DisableCtrlAltDel = -1;
            client.AdvancedSettings2.EnableWindowsKey = -1;
            client.AdvancedSettings2.EncryptionEnabled = -1;
            client.AdvancedSettings2.GrabFocusOnConnect = true;
            client.AdvancedSettings3.ConnectToServerConsole = c.ConnectToConsole;
            client.AdvancedSettings3.SmartSizing = c.DesktopSize != DesktopSizeMode.Custom;
            client.AdvancedSettings6.RedirectClipboard = c.RedirectClipboard;
            client.AdvancedSettings6.RedirectDevices = c.RedirectDevices;
            client.AdvancedSettings6.ConnectionBarShowMinimizeButton = false;
            client.AdvancedSettings6.ConnectionBarShowPinButton = false;
            client.AdvancedSettings6.ConnectionBarShowRestoreButton = false;

            client.ColorDepth = (int)c.ColorDepth;

            bool fullscreen = c.DesktopSize == DesktopSizeMode.FullScreen;
            var size = ResolveSize(c);
            if (fullscreen)
                client.FullScreen = true;
            else
            {
                client.DesktopWidth = size.Width;
                client.DesktopHeight = size.Height;
            }

            ApplyPerformanceFlags(client, c);
            ApplyGateway(client, c);
            ApplyProgram(client, c);

            var nonScriptable = (IMsRdpClientNonScriptable4?)client.GetOcx();
            if (nonScriptable != null)
            {
                nonScriptable.EnableCredSspSupport = c.EnableNlaAuthentication;
            }

            client.Connect();
        }

        private static void ApplyPerformanceFlags(AxMsRdpClient6NotSafeForScripting client, RdpConnection c)
        {
            int flags = 0;
            if (!c.AllowWallpaper) flags |= 0x01;
            if (!c.AllowFullWindowDrag) flags |= 0x02;
            if (!c.AllowMenuAnimations) flags |= 0x04;
            if (!c.AllowTheming) flags |= 0x08;
            if (c.EnableFontSmoothing) flags |= 0x40;
            if (c.EnableDesktopComposition) flags |= 0x100;
            client.AdvancedSettings2.PerformanceFlags = flags;
        }

        private static void ApplyGateway(AxMsRdpClient6NotSafeForScripting client, RdpConnection c)
        {
            var gw = c.Gateway;
            client.TransportSettings.GatewayUsageMethod = (uint)gw.UsageMethod;
            client.TransportSettings.GatewayHostname = gw.HostName;
            client.TransportSettings.GatewayCredsSource = (uint)gw.CredentialSource;
            if (gw.SeparateLogin)
            {
                client.TransportSettings2.GatewayDomain = gw.Domain;
                client.TransportSettings2.GatewayUsername = gw.UserName;
                if (!string.IsNullOrEmpty(gw.Password))
                    client.TransportSettings2.GatewayPassword = gw.Password;
            }
        }

        private static void ApplyProgram(AxMsRdpClient6NotSafeForScripting client, RdpConnection c)
        {
            if (!string.IsNullOrEmpty(c.StartProgram))
            {
                client.AdvancedSettings2.StartProgram = c.StartProgram;
                if (!string.IsNullOrEmpty(c.WorkingFolder))
                    client.AdvancedSettings2.WorkDir = c.WorkingFolder;
            }
        }

        private void OnDisconnected(object sender, AxMSTSCLib.IMsTscAxEvents_OnDisconnectedEvent e)
        {
            this.Disconnected?.Invoke("Disconnected (code " + e.discReason + ")");
        }

        public void Disconnect()
        {
            if (this.client != null && this.client.Connected == 1)
                this.client.Disconnect();
        }

        private static System.Drawing.Size ResolveSize(RdpConnection c)
        {
            if (c.DesktopSize == DesktopSizeMode.FitToWindow || c.DesktopSize == DesktopSizeMode.AutoScale)
                return new Size(SystemInformation.WorkingArea.Width - 40, SystemInformation.WorkingArea.Height - 80);

            return new Size(
                RdpPortable.Core.Export.RdpFileExporter.ResolveWidth(c),
                RdpPortable.Core.Export.RdpFileExporter.ResolveHeight(c));
        }

        public void Dispose()
        {
            if (this.client != null)
            {
                try
                {
                    if (this.client.Connected == 1)
                        this.client.Disconnect();
                }
                catch
                {
                    // control may already be disposed
                }

                this.client.Dispose();
                this.client = null!;
            }
        }
    }
}
