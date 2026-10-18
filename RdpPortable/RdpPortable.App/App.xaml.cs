using System;
using System.IO;
using System.Windows;
using RdpPortable.App.ViewModels;

namespace RdpPortable.App
{
    public partial class App : Application
    {
        public static string DataFolder { get; } = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Data");

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var store = new Core.Storage.JsonConnectionStore(Path.Combine(DataFolder, "connections.json"));
            var window = new MainWindow(new MainViewModel(store));
            window.Show();
        }
    }
}
