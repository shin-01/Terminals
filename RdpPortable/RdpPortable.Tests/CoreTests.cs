using System;
using System.IO;
using System.Linq;
using System.Text;
using RdpPortable.Core.Export;
using RdpPortable.Core.Import;
using RdpPortable.Core.Models;
using RdpPortable.Core.Storage;
using Xunit;

namespace RdpPortable.Tests
{
    public class TerminalsXmlImporterTests
    {
        private static string TestFile(string name)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "TestData", name);
            Assert.True(File.Exists(path), "Missing test file: " + path);
            return path;
        }

        [Fact]
        public void Import_ImportsRdpFavoritesAndSkipsOtherProtocols()
        {
            var result = TerminalsXmlImporter.Import(TestFile("terminals-export.xml"));

            Assert.Equal(2, result.Imported.Count);
            Assert.Single(result.Skipped);

            var prod = result.Imported.First(c => c.Name == "Server Prod");
            Assert.Equal("prod01.corp.local", prod.Server);
            Assert.Equal(3389, prod.Port);
            Assert.Equal("administrator", prod.UserName);
            Assert.Equal("CORP", prod.Domain);
            Assert.Equal("secret123", prod.Password);
            Assert.True(prod.ConnectToConsole);
            Assert.Equal(DesktopSizeMode.Size1024X768, prod.DesktopSize);
            Assert.Equal(ColorDepth.Depth32, prod.ColorDepth);
            Assert.True(prod.EnableNlaAuthentication);
            Assert.True(prod.RedirectClipboard);
            Assert.True(prod.RedirectPrinters);
            Assert.True(prod.RedirectDrives);
            Assert.Equal(SoundRedirection.PlayOnRemoteComputer, prod.Sounds);
            Assert.False(prod.AllowWallpaper);
            Assert.True(prod.EnableFontSmoothing);
            Assert.Equal(new[] { "produzione", "client" }, prod.Tags);
            Assert.Equal("Server di produzione", prod.Notes);

            Assert.Equal(GatewayUsageMode.Always, prod.Gateway.UsageMethod);
            Assert.Equal("gw.corp.local", prod.Gateway.HostName);
            Assert.Equal(GatewayCredentialSource.Custom, prod.Gateway.CredentialSource);
            Assert.True(prod.Gateway.SeparateLogin);
            Assert.Equal("gwuser", prod.Gateway.UserName);

            var second = result.Imported.First(c => c.Name == "Second RDP");
            Assert.Equal("vm-test.local", second.Server);
            Assert.Equal(3390, second.Port);
            Assert.Equal(DesktopSizeMode.FitToWindow, second.DesktopSize);
            Assert.Equal(ColorDepth.Depth16, second.ColorDepth);
            Assert.Equal(SoundRedirection.DoNotPlay, second.Sounds);
        }

        [Fact]
        public void Import_ThrowsOnMissingFile()
        {
            Assert.Throws<FileNotFoundException>(() => TerminalsXmlImporter.Import("does-not-exist.xml"));
        }

        [Fact]
        public void Import_SkipsFavoriteWithoutServer()
        {
            string xml = "<favorites><favorite><name>no server</name><protocol>RDP</protocol></favorite></favorites>";
            string path = Path.Combine(Path.GetTempPath(), "no-server-" + Guid.NewGuid() + ".xml");
            File.WriteAllText(path, xml);

            try
            {
                var result = TerminalsXmlImporter.Import(path);
                Assert.Empty(result.Imported);
                Assert.Single(result.Skipped);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }

    public class JsonConnectionStoreTests
    {
        [Fact]
        public void SaveAndLoad_RoundTripsConnections()
        {
            string path = Path.Combine(Path.GetTempPath(), "rdp-test-" + Guid.NewGuid() + ".json");
            var store = new JsonConnectionStore(path);

            var connection = new RdpConnection
            {
                Name = "Test",
                Server = "test.local",
                Port = 3390,
                Tags = { "one", "two" },
                Gateway = new TsGatewaySettings
                {
                    UsageMethod = GatewayUsageMode.Always,
                    HostName = "gw.local",
                    SeparateLogin = true,
                },
            };

            store.Save(new[] { connection });
            Assert.True(File.Exists(path));

            var loaded = store.Load();
            var first = Assert.Single(loaded);
            Assert.Equal(connection.Id, first.Id);
            Assert.Equal("Test", first.Name);
            Assert.Equal(3390, first.Port);
            Assert.Equal(new[] { "one", "two" }, first.Tags);
            Assert.Equal(GatewayUsageMode.Always, first.Gateway.UsageMethod);
            Assert.Equal("gw.local", first.Gateway.HostName);
            Assert.True(first.Gateway.SeparateLogin);

            File.Delete(path);
        }

        [Fact]
        public void Load_MissingFileReturnsEmptyList()
        {
            var store = new JsonConnectionStore(Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid() + ".json"));
            Assert.Empty(store.Load());
        }

        [Fact]
        public void Load_InvalidXmlFallsBackEmpty()
        {
            string path = Path.Combine(Path.GetTempPath(), "invalid-" + Guid.NewGuid() + ".json");
            File.WriteAllText(path, "this is not json");

            try
            {
                var store = new JsonConnectionStore(path);
                Assert.Throws<System.Text.Json.JsonException>(() => store.Load());
            }
            finally
            {
                File.Delete(path);
            }
        }
    }

    public class RdpFileExporterTests
    {
        [Fact]
        public void BuildRdpContent_IncludesKeySettings()
        {
            var c = new RdpConnection
            {
                Name = "Prod",
                Server = "prod.local",
                Port = 3390,
                UserName = "admin",
                Domain = "CORP",
                ConnectToConsole = true,
                ColorDepth = ColorDepth.Depth16,
                DesktopSize = DesktopSizeMode.FullScreen,
                Gateway = new TsGatewaySettings
                {
                    UsageMethod = GatewayUsageMode.Always,
                    HostName = "gw.local",
                    CredentialSource = GatewayCredentialSource.Custom,
                },
                StartProgram = "notepad.exe",
                WorkingFolder = "C:\\Temp",
            };

            string rdp = RdpFileExporter.BuildRdpContent(c);

            Assert.Contains("full address:s:prod.local:3390", rdp);
            Assert.Contains("username:s:admin", rdp);
            Assert.Contains("domain:s:CORP", rdp);
            Assert.Contains("connect to console:i:1", rdp);
            Assert.Contains("session bpp:i:16", rdp);
            Assert.Contains("screen mode id:i:2", rdp);
            Assert.Contains("gatewayhostname:s:gw.local", rdp);
            Assert.Contains("gatewayusagemethod:i:1", rdp);
            Assert.Contains("gatewaycredentialssource:i:2", rdp);
            Assert.Contains("alternate shell:s:notepad.exe", rdp);
            Assert.Contains("shell working directory:s:C:\\Temp", rdp);
        }

        [Fact]
        public void FullAddress_OmitsDefaultPort()
        {
            var c = new RdpConnection { Server = "srv.local", Port = 3389 };
            Assert.Equal("srv.local", RdpFileExporter.FullAddress(c));

            c.Port = 3390;
            Assert.Equal("srv.local:3390", RdpFileExporter.FullAddress(c));
        }

        [Fact]
        public void ExportToFile_WritesUnicodeFile()
        {
            string path = Path.Combine(Path.GetTempPath(), "export-" + Guid.NewGuid() + ".rdp");
            var c = new RdpConnection { Name = "X", Server = "x.local" };

            try
            {
                RdpFileExporter.ExportToFile(c, path);
                Assert.True(File.Exists(path));
                string content = File.ReadAllText(path, Encoding.Unicode);
                Assert.Contains("full address:s:x.local", content);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
