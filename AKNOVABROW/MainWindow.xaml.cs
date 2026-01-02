using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;
using AKNOVABROW.Models;
using AKNOVABROW.Services;
using Microsoft.Web.WebView2.Wpf;

namespace AKNOVABROW
{
    public partial class MainWindow : Window
    {
        private int adsBlocked;
        private bool adBlockEnabled;
        private bool vpnConnected;
        private List<string> adPatterns;
        private BookmarkService bookmarkService;
        private VPNService vpnService;
        private SecurityService securityService;
        private VPNServer? currentVPNServer;
        private Dictionary<string, WebView2> tabs;
        private string activeTabId;

        public MainWindow()
        {
            InitializeComponent();
            InitializeServices();
            InitializeVariables();
            InitializeBrowser();
            InitializeVPN();
            CreateInitialTab();
        }

        private void InitializeServices()
        {
            bookmarkService = new BookmarkService();
            vpnService = new VPNService();
            securityService = new SecurityService();
        }

        private void InitializeVariables()
        {
            adsBlocked = 0;
            adBlockEnabled = true;
            vpnConnected = false;
            tabs = new Dictionary<string, WebView2>();
            activeTabId = "tab-0";

            adPatterns = new List<string>
            {
                "doubleclick.net", "googlesyndication.com", "googleadservices.com",
                "youtube.com/api/stats/ads", "youtube.com/ptracking", "youtube.com/pagead",
                "google-analytics.com", "googletagmanager.com", "/ads/", "advertising",
                "malware", "spyware", "phishing", "scam"
            };
        }

        private void InitializeVPN()
        {
            var servers = vpnService.GetAvailableServers();
            VPNComboBox.ItemsSource = servers;
            if (servers.Count > 0)
                VPNComboBox.SelectedIndex = 0;
        }

        private async void InitializeBrowser()
        {
            try
            {
                await Browser.EnsureCoreWebView2Async();
                SetupBrowserEvents(Browser);
                StatusText.Text = "Welcome to AKNOVA Browser - Secure & Private";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing browser: {ex.Message}", "Error");
            }
        }

        private void SetupBrowserEvents(WebView2 browser)
        {
            browser.CoreWebView2.NavigationStarting += Browser_NavigationStarting;
            browser.CoreWebView2.NavigationCompleted += Browser_NavigationCompleted;
            browser.CoreWebView2.SourceChanged += Browser_SourceChanged;
            browser.CoreWebView2.WebResourceRequested += Browser_WebResourceRequested;
            browser.CoreWebView2.NewWindowRequested += Browser_NewWindowRequested;
            browser.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
        }

        //private void CreateInitialTab()
        //{
        //    tabs[activeTabId] = Browser;
        //    //AddTabButton("Home", activeTabId, true);
        //}
        //private void AddTabButton(string title, string tabId, bool isActive)
        //{
        //    var tabButton = new Button
        //    {
        //        Content = $"  {title}  ✕",
        //        Height = 32,
        //        Padding = new Thickness(12, 6, 12, 6),
        //        Margin = new Thickness(2, 0, 2, 0),
        //        Background = isActive ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.LightGray,
        //        BorderThickness = new Thickness(0),
        //        Tag = tabId,
        //        FontSize = 11
        //    };
        //    tabButton.Click += (s, e) =>
        //    {
        //        if (tabButton.Content.ToString()!.Contains("✕") &&
        //            Mouse.DirectlyOver is System.Windows.Documents.Run)
        //        {
        //            CloseTab(tabId);
        //        }
        //        else
        //        {
        //            SwitchToTab(tabId);
        //        }
        //    };
        //    TabsPanel.Children.Insert(TabsPanel.Children.Count - 1, tabButton);
        //}
        //private void AddTabButton(string title, string tabId, bool isActive)
        //{
        //    var tabButton = new Button
        //    {
        //        Content = $"  {title}  ✕",
        //        Height = 32,
        //        Padding = new Thickness(12, 6, 12, 6),
        //        Margin = new Thickness(2, 0, 2, 0),
        //        Background = isActive ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.LightGray,
        //        BorderThickness = new Thickness(0),
        //        Tag = tabId,
        //        FontSize = 11
        //    };
        //    tabButton.Click += (s, e) =>
        //    {
        //        SwitchToTab(tabId);
        //    };
        //    // Find "New Tab" button
        //    Button? newTabButton = null;
        //    foreach (UIElement child in TabsPanel.Children)
        //    {
        //        if (child is Button btn && btn.Content?.ToString()?.Contains("New Tab") == true)
        //        {
        //            newTabButton = btn;
        //            break;
        //        }
        //    }
        //    // Insert before "New Tab" button, or add at end if not found
        //    if (newTabButton != null)
        //    {
        //        int index = TabsPanel.Children.IndexOf(newTabButton);
        //        TabsPanel.Children.Insert(index, tabButton);
        //    }
        //    else
        //    {
        //        TabsPanel.Children.Add(tabButton);
        //    }
        //}
        //private void NewTab_Click(object sender, RoutedEventArgs e)
        //{
        //    var tabId = $"tab-{tabs.Count}";
        //    var newBrowser = new WebView2 { Source = new Uri("https://www.google.com") };
        //    BrowserContainer.Children.Add(newBrowser);
        //    newBrowser.Visibility = Visibility.Collapsed;
        //    tabs[tabId] = newBrowser;
        //    AddTabButton("New Tab", tabId, false);
        //    newBrowser.CoreWebView2InitializationCompleted += (s, args) =>
        //    {
        //        if (args.IsSuccess)
        //        {
        //            SetupBrowserEvents(newBrowser);
        //        }
        //    };
        //}
        //        private async void NewTab_Click(object sender, RoutedEventArgs e)
        //{
        //    var tabId = $"tab-{tabs.Count}";
        //    var newBrowser = new WebView2 { Source = new Uri("https://www.google.com") };

        //    BrowserContainer.Children.Add(newBrowser);
        //    newBrowser.Visibility = Visibility.Collapsed;

        //    tabs[tabId] = newBrowser;

        //    try
        //    {
        //        await newBrowser.EnsureCoreWebView2Async();
        //        SetupBrowserEvents(newBrowser);
        //        AddTabButton($"Tab {tabs.Count}", tabId, false);
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Error creating tab: {ex.Message}");
        //    }
        //}

        //        private void SwitchToTab(string tabId)
        //        {
        //            foreach (var tab in tabs)
        //            {
        //                tabs[tab.Key].Visibility = tab.Key == tabId ? Visibility.Visible : Visibility.Collapsed;
        //            }
        //            activeTabId = tabId;
        //            UpdateTabButtons();
        //        }

        //        private void CloseTab(string tabId)
        //        {
        //            if (tabs.Count <= 1)
        //            {
        //                MessageBox.Show("Cannot close the last tab!", "Warning");
        //                return;
        //            }

        //            var browser = tabs[tabId];
        //            BrowserContainer.Children.Remove(browser);
        //            tabs.Remove(tabId);

        //            var tabButton = TabsPanel.Children.OfType<Button>()
        //                .FirstOrDefault(b => b.Tag?.ToString() == tabId);
        //            if (tabButton != null)
        //                TabsPanel.Children.Remove(tabButton);

        //            if (activeTabId == tabId)
        //            {
        //                activeTabId = tabs.Keys.First();
        //                SwitchToTab(activeTabId);
        //            }
        //        }

        //private void UpdateTabButtons()
        //{
        //    foreach (Button button in TabsPanel.Children.OfType<Button>())
        //    {
        //        if (button.Tag != null)
        //        {
        //            button.Background = button.Tag.ToString() == activeTabId
        //                ? System.Windows.Media.Brushes.White
        //                : System.Windows.Media.Brushes.LightGray;
        //        }
        //    }
        //}

        //private WebView2 GetActiveBrowser() => tabs[activeTabId];

        //private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        //{
        //    StatusText.Text = "Loading...";

        //    // Security check
        //    if (!securityService.IsSafe(e.Uri))
        //    {
        //        var result = MessageBox.Show(
        //            $"{securityService.GetThreatInfo(e.Uri)}\n\nDo you want to proceed anyway?",
        //            "Security Warning",
        //            MessageBoxButton.YesNo,
        //            MessageBoxImage.Warning
        //        );

        //        if (result == MessageBoxResult.No)
        //        {
        //            e.Cancel = true;
        //            StatusText.Text = "Navigation blocked - Security threat detected";
        //            SecurityStatus.Text = "⚠️ Threat Blocked";
        //            SecurityStatus.Foreground = System.Windows.Media.Brushes.Red;
        //            return;
        //        }
        //    }

        //    SecurityStatus.Text = "🛡️ Protected";
        //    SecurityStatus.Foreground = System.Windows.Media.Brushes.Green;
        //}

        private void CreateInitialTab()
        {
            tabs[activeTabId] = Browser;
            AddTabButton("Home", activeTabId, true);
        }

        private void AddTabButton(string title, string tabId, bool isActive)
        {
            var tabButton = new Button
            {
                Content = $"{title} ✕",
                Height = 32,
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(2, 0, 2, 0),
                Background = isActive ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(0),
                Tag = tabId,
                FontSize = 11,
                Cursor = Cursors.Hand
            };

            tabButton.Click += (s, e) => SwitchToTab(tabId);

            tabButton.MouseRightButtonUp += (s, e) =>
            {
                if (tabId != activeTabId && tabs.Count > 1)
                {
                    CloseTab(tabId);
                }
            };

            // Add before the last element (which should be "New Tab" button)
            int insertIndex = Math.Max(0, TabsPanel.Children.Count - 1);
            if (TabsPanel.Children.Count == 0)
            {
                TabsPanel.Children.Add(tabButton);
            }
            else
            {
                TabsPanel.Children.Insert(insertIndex, tabButton);
            }
        }

        private async void NewTab_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var tabId = $"tab-{DateTime.Now.Ticks}";
                var newBrowser = new WebView2();

                BrowserContainer.Children.Add(newBrowser);
                newBrowser.Visibility = Visibility.Collapsed;

                tabs[tabId] = newBrowser;

                await newBrowser.EnsureCoreWebView2Async();
                newBrowser.Source = new Uri("https://www.google.com");
                SetupBrowserEvents(newBrowser);

                AddTabButton($"Tab {tabs.Count}", tabId, false);
                SwitchToTab(tabId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating tab: {ex.Message}", "Error");
            }
        }
        private void SwitchToTab(string tabId)
        {
            if (!tabs.ContainsKey(tabId)) return;

            foreach (var tab in tabs)
            {
                tabs[tab.Key].Visibility = tab.Key == tabId ? Visibility.Visible : Visibility.Collapsed;
            }

            activeTabId = tabId;
            UpdateTabButtons();

            var browser = GetActiveBrowser();
            if (browser.Source != null)
            {
                AddressBar.Text = browser.Source.ToString();
            }
        }
        private void CloseTab(string tabId)
        {
            if (tabs.Count <= 1)
            {
                MessageBox.Show("Cannot close the last tab!", "Warning");
                return;
            }

            if (tabs.ContainsKey(tabId))
            {
                var browser = tabs[tabId];
                BrowserContainer.Children.Remove(browser);
                browser.Dispose();
                tabs.Remove(tabId);
            }

            var tabButton = TabsPanel.Children.OfType<Button>()
                .FirstOrDefault(b => b.Tag?.ToString() == tabId);
            if (tabButton != null)
            {
                TabsPanel.Children.Remove(tabButton);
            }

            if (activeTabId == tabId && tabs.Count > 0)
            {
                activeTabId = tabs.Keys.First();
                SwitchToTab(activeTabId);
            }
        }
        private void UpdateTabButtons()
        {
            foreach (Button button in TabsPanel.Children.OfType<Button>())
            {
                if (button.Tag != null)
                {
                    button.Background = button.Tag.ToString() == activeTabId
                        ? System.Windows.Media.Brushes.White
                        : System.Windows.Media.Brushes.LightGray;
                }
            }
        }
        private WebView2 GetActiveBrowser()
        {
            return tabs.ContainsKey(activeTabId) ? tabs[activeTabId] : Browser;
        }

        private void Browser_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            StatusText.Text = "Loading...";

            // Security check
            if (!securityService.IsSafe(e.Uri))
            {
                var result = MessageBox.Show(
                    $"{securityService.GetThreatInfo(e.Uri)}\n\nDo you want to proceed anyway?",
                    "Security Warning",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    StatusText.Text = "Navigation blocked - Security threat detected";
                    SecurityStatus.Text = "⚠️ Threat Blocked";
                    SecurityStatus.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }
            }

            SecurityStatus.Text = "🛡️ Protected";
            SecurityStatus.Foreground = System.Windows.Media.Brushes.Green;
        }

        private async void Browser_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            StatusText.Text = "Ready";
            UpdateNavButtons();
            await InjectScripts();
        }

        private void Browser_SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
        {
            var browser = GetActiveBrowser();
            AddressBar.Text = browser.Source?.ToString() ?? "";
            SecureIcon.Text = browser.Source?.ToString().StartsWith("https://") == true ? "🔒" : "⚠️";
            Title = $"{browser.CoreWebView2?.DocumentTitle ?? "AKNOVA"} - AKNOVA Browser";
        }

        private void Browser_WebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            if (!adBlockEnabled) return;

            if (adPatterns.Any(p => e.Request.Uri.ToLower().Contains(p)))
            {
                adsBlocked++;
                AdCountText.Text = adsBlocked.ToString();
                UpdateStatsText();
                e.Response = GetActiveBrowser().CoreWebView2.Environment.CreateWebResourceResponse(
                    null, 403, "Blocked", "");
            }
        }

        private void Browser_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;
            GetActiveBrowser().CoreWebView2.Navigate(e.Uri);
        }

        private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            StatusText.Text = "Ready";
            UpdateNavButtons();
            await InjectScripts();
        }

        private void OnSourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
        {
            var browser = GetActiveBrowser();
            AddressBar.Text = browser.Source?.ToString() ?? "";
            SecureIcon.Text = browser.Source?.ToString().StartsWith("https://") == true ? "🔒" : "⚠️";
            Title = $"{browser.CoreWebView2?.DocumentTitle ?? "AKNOVA"} - AKNOVA Browser";
        }

        private void OnWebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            if (!adBlockEnabled) return;

            if (adPatterns.Any(p => e.Request.Uri.ToLower().Contains(p)))
            {
                adsBlocked++;
                AdCountText.Text = adsBlocked.ToString();
                UpdateStatsText();
                e.Response = GetActiveBrowser().CoreWebView2.Environment.CreateWebResourceResponse(
                    null, 403, "Blocked", "");
            }
        }

        private void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;
            GetActiveBrowser().CoreWebView2.Navigate(e.Uri);
        }

        private async System.Threading.Tasks.Task InjectScripts()
        {
            try
            {
                await GetActiveBrowser().ExecuteScriptAsync(@"
                    if(window.location.hostname.includes('youtube.com')){
                        const s=document.createElement('style');
                        s.textContent='ytd-reel-shelf-renderer,[is-shorts]{display:none!important}';
                        s.id='ab';
                        if(!document.getElementById('ab'))document.head.appendChild(s);
                        document.addEventListener('click',e=>{
                            const l=e.target.closest('a');
                            if(l?.href?.includes('/shorts/')){
                                e.preventDefault();
                                window.location.href='https://www.youtube.com/watch?v='+l.href.split('/shorts/')[1].split('?')[0];
                            }
                        },true);
                    }
                ");
            }
            catch { }
        }

        // Window Controls
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                Maximize_Click(sender, e);
            else
                DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void Maximize_Click(object sender, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        // Navigation
        private void BackButton_Click(object sender, RoutedEventArgs e) => GetActiveBrowser().GoBack();
        private void ForwardButton_Click(object sender, RoutedEventArgs e) => GetActiveBrowser().GoForward();
        private void RefreshButton_Click(object sender, RoutedEventArgs e) => GetActiveBrowser().Reload();
        private void HomeButton_Click(object sender, RoutedEventArgs e) =>
            GetActiveBrowser().Source = new Uri("https://www.google.com");
        private void GoButton_Click(object sender, RoutedEventArgs e) => Navigate(AddressBar.Text);
        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Navigate(AddressBar.Text);
        }

        private void Navigate(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            url = url.Trim();
            if (!url.StartsWith("http"))
                url = url.Contains(".") ? "https://" + url :
                      "https://www.google.com/search?q=" + Uri.EscapeDataString(url);
            if (url.Contains("/shorts/"))
                url = "https://www.youtube.com/watch?v=" + url.Split("/shorts/")[1].Split('?')[0];
            GetActiveBrowser().Source = new Uri(url);
        }

        // VPN
        private void VPNConnect_Click(object sender, RoutedEventArgs e)
        {
            if (vpnConnected)
            {
                vpnService.Disconnect();
                vpnConnected = false;
                VPNConnectButton.Content = "Connect";
                VPNConnectButton.Background = System.Windows.Media.Brushes.Orange;
                currentVPNServer = null;
                UpdateStatsText();
                MessageBox.Show("VPN Disconnected", "VPN Status");
            }
            else
            {
                currentVPNServer = VPNComboBox.SelectedItem as VPNServer;
                if (currentVPNServer != null)
                {
                    vpnService.Connect(currentVPNServer);
                    vpnConnected = true;
                    VPNConnectButton.Content = "Disconnect";
                    VPNConnectButton.Background = System.Windows.Media.Brushes.Green;
                    UpdateStatsText();
                    MessageBox.Show($"Connected to {currentVPNServer.Country}", "VPN Status");
                }
            }
        }

        // Bookmarks
        private void AddBookmark_Click(object sender, RoutedEventArgs e)
        {
            var browser = GetActiveBrowser();
            var bookmark = new Bookmark
            {
                Title = browser.CoreWebView2?.DocumentTitle ?? "Untitled",
                Url = browser.Source?.ToString() ?? "",
                CreatedAt = DateTime.Now
            };

            bookmarkService.AddBookmark(bookmark);
            MessageBox.Show($"Bookmark added: {bookmark.Title}", "Bookmarks");
        }

        private void ShowBookmarks_Click(object sender, RoutedEventArgs e)
        {
            var bookmarks = bookmarkService.GetBookmarks();
            var window = new Window
            {
                Title = "Bookmark Manager",
                Width = 600,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var listBox = new ListBox { Margin = new Thickness(10) };
            foreach (var bookmark in bookmarks)
            {
                var item = new ListBoxItem
                {
                    Content = $"{bookmark.Title}\n{bookmark.Url}",
                    Tag = bookmark
                };
                item.MouseDoubleClick += (s, args) =>
                {
                    GetActiveBrowser().Source = new Uri(bookmark.Url);
                    window.Close();
                };
                listBox.Items.Add(item);
            }
            Grid.SetRow(listBox, 0);
            grid.Children.Add(listBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10)
            };

            var deleteBtn = new Button { Content = "Delete", Width = 80, Margin = new Thickness(5) };
            deleteBtn.Click += (s, args) =>
            {
                if (listBox.SelectedItem is ListBoxItem item && item.Tag is Bookmark bm)
                {
                    bookmarkService.DeleteBookmark(bm);
                    listBox.Items.Remove(item);
                }
            };

            var exportBtn = new Button { Content = "Export", Width = 80, Margin = new Thickness(5) };
            exportBtn.Click += (s, args) =>
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json",
                    FileName = "bookmarks.json"
                };
                if (dialog.ShowDialog() == true)
                {
                    bookmarkService.ExportBookmarks(dialog.FileName);
                    MessageBox.Show("Bookmarks exported!", "Success");
                }
            };

            var importBtn = new Button { Content = "Import", Width = 80, Margin = new Thickness(5) };
            importBtn.Click += (s, args) =>
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "JSON files (*.json)|*.json"
                };
                if (dialog.ShowDialog() == true)
                {
                    bookmarkService.ImportBookmarks(dialog.FileName);
                    MessageBox.Show("Bookmarks imported!", "Success");
                    window.Close();
                    ShowBookmarks_Click(sender, e);
                }
            };

            buttonPanel.Children.Add(deleteBtn);
            buttonPanel.Children.Add(exportBtn);
            buttonPanel.Children.Add(importBtn);
            Grid.SetRow(buttonPanel, 1);
            grid.Children.Add(buttonPanel);

            window.Content = grid;
            window.ShowDialog();
        }

        // Security
        private void SecurityPanel_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                $"🛡️ SECURITY STATUS\n\n" +
                $"✅ Ad Blocker: {(adBlockEnabled ? "Active" : "Disabled")}\n" +
                $"✅ Ads Blocked: {adsBlocked}\n" +
                $"✅ Anti-Malware: Active\n" +
                $"✅ Anti-Phishing: Active\n" +
                $"✅ Secure Connection: {(SecureIcon.Text == "🔒" ? "Yes" : "No")}\n" +
                $"✅ VPN Status: {(vpnConnected ? "Connected" : "Disconnected")}\n\n" +
                $"Your browsing is protected!",
                "Security Dashboard",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        // Ad Blocker
        private void AdBlockButton_Click(object sender, RoutedEventArgs e)
        {
            adBlockEnabled = !adBlockEnabled;
            AdBlockButton.Background = adBlockEnabled
                ? System.Windows.Media.Brushes.Green
                : System.Windows.Media.Brushes.Red;
            MessageBox.Show(adBlockEnabled ? "Ad Blocker Enabled" : "Ad Blocker Disabled", "Ad Blocker");
        }

        // Menu
        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            menu.Items.Add(CreateMenuItem("New Window", (s, a) => new MainWindow().Show()));
            menu.Items.Add(new Separator());
            menu.Items.Add(CreateMenuItem("Zoom In", (s, a) => GetActiveBrowser().ZoomFactor += 0.1));
            menu.Items.Add(CreateMenuItem("Zoom Out", (s, a) => GetActiveBrowser().ZoomFactor -= 0.1));
            menu.Items.Add(CreateMenuItem("Reset Zoom", (s, a) => GetActiveBrowser().ZoomFactor = 1.0));
            menu.Items.Add(new Separator());
            menu.Items.Add(CreateMenuItem("Clear Cache", async (s, a) =>
            {
                await GetActiveBrowser().CoreWebView2.Profile.ClearBrowsingDataAsync();
                MessageBox.Show("Cache cleared!");
            }));
            menu.Items.Add(new Separator());
            menu.Items.Add(CreateMenuItem("About", (s, a) =>
            {
                MessageBox.Show("AKNOVA Browser v1.0\n\n© 2025 - Secure & Private", "About");
            }));

            menu.PlacementTarget = sender as Button;
            menu.IsOpen = true;
        }

        private MenuItem CreateMenuItem(string h, RoutedEventHandler r)
        {
            var i = new MenuItem { Header = h };
            i.Click += r;
            return i;
        }

        private void UpdateNavButtons()
        {
            var browser = GetActiveBrowser();
            BackButton.IsEnabled = browser.CanGoBack;
            ForwardButton.IsEnabled = browser.CanGoForward;
        }

        private void UpdateStatsText()
        {
            var vpnStatus = vpnConnected && currentVPNServer != null
                ? $"VPN: {currentVPNServer.Country}"
                : "VPN: Disconnected";
            StatsText.Text = $"Ads: {adsBlocked} | {vpnStatus}";
        }
    }
}