using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using RdpPortable.Core.Models;

namespace RdpPortable.App
{
    public partial class ConnectionEditorWindow : Window
    {
        public RdpConnection Connection { get; }

        public ConnectionEditorWindow(RdpConnection connection)
        {
            this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            InitializeComponent();
            this.LoadConnection();
        }

        private void LoadConnection()
        {
            var c = this.Connection;
            this.NameBox.Text = c.Name;
            this.ServerBox.Text = c.Server;
            this.PortBox.Text = c.Port.ToString(CultureInfo.InvariantCulture);
            this.UserNameBox.Text = c.UserName;
            this.DomainBox.Text = c.Domain;
            this.PasswordBox.Password = c.Password;
            this.NotesBox.Text = c.Notes;

            this.DesktopSizeBox.ItemsSource = Enum.GetValues(typeof(DesktopSizeMode));
            this.DesktopSizeBox.SelectedItem = c.DesktopSize;
            this.WidthBox.Text = c.CustomWidth.ToString(CultureInfo.InvariantCulture);
            this.HeightBox.Text = c.CustomHeight.ToString(CultureInfo.InvariantCulture);

            this.ColorDepthBox.ItemsSource = Enum.GetValues(typeof(ColorDepth));
            this.ColorDepthBox.SelectedItem = c.ColorDepth;

            this.SoundsBox.ItemsSource = Enum.GetValues(typeof(SoundRedirection));
            this.SoundsBox.SelectedItem = c.Sounds;

            this.ConsoleCheckBox.IsChecked = c.ConnectToConsole;
            this.ConnectionBarCheckBox.IsChecked = c.DisplayConnectionBar;
            this.ClipboardCheckBox.IsChecked = c.RedirectClipboard;
            this.DrivesCheckBox.IsChecked = c.RedirectDrives;
            this.PrintersCheckBox.IsChecked = c.RedirectPrinters;
            this.SmartCardsCheckBox.IsChecked = c.RedirectSmartCards;
            this.PortsCheckBox.IsChecked = c.RedirectPorts;
            this.NlaCheckBox.IsChecked = c.EnableNlaAuthentication;

            this.GatewayBox.Text = c.Gateway.HostName;
            this.StartProgramBox.Text = c.StartProgram;
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            var c = this.Connection;

            if (string.IsNullOrWhiteSpace(this.NameBox.Text) || string.IsNullOrWhiteSpace(this.ServerBox.Text))
            {
                MessageBox.Show(this, "Nome e server sono obbligatori.", "RDP Portable",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            c.Name = this.NameBox.Text.Trim();
            c.Server = this.ServerBox.Text.Trim();
            c.Port = ParseInt(this.PortBox.Text, 3389);
            c.UserName = this.UserNameBox.Text.Trim();
            c.Domain = this.DomainBox.Text.Trim();
            c.Password = this.PasswordBox.Password;
            c.Notes = this.NotesBox.Text;

            c.DesktopSize = (DesktopSizeMode)this.DesktopSizeBox.SelectedItem!;
            c.CustomWidth = ParseInt(this.WidthBox.Text, 1280);
            c.CustomHeight = ParseInt(this.HeightBox.Text, 1024);
            c.ColorDepth = (ColorDepth)this.ColorDepthBox.SelectedItem!;
            c.Sounds = (SoundRedirection)this.SoundsBox.SelectedItem!;

            c.ConnectToConsole = this.ConsoleCheckBox.IsChecked == true;
            c.DisplayConnectionBar = this.ConnectionBarCheckBox.IsChecked == true;
            c.RedirectClipboard = this.ClipboardCheckBox.IsChecked == true;
            c.RedirectDrives = this.DrivesCheckBox.IsChecked == true;
            c.RedirectPrinters = this.PrintersCheckBox.IsChecked == true;
            c.RedirectSmartCards = this.SmartCardsCheckBox.IsChecked == true;
            c.RedirectPorts = this.PortsCheckBox.IsChecked == true;
            c.EnableNlaAuthentication = this.NlaCheckBox.IsChecked == true;

            var gatewayHostName = this.GatewayBox.Text.Trim();
            c.Gateway.HostName = gatewayHostName;
            c.Gateway.UsageMethod = string.IsNullOrEmpty(gatewayHostName)
                ? GatewayUsageMode.Never
                : GatewayUsageMode.Always;

            c.StartProgram = this.StartProgramBox.Text.Trim();

            this.DialogResult = true;
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private static int ParseInt(string text, int fallback)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                ? value
                : fallback;
        }
    }
}
