// PoC: RDP ActiveX hosting on .NET 8 WinForms via Devolutions.MsRdpEx.
// Go/no-go validation for the Terminals .NET 8 migration: create, host,
// connect, receive events and dispose the mstscax ActiveX control.
//
// Modelled after MsRdpEx_App/MainDlg.cs (Devolutions upstream sample).
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using MSTSCLib;
using MsRdpEx;
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
        private readonly Button closeButton = new Button();
        private readonly StatusStrip statusStrip = new StatusStrip();
        private readonly ToolStripStatusLabel statusLabel = new ToolStripStatusLabel();

        public MainForm(string[] args)
        {
            Text = "RDP on .NET 8 - PoC (MsRdpEx)";
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

            closeButton.Text = "Close";
            closeButton.Dock = DockStyle.Top;
            closeButton.Click += (s, e) => Close();

            statusLabel.Text = "Ready - net8.0-windows + Devolutions.MsRdpEx";
            statusStrip.Items.Add(statusLabel);
            statusStrip.Dock = DockStyle.Bottom;

            Controls.Add(serverBox);
            Controls.Add(userBox);
            Controls.Add(passwordBox);
            Controls.Add(connectButton);
            Controls.Add(closeButton);
            Controls.Add(statusStrip);
        }

        private void OnConnect(object sender, EventArgs e)
        {
            MsRdpExManager manager = MsRdpExManager.Instance;
            RdpCoreApi coreApi = manager.CoreApi;
            bool axHookEnabled = manager.AxHookEnabled;
            string rdpExDll = coreApi.MsRdpExDllPath;

            RdpView rdpView;

            if (axHookEnabled)
            {
                rdpView = new RdpView("mstscax", rdpExDll);
            }
            else
            {
                rdpView = new RdpView("mstscax", null);
            }

            AxMsRdpClient9NotSafeForScripting rdp = rdpView.rdpClient;

            rdp.Server = serverBox.Text;
            rdp.UserName = userBox.Text;

            IMsRdpClientAdvancedSettings8 advancedSettings = rdp.AdvancedSettings9;
            advancedSettings.EnableCredSspSupport = true;

            IMsTscNonScriptable secured = (IMsTscNonScriptable)rdp.GetOcx();
            secured.ClearTextPassword = passwordBox.Text;

            rdp.DesktopWidth = rdpView.ClientSize.Width;
            rdp.DesktopHeight = rdpView.ClientSize.Height;

            rdp.OnConnected += (s, ev) =>
                BeginInvoke((Action)(() => statusLabel.Text = "Connected"));
            rdp.OnDisconnected += (s, ev) =>
                BeginInvoke((Action)(() => statusLabel.Text = string.Format("Disconnected (reason {0})", ev.discReason)));

            rdpView.Text = string.Format("{0} - Terminals PoC", rdp.Server);
            rdp.Connect();
            rdpView.Show(this);
        }
    }
}
