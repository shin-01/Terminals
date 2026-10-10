// PoC: RDP ActiveX hosting on .NET 8 WinForms via Devolutions.MsRdpEx.
// Go/no-go validation for the Terminals .NET 8 migration: the NuGet package
// ships a legacy AxInterop.MSTSCLib compiled for net8.0-windows, which makes
// the classic AxHost-based control usable on the modern runtime.
//
// MsRdpExComInterop=Legacy is the package default; the build targets add
// Interop.MSTSCLib + AxInterop.MSTSCLib references automatically.
using System;
using System.Drawing;
using System.Windows.Forms;

using MSTSCLib;
using AxMSTSCLib;

namespace RdpNet8
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(args));
        }
    }

    internal class MainForm : Form
    {
        private readonly TextBox serverBox = new TextBox();
        private readonly TextBox userBox = new TextBox();
        private readonly TextBox passwordBox = new TextBox();
        private readonly Button connectButton = new Button();
        private readonly StatusStrip statusStrip = new StatusStrip();
        private readonly ToolStripStatusLabel statusLabel = new ToolStripStatusLabel();

        private AxMsRdpClient9NotSafeForScripting rdp;

        public MainForm(string[] args)
        {
            Text = "RDP on .NET 8 - PoC (MsRdpEx legacy interop)";
            ClientSize = new Size(1024, 768);

            serverBox.Text = args.Length > 0 ? args[0] : "localhost";
            serverBox.Dock = DockStyle.Top;
            userBox.Text = Environment.GetEnvironmentVariable("RDP_USERNAME") ?? string.Empty;
            userBox.Dock = DockStyle.Top;
            passwordBox.UseSystemPasswordChar = true;
            passwordBox.Text = Environment.GetEnvironmentVariable("RDP_PASSWORD") ?? string.Empty;
            passwordBox.Dock = DockStyle.Top;

            connectButton.Text = "Connect";
            connectButton.Dock = DockStyle.Top;
            connectButton.Click += OnConnect;

            statusLabel.Text = "Ready - net8.0-windows + MsRdpEx legacy AxInterop";
            statusStrip.Items.Add(statusLabel);
            statusStrip.Dock = DockStyle.Bottom;

            Controls.Add(serverBox);
            Controls.Add(userBox);
            Controls.Add(passwordBox);
            Controls.Add(connectButton);
            Controls.Add(statusStrip);
        }

        private void OnConnect(object sender, EventArgs e)
        {
            if (rdp != null)
            {
                // single session per PoC run: replace the control
                rdp.Dispose();
                rdp = null;
            }

            rdp = new AxMsRdpClient9NotSafeForScripting();
            ((System.ComponentModel.ISupportInitialize)rdp).BeginInit();
            Controls.Add(rdp);
            rdp.Dock = DockStyle.Fill;
            ((System.ComponentModel.ISupportInitialize)rdp).EndInit();

            rdp.Server = serverBox.Text;
            rdp.UserName = userBox.Text;

            IMsRdpClientAdvancedSettings8 advancedSettings = rdp.AdvancedSettings9;
            advancedSettings.EnableCredSspSupport = true;

            IMsTscNonScriptable secured = (IMsTscNonScriptable)rdp.GetOcx();
            secured.ClearTextPassword = passwordBox.Text;

            rdp.DesktopWidth = ClientSize.Width;
            rdp.DesktopHeight = ClientSize.Height - statusStrip.Height;

            rdp.OnConnected += (s, ev) =>
                BeginInvoke((Action)(() => statusLabel.Text = "Connected"));
            rdp.OnDisconnected += (s, ev) =>
                BeginInvoke((Action)(() => statusLabel.Text =
                    string.Format("Disconnected (reason {0})", ((IMsTscAxEvents_OnDisconnectedEvent)ev).discReason)));

            try
            {
                rdp.Connect();
                statusLabel.Text = "Connecting...";
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Connect failed: " + ex.Message;
            }
        }
    }
}
