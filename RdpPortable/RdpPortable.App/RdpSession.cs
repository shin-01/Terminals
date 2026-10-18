using System;
using RdpPortable.Core.Models;

namespace RdpPortable.App
{
    /// <summary>
    /// An open RDP session: the MSTSC host plus its tab/tile placeholders.
    /// </summary>
    public class RdpSession : IDisposable
    {
        public RdpSession(RdpConnection connection, RdpClientHost host)
        {
            this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.Host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public RdpConnection Connection { get; }

        public RdpClientHost Host { get; }

        public System.Windows.Controls.TabItem? Tab { get; set; }

        public System.Windows.Controls.ContentControl? Tile { get; set; }

        public System.Windows.Forms.Integration.WindowsFormsHost? HostElement { get; set; }

        public DetachedSessionWindow? DetachedWindow { get; set; }

        public bool Reattaching { get; set; }

        public string Title
        {
            get
            {
                string name = string.IsNullOrEmpty(this.Connection.Name)
                    ? this.Connection.Server
                    : this.Connection.Name;
                return name + " (" + this.Connection.Server + ")";
            }
        }

        public void Dispose()
        {
            this.Host.Dispose();
        }
    }
}
