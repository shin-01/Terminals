using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using RdpPortable.Core.Models;

namespace RdpPortable.App
{
    /// <summary>
    /// Manages up to MaxSessions simultaneous RDP sessions and the
    /// tabbed / tiled (side-by-side, stacked, 2x2 grid) layouts.
    /// </summary>
    public class SessionManager : IDisposable
    {
        public const int MaxSessions = 4;

        private readonly TabControl tabs;
        private readonly UniformGrid tiles;
        private readonly ObservableCollection<RdpSession> sessions = new ObservableCollection<RdpSession>();

        public event Action<string>? StatusMessage;

        public SessionManager(TabControl tabs, UniformGrid tiles)
        {
            this.tabs = tabs ?? throw new ArgumentNullException(nameof(tabs));
            this.tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
        }

        public ObservableCollection<RdpSession> Sessions => this.sessions;

        public SessionLayout Layout { get; private set; } = SessionLayout.Tabs;

        public bool CanConnect => this.sessions.Count < MaxSessions;

        public RdpSession? ActiveSession
        {
            get
            {
                var selectedTab = this.tabs.SelectedItem as TabItem;
                return selectedTab == null ? null : this.FirstByTab(selectedTab);
            }
        }

        private RdpSession? FirstByTab(TabItem tab)
        {
            foreach (var session in this.sessions)
            {
                if (session.Tab == tab)
                    return session;
            }

            return null;
        }

        public void SetLayout(SessionLayout layout)
        {
            this.Layout = layout;

            bool tiled = layout != SessionLayout.Tabs;
            this.tiles.Visibility = tiled ? Visibility.Visible : Visibility.Collapsed;
            this.tabs.Visibility = tiled ? Visibility.Collapsed : Visibility.Visible;

            this.tiles.Rows = 1;
            this.tiles.Columns = 1;
            if (layout == SessionLayout.Horizontal)
                this.tiles.Columns = MaxSessions;
            else if (layout == SessionLayout.Vertical)
                this.tiles.Rows = MaxSessions;
            else if (layout == SessionLayout.Grid)
            {
                this.tiles.Rows = 2;
                this.tiles.Columns = 2;
            }

            this.RebuildContainers();
        }

        public RdpSession Open(RdpConnection connection)
        {
            if (!this.CanConnect)
                throw new InvalidOperationException("Limite di " + MaxSessions + " sessioni raggiunto.");

            var host = new RdpClientHost();
            var session = new RdpSession(connection, host);
            host.Initialize();
            host.Disconnected += message =>
                Application.Current?.Dispatcher.Invoke(() => this.StatusMessage?.Invoke(message));

            this.sessions.Add(session);
            this.RebuildContainers();
            host.Connect(connection);
            return session;
        }

        public void Close(RdpSession session)
        {
            int index = this.sessions.IndexOf(session);
            if (index < 0)
                return;

            this.sessions.Remove(session);

            if (session.DetachedWindow != null)
            {
                session.Reattaching = false;
                session.DetachedWindow.Close();
                session.DetachedWindow = null;
            }

            this.RebuildContainers();
            session.Dispose();

            if (this.sessions.Count > 0)
                this.tabs.SelectedIndex = Math.Min(index, this.sessions.Count - 1);
        }

        public void Detach(RdpSession session)
        {
            if (session.DetachedWindow != null)
            {
                session.DetachedWindow.Activate();
                return;
            }

            this.RebuildContainersWithout(session);

            session.Reattaching = false;
            var window = new DetachedSessionWindow(this, session);
            session.DetachedWindow = window;
            window.Show();
        }

        public void Reattach(RdpSession session)
        {
            var window = session.DetachedWindow;
            if (window == null)
                return;

            session.Reattaching = true;
            window.Close();
            session.DetachedWindow = null;
            session.Reattaching = false;

            this.RebuildContainers();
            if (this.Layout == SessionLayout.Tabs)
                this.tabs.SelectedItem = session.Tab;
        }

        public void ToggleFullscreen(RdpSession session)
        {
            session.Host.ToggleFullscreen();
        }

        public void CloseAll()
        {
            while (this.sessions.Count > 0)
                this.Close(this.sessions[0]);
        }

        private void RebuildContainers()
        {
            this.RebuildContainersWithout(null);
        }

        private void RebuildContainersWithout(RdpSession? excluded)
        {
            this.tabs.Items.Clear();
            this.tiles.Children.Clear();

            foreach (var session in this.sessions)
            {
                if (session == excluded)
                {
                    session.Tab = null;
                    session.Tile = null;
                    continue;
                }

                session.Tab = null;
                session.Tile = null;

                if (this.Layout == SessionLayout.Tabs)
                    this.CreateTab(session);
                else
                    this.CreateTile(session);
            }

            if (this.sessions.Count > 0 && this.Layout == SessionLayout.Tabs && this.tabs.SelectedIndex < 0)
                this.tabs.SelectedIndex = 0;
        }

        private static WindowsFormsHost EnsureHost(RdpSession session)
        {
            if (session.HostElement == null)
                session.HostElement = new WindowsFormsHost { Child = session.Host.Control };
            else if (session.HostElement.Parent is System.Windows.Controls.Panel panel)
                panel.Children.Remove(session.HostElement);
            else if (session.HostElement.Parent is System.Windows.Controls.ContentControl content)
                content.Content = null;

            return session.HostElement;
        }

        private void CreateTab(RdpSession session)
        {
            var detach = new Button
            {
                Content = "⇱",
                Width = 20,
                Height = 18,
                Padding = new Thickness(0),
                Margin = new Thickness(6, 0, 0, 0),
                FontSize = 10,
                ToolTip = "Detach: sessione in finestra autonoma",
            };
            detach.Click += (s, e) => this.Detach(session);
            var close = new Button
            {
                Content = "✕",
                Width = 20,
                Height = 18,
                Padding = new Thickness(0),
                Margin = new Thickness(3, 0, 0, 0),
                FontSize = 10,
                ToolTip = "Chiudi sessione",
            };
            close.Click += (s, e) => this.Close(session);
            var header = new StackPanel { Orientation = Orientation.Horizontal };
            var titleBlock = new TextBlock { Text = session.Title, VerticalAlignment = VerticalAlignment.Center };
            titleBlock.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                    this.ToggleFullscreen(session);
            };
            header.Children.Add(titleBlock);
            header.Children.Add(detach);
            header.Children.Add(close);

            session.Tab = new TabItem { Header = header, Content = EnsureHost(session) };
            this.tabs.Items.Add(session.Tab);
        }

        private void CreateTile(RdpSession session)
        {
            var close = new Button { Content = "✕", Width = 20, Height = 18, Padding = new Thickness(0), FontSize = 10 };
            close.Click += (s, e) => this.Close(session);
            var detach = new Button { Content = "⇱", Width = 20, Height = 18, Padding = new Thickness(0), FontSize = 10, ToolTip = "Detach: sessione in finestra autonoma" };
            detach.Click += (s, e) => this.Detach(session);
            var title = new TextBlock { Text = session.Title, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 4, 0) };
            var header = new DockPanel { LastChildFill = true };
            DockPanel.SetDock(close, Dock.Right);
            DockPanel.SetDock(detach, Dock.Right);
            header.Children.Add(close);
            header.Children.Add(detach);
            header.Children.Add(title);

            var tile = new DockPanel { Background = System.Windows.Media.Brushes.Black };
            DockPanel.SetDock(header, Dock.Top);
            tile.Children.Add(header);
            tile.Children.Add(EnsureHost(session));

            session.Tile = new ContentControl { Content = tile };
            this.tiles.Children.Add(session.Tile);
        }

        public void Dispose()
        {
            this.CloseAll();
        }
    }

    public enum SessionLayout
    {
        Tabs,
        Horizontal,
        Vertical,
        Grid,
    }
}
