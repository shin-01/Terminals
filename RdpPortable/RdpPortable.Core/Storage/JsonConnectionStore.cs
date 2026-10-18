using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using RdpPortable.Core.Models;

namespace RdpPortable.Core.Storage
{
    public class JsonConnectionStore
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private readonly string filePath;

        public JsonConnectionStore(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            this.filePath = filePath;
        }

        public string FilePath => this.filePath;

        public List<RdpConnection> Load()
        {
            if (!File.Exists(this.filePath))
                return new List<RdpConnection>();

            string json = File.ReadAllText(this.filePath);
            if (string.IsNullOrWhiteSpace(json))
                return new List<RdpConnection>();

            return JsonSerializer.Deserialize<List<RdpConnection>>(json, Options) ?? new List<RdpConnection>();
        }

        public void Save(IEnumerable<RdpConnection> connections)
        {
            string directory = Path.GetDirectoryName(Path.GetFullPath(this.filePath));
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            string json = JsonSerializer.Serialize(connections, Options);
            string tmp = this.filePath + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(this.filePath))
                File.Replace(tmp, this.filePath, null);
            else
                File.Move(tmp, this.filePath);
        }
    }
}
