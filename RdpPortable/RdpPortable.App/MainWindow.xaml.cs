using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using RdpPortable.Core.Models;
using RdpPortable.App.ViewModels;

namespace RdpPortable.App
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel viewModel;
        private readonly RdpClientHost clientHost = new RdpClientHost();

        public MainWindow(MainViewModel viewModel)
        {
            this.viewModel = viewModel;
            InitializeComponent();
            this.ConnectionList.ItemsSource = this.viewModel.Connections;
            this.ConnectionList.Items.SortDescriptions.Add(
                new SortDescription(nameof(RdpConnection.Name), ListSortDirection.Ascending));
            this.ConnectionList.Items.IsLiveSorting = true;

            this.clientHost.Initialize();
            this.RdpHost.Child = this.clientHost.Control;
            this.clientHost.Disconnected += msg => Dispatcher.Invoke(() => this.StatusBarItem());
        }

        private void StatusBarItem()
        {
            this.ConnectButton.IsEnabled = this.ConnectionList.SelectedItem is RdpConnection;
        }

        private RdpConnection? Selected => this.ConnectionList.SelectedItem as RdpConnection;

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.StatusBarItem();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var view = (System.Windows.Data.CollectionView)
                System.ComponentModel.CollectionViewSource.GetDefaultView(this.ConnectionList.ItemsSource);
            string filter = this.SearchBox.Text.Trim();
            view.Filter = string.IsNullOrEmpty(filter)
                ? null
                : (object o => FilterConnection((RdpConnection)o, filter));
        }

        private static bool FilterConnection(RdpConnection c, string filter)
        {
            return c.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                || c.Server.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                || c.Tags.Any(t => t.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void OnConnectDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.ConnectSelected();
        }

        private void OnConnect(object sender, RoutedEventArgs e)
        {
            this.ConnectSelected();
        }

        private void ConnectSelected()
        {
            var connection = this.Selected;
            if (connection == null)
                return;

            try
            {
                this.clientHost.Connect(connection);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Connessione fallita: " + ex.Message, "RDP Portable",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnDisconnect(object sender, RoutedEventArgs e)
        {
            this.clientHost.Disconnect();
        }

        private void OnNew(object sender, RoutedEventArgs e)
        {
            this.OpenEditor(new RdpConnection());
        }

        private void OnEdit(object sender, RoutedEventArgs e)
        {
            var connection = this.Selected;
            if (connection != null)
                this.OpenEditor(Clone(connection));
        }

        private static RdpConnection Clone(RdpConnection c)
        {
            return new RdpConnection
            {
                Id = c.Id,
                Name = c.Name,
                Server = c.Server,
                Port = c.Port,
                UserName = c.UserName,
                Domain = c.Domain,
                Password = c.Password,
                Tags = new System.Collections.Generic.List<string>(c.Tags),
                Notes = c.Notes,
                ConnectToConsole = c.ConnectToConsole,
                DesktopSize = c.DesktopSize,
                CustomWidth = c.CustomWidth,
                CustomHeight = c.CustomHeight,
                ColorDepth = c.ColorDepth,
                AllowFullWindowDrag = c.AllowFullWindowDrag,
                AllowMenuAnimations = c.AllowMenuAnimations,
                AllowTheming = c.AllowTheming,
                AllowWallpaper = c.AllowWallpaper,
                EnableFontSmoothing = c.EnableFontSmoothing,
                EnableDesktopComposition = c.EnableDesktopComposition,
                RedirectClipboard = c.RedirectClipboard,
                RedirectDrives = c.RedirectDrives,
                RedirectDrivesList = c.RedirectDrivesList,
                RedirectPrinters = c.RedirectPrinters,
                RedirectPorts = c.RedirectPorts,
                RedirectSmartCards = c.RedirectSmartCards,
                RedirectDevices = c.RedirectDevices,
                Sounds = c.Sounds,
                DisplayConnectionBar = c.DisplayConnectionBar,
                EnableTlsAuthentication = c.EnableTlsAuthentication,
                EnableNlaAuthentication = c.EnableNlaAuthentication,
                Gateway = new TsGatewaySettings
                {
                    UsageMethod = c.Gateway.UsageMethod,
                    HostName = c.Gateway.HostName,
                    CredentialSource = c.Gateway.CredentialSource,
                    SeparateLogin = c.Gateway.SeparateLogin,
                    UserName = c.Gateway.UserName,
                    Domain = c.Gateway.Domain,
                    Password = c.Gateway.Password,
                },
                StartProgram = c.StartProgram,
                WorkingFolder = c.WorkingFolder,
            };
        }

        private void OpenEditor(RdpConnection connection)
        {
            var editor = new ConnectionEditorWindow(connection)
            {
                Owner = this,
            };
            if (editor.ShowDialog() == true)
            {
                this.viewModel.AddOrReplace(editor.Connection);
            }
        }

        private void OnDelete(object sender, RoutedEventArgs e)
        {
            var connection = this.Selected;
            if (connection == null)
                return;

            var result = MessageBox.Show(this,
                "Eliminare la connessione \"" + connection.Name + "\"?",
                "RDP Portable", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
                this.viewModel.Delete(connection);
        }

        private void OnListKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                this.OnDelete(sender, e);
        }

        private void OnImport(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Importa export XML di Terminals",
                Filter = "File XML (*.xml)|*.xml|Tutti i file (*.*)|*.*",
            };
            if (dialog.ShowDialog(this) != true)
                return;

            try
            {
                var result = this.viewModel.ImportFromTerminals(dialog.FileName);
                int importedCount = result?.Imported.Count ?? 0;
                int skippedCount = result?.Skipped.Count ?? 0;
                MessageBox.Show(this,
                    "Importate " + importedCount + " connessioni RDP.\n" +
                    "Ignorate " + skippedCount + " voci non RDP o senza server.",
                    "RDP Portable", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Import fallito: " + ex.Message, "RDP Portable",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnExportRdp(object sender, RoutedEventArgs e)
        {
            var connection = this.Selected;
            if (connection == null)
            {
                MessageBox.Show(this, "Selezionare una connessione.", "RDP Portable",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Esporta file .rdp",
                Filter = "File RDP (*.rdp)|*.rdp",
                FileName = connection.Name + ".rdp",
            };
            if (dialog.ShowDialog(this) == true)
            {
                try
                {
                    this.viewModel.ExportRdpFile(connection, dialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Export fallito: " + ex.Message, "RDP Portable",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            this.clientHost.Dispose();
            base.OnClosed(e);
        }
    }
}
