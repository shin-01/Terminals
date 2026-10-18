using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using RdpPortable.Core.Models;

namespace RdpPortable.Core.Import
{
    /// <summary>
    /// Imports connection favorites from a legacy Terminals XML export
    /// (the format produced by Terminals ExportImport.ExportXML).
    /// Only RDP favorites are imported; other protocols are skipped.
    /// </summary>
    public static class TerminalsXmlImporter
    {
        private const string RdpProtocol = "RDP";

        public static ImportResult Import(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));
            if (!File.Exists(fileName))
                throw new FileNotFoundException("Terminals export file not found.", fileName);

            var imported = new List<RdpConnection>();
            var skipped = new List<string>();

            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, IgnoreComments = true };
            using (XmlReader reader = XmlReader.Create(fileName, settings))
            {
                RdpConnection? current = null;

                while (reader.Read())
                {
                    if (reader.NodeType != XmlNodeType.Element)
                        continue;

                    if (reader.Name == "favorite")
                    {
                        if (current != null)
                            FinalizeFavorite(current, imported, skipped);

                        current = new RdpConnection();
                        continue;
                    }

                    if (current == null)
                        continue;

                    string value = reader.ReadElementContentAsString();
                    ApplyProperty(current, reader.Name, value);
                }

                if (current != null)
                    FinalizeFavorite(current, imported, skipped);
            }

            return new ImportResult(imported, skipped);
        }

        private static void FinalizeFavorite(RdpConnection favorite, List<RdpConnection> imported, List<string> skipped)
        {
            if (string.IsNullOrEmpty(favorite.Server))
            {
                skipped.Add(favorite.Name + " (no server)");
                return;
            }

            imported.Add(favorite);
        }

        private static void ApplyProperty(RdpConnection favorite, string name, string value)
        {
            switch (name)
            {
                case "name":
                    favorite.Name = value;
                    break;
                case "serverName":
                    favorite.Server = value;
                    break;
                case "port":
                    favorite.Port = ReadInt(value, 3389);
                    break;
                case "userName":
                    favorite.UserName = value;
                    break;
                case "domainName":
                    favorite.Domain = value;
                    break;
                case "password":
                    favorite.Password = value;
                    break;
                case "notes":
                    favorite.Notes = value;
                    break;
                case "tags":
                    favorite.Tags = ParseTags(value);
                    break;
                case "protocol":
                    if (!string.Equals(value, RdpProtocol, StringComparison.OrdinalIgnoreCase))
                        favorite.Server = string.Empty; // marks the favorite as non-RDP -> skipped
                    break;
                case "connectToConsole":
                    favorite.ConnectToConsole = ReadBool(value);
                    break;
                case "desktopSize":
                    favorite.DesktopSize = ParseDesktopSize(value);
                    break;
                case "desktopSizeWidth":
                    favorite.CustomWidth = ReadInt(value, 1280);
                    break;
                case "desktopSizeHeight":
                    favorite.CustomHeight = ReadInt(value, 1024);
                    break;
                case "colors":
                    favorite.ColorDepth = ParseColors(value);
                    break;
                case "disableFullWindowDrag":
                    favorite.AllowFullWindowDrag = !ReadBool(value);
                    break;
                case "disableMenuAnimations":
                    favorite.AllowMenuAnimations = !ReadBool(value);
                    break;
                case "disableTheming":
                    favorite.AllowTheming = !ReadBool(value);
                    break;
                case "disableWallPaper":
                    favorite.AllowWallpaper = !ReadBool(value);
                    break;
                case "enableFontSmoothing":
                    favorite.EnableFontSmoothing = ReadBool(value);
                    break;
                case "enableDesktopComposition":
                    favorite.EnableDesktopComposition = ReadBool(value);
                    break;
                case "redirectClipboard":
                    favorite.RedirectClipboard = ReadBool(value);
                    break;
                case "redirectedDrives":
                    if (!string.IsNullOrEmpty(value))
                    {
                        favorite.RedirectDrives = true;
                        favorite.RedirectDrivesList = value;
                    }
                    break;
                case "redirectPrinters":
                    favorite.RedirectPrinters = ReadBool(value);
                    break;
                case "redirectPorts":
                    favorite.RedirectPorts = ReadBool(value);
                    break;
                case "redirectSmartCards":
                    favorite.RedirectSmartCards = ReadBool(value);
                    break;
                case "redirectDevices":
                    favorite.RedirectDevices = ReadBool(value);
                    break;
                case "sounds":
                    favorite.Sounds = ParseSounds(value);
                    break;
                case "displayConnectionBar":
                    favorite.DisplayConnectionBar = ReadBool(value);
                    break;
                case "enableTLSAuthentication":
                    favorite.EnableTlsAuthentication = ReadBool(value);
                    break;
                case "enableNLAAuthentication":
                    favorite.EnableNlaAuthentication = ReadBool(value);
                    break;
                case "securityStartProgram":
                    favorite.StartProgram = value;
                    break;
                case "securityWorkingFolder":
                    favorite.WorkingFolder = value;
                    break;
                case "tsgwUsageMethod":
                    favorite.Gateway.UsageMethod = ParseGatewayUsage(value);
                    break;
                case "tsgwHostname":
                    favorite.Gateway.HostName = value;
                    break;
                case "tsgwCredsSource":
                    favorite.Gateway.CredentialSource = ParseGatewayCredsSource(value);
                    break;
                case "tsgwSeparateLogin":
                    favorite.Gateway.SeparateLogin = ReadBool(value);
                    break;
                case "tsgwUsername":
                    favorite.Gateway.UserName = value;
                    break;
                case "tsgwDomain":
                    favorite.Gateway.Domain = value;
                    break;
                case "tsgwPassword":
                    favorite.Gateway.Password = value;
                    break;
            }
        }

        private static List<string> ParseTags(string value)
        {
            var tags = new List<string>();
            if (string.IsNullOrEmpty(value))
                return tags;

            foreach (string tag in value.Split(','))
            {
                string trimmed = tag.Trim();
                if (trimmed.Length > 0 && !tags.Contains(trimmed))
                    tags.Add(trimmed);
            }

            return tags;
        }

        private static bool ReadBool(string value)
        {
            return bool.TryParse(value, out bool result) && result;
        }

        private static int ReadInt(string value, int fallback)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)
                ? result
                : fallback;
        }

        private static DesktopSizeMode ParseDesktopSize(string value)
        {
            switch (value)
            {
                case "x640": return DesktopSizeMode.Size640X480;
                case "x800": return DesktopSizeMode.Size800X600;
                case "x1024": return DesktopSizeMode.Size1024X768;
                case "x1152": return DesktopSizeMode.Size1152X864;
                case "x1280": return DesktopSizeMode.Size1280X1024;
                case "FitToWindow": return DesktopSizeMode.FitToWindow;
                case "FullScreen": return DesktopSizeMode.FullScreen;
                case "AutoScale": return DesktopSizeMode.AutoScale;
                case "Custom": return DesktopSizeMode.Custom;
                default: return DesktopSizeMode.FitToWindow;
            }
        }

        private static ColorDepth ParseColors(string value)
        {
            switch (value)
            {
                case "Bits8": return ColorDepth.Depth8;
                case "Bit16": return ColorDepth.Depth16;
                case "Bits24": return ColorDepth.Depth24;
                case "Bits32": return ColorDepth.Depth32;
                default: return ColorDepth.Depth32;
            }
        }

        private static SoundRedirection ParseSounds(string value)
        {
            switch (value)
            {
                case "PlayOnServer": return SoundRedirection.PlayOnRemoteComputer;
                case "DontPlay": return SoundRedirection.DoNotPlay;
                default: return SoundRedirection.PlayOnThisComputer;
            }
        }

        private static GatewayUsageMode ParseGatewayUsage(string value)
        {
            int mode = ReadInt(value, 0);
            if (mode == 1) return GatewayUsageMode.Always;
            if (mode == 2) return GatewayUsageMode.Detect;
            return GatewayUsageMode.Never;
        }

        private static GatewayCredentialSource ParseGatewayCredsSource(string value)
        {
            int source = ReadInt(value, 0);
            if (source == 1) return GatewayCredentialSource.SameAsTarget;
            if (source == 2) return GatewayCredentialSource.Custom;
            if (source == 3) return GatewayCredentialSource.SmartCard;
            return GatewayCredentialSource.Ask;
        }
    }

    public class ImportResult
    {
        public ImportResult(List<RdpConnection> imported, List<string> skipped)
        {
            this.Imported = imported;
            this.Skipped = skipped;
        }

        public List<RdpConnection> Imported { get; }

        public List<string> Skipped { get; }
    }
}
