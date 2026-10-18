using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using RdpPortable.Core.Import;
using RdpPortable.Core.Models;
using RdpPortable.Core.Storage;

namespace RdpPortable.App.ViewModels
{
    public class MainViewModel
    {
        private readonly JsonConnectionStore store;

        public ObservableCollection<RdpConnection> Connections { get; } = new ObservableCollection<RdpConnection>();

        public MainViewModel(JsonConnectionStore store)
        {
            this.store = store;
            foreach (var connection in this.store.Load().OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
                this.Connections.Add(connection);
        }

        public void Save()
        {
            this.store.Save(this.Connections);
        }

        public void AddOrReplace(RdpConnection connection)
        {
            var existing = this.Connections.FirstOrDefault(c => c.Id == connection.Id);
            if (existing != null)
            {
                int index = this.Connections.IndexOf(existing);
                this.Connections[index] = connection;
            }
            else
            {
                this.Connections.Add(connection);
            }

            this.Save();
        }

        public void Delete(RdpConnection connection)
        {
            this.Connections.Remove(connection);
            this.Save();
        }

        public ImportResult? ImportFromTerminals(string fileName)
        {
            var result = TerminalsXmlImporter.Import(fileName);
            foreach (var connection in result.Imported)
            {
                connection.Id = Guid.NewGuid();
                this.Connections.Add(connection);
            }

            this.Save();
            return result;
        }

        public void ExportRdpFile(RdpConnection connection, string fileName)
        {
            Core.Export.RdpFileExporter.ExportToFile(connection, fileName);
        }
    }
}
