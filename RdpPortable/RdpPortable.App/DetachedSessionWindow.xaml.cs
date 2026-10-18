using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Forms.Integration;

namespace RdpPortable.App
{
    /// <summary>
    /// Standalone window hosting a detached RDP session. Closing or
    /// re-attaching returns the session to the main window.
    /// </summary>
    public partial class DetachedSessionWindow : Window
    {
        private readonly SessionManager sessionManager;
        private readonly RdpSession session;

        public DetachedSessionWindow(SessionManager sessionManager, RdpSession session)
        {
            this.sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            InitializeComponent();

            this.Title = session.Title;
            this.HostRoot.Children.Add(session.HostElement!);
            this.session.DetachedWindow = this;
        }

        private void OnReattach(object sender, RoutedEventArgs e)
        {
            this.sessionManager.Reattach(this.session);
        }

        private void OnFullScreen(object sender, RoutedEventArgs e)
        {
            this.session.Host.ToggleFullscreen();
        }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            this.session.Host.ToggleFullscreen();
            base.OnMouseDoubleClick(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            this.session.DetachedWindow = null;
            if (!this.session.Reattaching)
                this.sessionManager.Close(this.session);
            base.OnClosed(e);
        }
    }
}
