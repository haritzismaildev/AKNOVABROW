using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;
using AKNOVABROW.Models;
using AKNOVABROW.Services;

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
        private VPNServer currentVPNServer; // Remove ? to avoid nullable warning
        private Dictionary<string, WebView2> tabs;
        private string activeTabId;

        // Window state persistence
        private const string WINDOW_STATE_KEY = "WindowState";
        private const string WINDOW_LEFT_KEY = "WindowLeft";
        private const string WINDOW_TOP_KEY = "WindowTop";
        private const string WINDOW_WIDTH_KEY = "WindowWidth";
        private const string WINDOW_HEIGHT_KEY = "WindowHeight";
        private const string WINDOW_MAXIMIZED_KEY = "WindowMaximized";

        private List<string> adDomains;
        private List<string> adKeywords;
        private HashSet<string> blockedDomains;

        public MainWindow()
        {
            InitializeComponent();
            // Load window state BEFORE showing
            LoadWindowState();
            InitializeServices();
            InitializeVariables();
            InitializeBrowser();
            InitializeVPN();
            CreateInitialTab();

            // ADD THIS - F11 for fullscreen
            this.KeyDown += MainWindow_KeyDown;

            // Save window state on close
            this.Closing += MainWindow_Closing;

            // Show tips
            this.Loaded += (s, e) => ShowBrowserTips();
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

            // COMPREHENSIVE AD DOMAINS (based on EasyList + uBlock Origin)
            adDomains = new List<string>
    {
        // Google Ads
        "doubleclick.net", "googlesyndication.com", "googleadservices.com",
        "google-analytics.com", "googletagmanager.com", "googletagservices.com",
        "adservice.google.com", "pagead2.googlesyndication.com",
        
        // Facebook/Meta Ads
        "facebook.com/tr", "connect.facebook.net", "facebook.com/plugins",
        
        // YouTube Ads
        "youtube.com/api/stats/ads", "youtube.com/ptracking",
        "youtube.com/pagead", "youtube.com/get_video_info",
        "s.ytimg.com/yts/jsbin", "youtube.com/youtubei/v1/log_event",
        
        // Ad Networks
        "adnxs.com", "adsrvr.org", "advertising.com", "criteo.com",
        "scorecardresearch.com", "2mdn.net", "admob.com", "adsense.com",
        "pubmatic.com", "rubiconproject.com", "openx.net", "contextweb.com",
        "advertising.yahoo.com", "gemini.yahoo.com", "ads.yahoo.com",
        
        // Trackers
        "track", "telemetry", "analytics", "metrics", "pixel",
        "beacon", "statsig", "amplitude", "mixpanel", "segment",
        
        // Video Ads
        "imasdk.googleapis.com", "innovid.com", "videoplaza.tv",
        "fwmrm.net", "spotxchange.com",
        
        // Pop-ups & Redirects
        "popads.net", "popcash.net", "propellerads.com", "zedo.com",
        "adcash.com", "juicyads.com", "exoclick.com", "trafficjunky.com",
        
        // Social Media Trackers
        "platform.twitter.com/widgets", "apis.google.com/js/plusone",
        "platform.linkedin.com/in", "static.addtoany.com",
        
        // More Ad Servers
        "taboola.com", "outbrain.com", "revcontent.com", "mgid.com",
        "contentabc.com", "content.ad", "adblade.com", "adroll.com"
    };

            adKeywords = new List<string>
    {
        "/ads/", "/ad/", "/advert", "/banner", "/sponsor",
        "ad-", "ads-", "_ad_", "_ads_", "advertisement",
        "tracking", "tracker", "telemetry", "metrics",
        "doubleclick", "adsystem", "adserver", "adtech"
    };

            // Blocked domains set for fast lookup
            blockedDomains = new HashSet<string>(adDomains);
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

                // Enable context menu
                Browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                Browser.CoreWebView2.Settings.AreDevToolsEnabled = true;

                SetupBrowserEvents(Browser);
                StatusText.Text = "Welcome to AKNOVA Browser - Secure & Private";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing browser: {ex.Message}", "Error");
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // F11 - Fullscreen
            if (e.Key == Key.F11)
            {
                ToggleFullscreen();
                e.Handled = true;
            }
            // ESC - Exit fullscreen
            else if (e.Key == Key.Escape && this.WindowState == WindowState.Maximized)
            {
                ExitFullscreen();
                e.Handled = true;
            }
            // Ctrl+T - New Tab
            else if (e.Key == Key.T && Keyboard.Modifiers == ModifierKeys.Control)
            {
                NewTab_Click(sender, new RoutedEventArgs());
                e.Handled = true;
            }
            // Ctrl+W - Close Tab
            else if (e.Key == Key.W && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (tabs.Count > 1)
                {
                    CloseTab(activeTabId);
                }
                e.Handled = true;
            }
            // Ctrl+Tab - Next Tab
            else if (e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SwitchToNextTab();
                e.Handled = true;
            }
            // Ctrl+Shift+Tab - Previous Tab
            else if (e.Key == Key.Tab && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                SwitchToPreviousTab();
                e.Handled = true;
            }
            // Ctrl+1 to Ctrl+9 - Switch to tab number
            else if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key >= Key.D1 && e.Key <= Key.D9)
                {
                    int tabIndex = e.Key - Key.D1;
                    SwitchToTabByIndex(tabIndex);
                    e.Handled = true;
                }
            }
        }

        private async void LoadWindowState()
        {
            try
            {
                var prefs = await SharedPreferences.GetInstance();

                // Load window size
                var width = prefs.GetDouble(WINDOW_WIDTH_KEY, 1400);
                var height = prefs.GetDouble(WINDOW_HEIGHT_KEY, 900);

                this.Width = width;
                this.Height = height;

                // Load window position
                var left = prefs.GetDouble(WINDOW_LEFT_KEY, -1);
                var top = prefs.GetDouble(WINDOW_TOP_KEY, -1);

                if (left >= 0 && top >= 0)
                {
                    // Check if position is within screen bounds
                    if (left < SystemParameters.VirtualScreenWidth &&
                        top < SystemParameters.VirtualScreenHeight)
                    {
                        this.Left = left;
                        this.Top = top;
                        this.WindowStartupLocation = WindowStartupLocation.Manual;
                    }
                    else
                    {
                        // Position out of bounds, center it
                        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    }
                }
                else
                {
                    // First run, center window
                    this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }

                // Load maximized state
                var isMaximized = prefs.GetBool(WINDOW_MAXIMIZED_KEY, false);
                if (isMaximized)
                {
                    this.WindowState = WindowState.Maximized;
                }
            }
            catch
            {
                // If error, just center window
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }

        private async void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                var prefs = await SharedPreferences.GetInstance();

                // Save window state (not maximized)
                if (this.WindowState == WindowState.Normal)
                {
                    await prefs.SetDouble(WINDOW_WIDTH_KEY, this.Width);
                    await prefs.SetDouble(WINDOW_HEIGHT_KEY, this.Height);
                    await prefs.SetDouble(WINDOW_LEFT_KEY, this.Left);
                    await prefs.SetDouble(WINDOW_TOP_KEY, this.Top);
                    await prefs.SetBool(WINDOW_MAXIMIZED_KEY, false);
                }
                else if (this.WindowState == WindowState.Maximized)
                {
                    await prefs.SetBool(WINDOW_MAXIMIZED_KEY, true);
                }
            }
            catch { }
        }

        private void ToggleFullscreen()
        {
            if (this.WindowState == WindowState.Maximized && TitleBarGrid.Visibility == Visibility.Collapsed)
            {
                ExitFullscreen();
            }
            else
            {
                EnterFullscreen();
            }
        }

        private void EnterFullscreen()
        {
            this.WindowState = WindowState.Maximized;
            this.WindowStyle = WindowStyle.None;
            TitleBarGrid.Visibility = Visibility.Collapsed;
            NavigationBarGrid.Visibility = Visibility.Collapsed;
            TabsBarGrid.Visibility = Visibility.Collapsed;
        }

        private void ExitFullscreen()
        {
            this.WindowState = WindowState.Normal;
            this.WindowStyle = WindowStyle.None;
            TitleBarGrid.Visibility = Visibility.Visible;
            NavigationBarGrid.Visibility = Visibility.Visible;
            TabsBarGrid.Visibility = Visibility.Visible;
        }

        private void SetupBrowserEvents(WebView2 browser)
        {
            browser.CoreWebView2.NavigationStarting += Browser_NavigationStarting;
            browser.CoreWebView2.NavigationCompleted += Browser_NavigationCompleted;
            browser.CoreWebView2.SourceChanged += Browser_SourceChanged;
            browser.CoreWebView2.WebResourceRequested += Browser_WebResourceRequested;
            browser.CoreWebView2.NewWindowRequested += Browser_NewWindowRequested;

            // ADD FILTERS FOR ALL RESOURCE TYPES
            browser.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            browser.CoreWebView2.AddWebResourceRequestedFilter("https://*/*", CoreWebView2WebResourceContext.Script);
            browser.CoreWebView2.AddWebResourceRequestedFilter("https://*/*", CoreWebView2WebResourceContext.Image);
            browser.CoreWebView2.AddWebResourceRequestedFilter("https://*/*", CoreWebView2WebResourceContext.XmlHttpRequest);

            browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            browser.CoreWebView2.Settings.AreDevToolsEnabled = true;

            browser.CoreWebView2.ContainsFullScreenElementChanged += Browser_FullScreenChanged;
        }

        //private void Browser_ContextMenuRequested(object? sender, CoreWebView2ContextMenuRequestedEventArgs e)
        //{
        //    // Get context menu items
        //    var menuItems = e.MenuItems;

        //    // Check if it's a link
        //    if (!string.IsNullOrEmpty(e.ContextMenuTarget.LinkUri))
        //    {
        //        var linkUri = e.ContextMenuTarget.LinkUri;

        //        // Create custom menu item "Open link in new tab"
        //        var newTabItem = e.ContextMenuTarget.CreateMenuItem(
        //            "Open link in new tab",
        //            null,
        //            CoreWebView2ContextMenuItemKind.Command
        //        );

        //        newTabItem.CustomItemSelected += (s, args) =>
        //        {
        //            Dispatcher.Invoke(() =>
        //            {
        //                CreateNewTabWithUrl(linkUri);
        //            });
        //        };

        //        // Insert at position 0 (top)
        //        menuItems.Insert(0, newTabItem);

        //        // Add separator
        //        var separator = e.ContextMenuTarget.CreateMenuItem(
        //            string.Empty,
        //            null,
        //            CoreWebView2ContextMenuItemKind.Separator
        //        );
        //        menuItems.Insert(1, separator);
        //    }

        //    // Add "Inspect" for all contexts
        //    var inspectItem = e.ContextMenuTarget.CreateMenuItem(
        //        "Inspect Element",
        //        null,
        //        CoreWebView2ContextMenuItemKind.Command
        //    );

        //    inspectItem.CustomItemSelected += (s, args) =>
        //    {
        //        Dispatcher.Invoke(() =>
        //        {
        //            var browser = sender as CoreWebView2;
        //            browser?.OpenDevToolsWindow();
        //        });
        //    };

        //    menuItems.Add(inspectItem);
        //}

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
                Height = 35,
                Padding = new Thickness(12, 6, 35, 6), // Extra padding for close button
                Margin = new Thickness(2, 0, 2, 0),
                Background = isActive ? System.Windows.Media.Brushes.White : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240)),
                BorderThickness = new Thickness(1),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(204, 204, 204)),
                Tag = tabId,
                FontSize = 11,
                Cursor = Cursors.Hand,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };

            // Create tab content with title and close button
            var grid = new Grid();

            var titleText = new TextBlock
            {
                Text = title.Length > 20 ? title.Substring(0, 20) + "..." : title,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };

            var closeButton = new TextBlock
            {
                Text = "✕",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(5, 0, 0, 0),
                Cursor = Cursors.Hand
            };

            closeButton.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                CloseTab(tabId);
            };

            grid.Children.Add(titleText);
            grid.Children.Add(closeButton);

            tabButton.Content = grid;

            tabButton.Click += (s, e) => SwitchToTab(tabId);

            // Add before "New Tab" button
            int insertIndex = TabsPanel.Children.Count - 1; // Before "New Tab" button
            if (insertIndex < 0) insertIndex = 0;

            TabsPanel.Children.Insert(insertIndex, tabButton);
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

                AddTabButton($"New Tab", tabId, false);
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

            // Hide all tabs
            foreach (var tab in tabs)
            {
                tabs[tab.Key].Visibility = tab.Key == tabId ? Visibility.Visible : Visibility.Collapsed;
            }

            activeTabId = tabId;
            UpdateTabButtons();

            // Update address bar with active tab's URL
            var browser = GetActiveBrowser();
            if (browser.Source != null)
            {
                AddressBar.Text = browser.Source.ToString();
            }

            // Update tab title
            UpdateActiveTabTitle();
        }
        private void CloseTab(string tabId)
        {
            if (tabs.Count <= 1)
            {
                MessageBox.Show("Cannot close the last tab!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (tabs.ContainsKey(tabId))
            {
                var browser = tabs[tabId];
                BrowserContainer.Children.Remove(browser);
                browser.Dispose();
                tabs.Remove(tabId);
            }

            // Remove tab button
            Button? tabButtonToRemove = null;
            foreach (Button button in TabsPanel.Children.OfType<Button>())
            {
                if (button.Tag?.ToString() == tabId)
                {
                    tabButtonToRemove = button;
                    break;
                }
            }

            if (tabButtonToRemove != null)
            {
                TabsPanel.Children.Remove(tabButtonToRemove);
            }

            // Switch to another tab if closed tab was active
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
                if (button.Tag != null && button.Tag.ToString() != "newtab")
                {
                    string btnTabId = button.Tag.ToString()!;
                    button.Background = btnTabId == activeTabId
                        ? System.Windows.Media.Brushes.White
                        : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240));

                    button.BorderBrush = btnTabId == activeTabId
                        ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 150, 243))
                        : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(204, 204, 204));
                }
            }
        }

        private void UpdateActiveTabTitle()
        {
            var browser = GetActiveBrowser();
            var title = browser.CoreWebView2?.DocumentTitle ?? "New Tab";

            // Find and update active tab button text
            foreach (Button button in TabsPanel.Children.OfType<Button>())
            {
                if (button.Tag?.ToString() == activeTabId)
                {
                    var grid = button.Content as Grid;
                    if (grid != null && grid.Children.Count > 0)
                    {
                        var textBlock = grid.Children[0] as TextBlock;
                        if (textBlock != null)
                        {
                            textBlock.Text = title.Length > 20 ? title.Substring(0, 20) + "..." : title;
                        }
                    }
                    break;
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
            UpdateActiveTabTitle(); // ADD THIS LINE
            await InjectScripts();
        }

        private void Browser_SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
        {
            var browser = GetActiveBrowser();
            AddressBar.Text = browser.Source?.ToString() ?? "";
            SecureIcon.Text = browser.Source?.ToString().StartsWith("https://") == true ? "🔒" : "⚠️";
            Title = $"{browser.CoreWebView2?.DocumentTitle ?? "AKNOVA"} - AKNOVA Browser";
        }

        private void Browser_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            if (!adBlockEnabled) return;

            var url = e.Request.Uri.ToLower();

            // Don't block video streams!
            if (url.Contains("googlevideo.com") ||
                url.Contains("videoplayback") ||
                url.Contains(".mp4") ||
                url.Contains(".webm"))
            {
                return; // Allow video content
            }

            // Block by domain
            if (blockedDomains.Any(domain => url.Contains(domain)))
            {
                BlockRequest(e, url);
                return;
            }

            // Block by keyword (but be careful)
            if (adKeywords.Any(keyword => url.Contains(keyword)))
            {
                // Extra check - don't block if it's part of legit content
                if (!url.Contains("youtube.com/watch") &&
                    !url.Contains("player") &&
                    !url.Contains("video"))
                {
                    BlockRequest(e, url);
                    return;
                }
            }

            var resourceContext = e.ResourceContext;

            // Block tracking
            if (resourceContext == CoreWebView2WebResourceContext.XmlHttpRequest ||
                resourceContext == CoreWebView2WebResourceContext.Fetch)
            {
                if (url.Contains("log_event") ||
                    url.Contains("ptracking") ||
                    url.Contains("/stats/ads"))
                {
                    BlockRequest(e, url);
                    return;
                }
            }
        }

        private void BlockRequest(CoreWebView2WebResourceRequestedEventArgs e, string url)
        {
            adsBlocked++;
            AdCountText.Text = adsBlocked.ToString();
            UpdateStatsText();

            // Create empty response
            e.Response = GetActiveBrowser().CoreWebView2.Environment.CreateWebResourceResponse(
                null, 403, "Blocked by AKNOVA", "Content-Type: text/plain");

            System.Diagnostics.Debug.WriteLine($"[AD BLOCKED] {url}");
        }

        private void Browser_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;

            // Open in new tab
            Dispatcher.Invoke(() =>
            {
                CreateNewTabWithUrl(e.Uri);
            });
        }

        private void ShowBrowserTips()
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var folder = Path.Combine(appData, "AKNOVABROW");
                Directory.CreateDirectory(folder);
                var tipsFile = Path.Combine(folder, "tips_shown.txt");

                if (!File.Exists(tipsFile))
                {
                    MessageBox.Show(
                        "🎯 AKNOVA Browser Quick Guide:\n\n" +
                        "📌 Open Link in New Tab:\n" +
                        "   • Ctrl + Click on link\n" +
                        "   • Middle mouse click\n" +
                        "   • Right-click → \"Open in new window\"\n\n" +
                        "⌨️ Keyboard Shortcuts:\n" +
                        "   • Ctrl + T → New tab\n" +
                        "   • Ctrl + W → Close tab\n" +
                        "   • Ctrl + Tab → Next tab\n" +
                        "   • Ctrl + 1-9 → Switch to tab\n" +
                        "   • F11 → Fullscreen\n\n" +
                        "🛡️ Ad Blocker is automatically enabled!\n\n" +
                        "Enjoy secure browsing! 🚀",
                        "Welcome to AKNOVA Browser",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    File.WriteAllText(tipsFile, "shown");
                }
            }
            catch { }
        }

        private async void CreateNewTabWithUrl(string url)
        {
            try
            {
                var tabId = $"tab-{DateTime.Now.Ticks}";
                var newBrowser = new WebView2();

                BrowserContainer.Children.Add(newBrowser);
                newBrowser.Visibility = Visibility.Collapsed;

                tabs[tabId] = newBrowser;

                await newBrowser.EnsureCoreWebView2Async();
                SetupBrowserEvents(newBrowser);
                newBrowser.Source = new Uri(url);

                AddTabButton("Loading...", tabId, false);
                SwitchToTab(tabId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating tab: {ex.Message}", "Error");
            }
        }

        private void Browser_FullScreenChanged(object? sender, object e)
        {
            var browser = sender as CoreWebView2;
            if (browser == null) return;

            this.Dispatcher.Invoke(() =>
            {
                if (browser.ContainsFullScreenElement)
                {
                    // Enter fullscreen mode
                    this.WindowState = WindowState.Maximized;
                    this.WindowStyle = WindowStyle.None;
                    this.Topmost = true;

                    // Hide all navigation elements
                    TitleBarGrid.Visibility = Visibility.Collapsed;
                    NavigationBarGrid.Visibility = Visibility.Collapsed;
                    TabsBarGrid.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // Exit fullscreen mode
                    this.WindowState = WindowState.Normal;
                    this.WindowStyle = WindowStyle.None;
                    this.Topmost = false;

                    // Show all navigation elements
                    TitleBarGrid.Visibility = Visibility.Visible;
                    NavigationBarGrid.Visibility = Visibility.Visible;
                    TabsBarGrid.Visibility = Visibility.Visible;
                }
            });
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
                var script = @"
                    (function() {
                        'use strict';
    
                        // ===== YOUTUBE SPECIFIC AD BLOCKER =====
                        if (window.location.hostname.includes('youtube.com')) {
        
                            console.log('[AKNOVA] YouTube ad blocker initialized');
        
                            // 1. HIDE SHORTS (Safe - doesn't affect video player)
                            const hideShorts = () => {
                                const style = document.createElement('style');
                                style.id = 'aknova-shorts-blocker';
                                style.textContent = `
                                    ytd-reel-shelf-renderer,
                                    [is-shorts],
                                    ytd-guide-entry-renderer:has([title=""Shorts""]),
                                    ytd-mini-guide-entry-renderer:has([title=""Shorts""]) {
                                        display: none !important;
                                    }
                                `;
                                if (!document.getElementById('aknova-shorts-blocker')) {
                                    document.head.appendChild(style);
                                }
                            };
        
                            // 2. BLOCK VIDEO ADS (Precise targeting)
                            const blockVideoAds = () => {
                                // Skip button auto-click
                                const skipSelectors = [
                                    '.ytp-ad-skip-button',
                                    '.ytp-ad-skip-button-modern',
                                    '.ytp-skip-ad-button'
                                ];
            
                                skipSelectors.forEach(selector => {
                                    const skipBtn = document.querySelector(selector);
                                    if (skipBtn && skipBtn.offsetParent !== null) {
                                        skipBtn.click();
                                        console.log('[AKNOVA] Ad skip button clicked');
                                    }
                                });
            
                                // Remove ONLY ad overlays (not video player)
                                const adOverlaySelectors = [
                                    '.ytp-ad-overlay-container',
                                    '.ytp-ad-text-overlay',
                                    '.ytp-ad-image-overlay'
                                ];
            
                                adOverlaySelectors.forEach(selector => {
                                    document.querySelectorAll(selector).forEach(el => {
                                        if (el && !el.closest('.html5-video-player')) {
                                            el.remove();
                                        }
                                    });
                                });
            
                                // Fast-forward through ads
                                const video = document.querySelector('video');
                                const adIndicator = document.querySelector('.ad-showing, .ytp-ad-player-overlay');
            
                                if (video && adIndicator) {
                                    video.playbackRate = 16;
                                    video.muted = true;
                                    if (video.currentTime < video.duration - 0.5) {
                                        video.currentTime = video.duration - 0.5;
                                    }
                                    console.log('[AKNOVA] Ad fast-forwarded');
                                }
                            };
        
                            // 3. REMOVE ONLY SPECIFIC AD ELEMENTS (NOT video player)
                            const removeAdsOnly = () => {
                                const specificAdSelectors = [
                                    // Sidebar ads
                                    'ytd-display-ad-renderer',
                                    'ytd-promoted-sparkles-web-renderer',
                                    'ytd-ad-slot-renderer',
                
                                    // Banner ads
                                    'ytd-banner-promo-renderer',
                                    'ytd-statement-banner-renderer',
                                    '#masthead-ad',
                
                                    // Companion ads (below video)
                                    'ytd-action-companion-ad-renderer',
                
                                    // Merch shelf
                                    'ytd-merch-shelf-renderer',
                
                                    // Premium prompts
                                    'ytd-unlimited-offer-module-renderer',
                                    'tp-yt-paper-dialog:has(yt-mealbar-promo-renderer)'
                                ];
            
                                specificAdSelectors.forEach(selector => {
                                    document.querySelectorAll(selector).forEach(el => {
                                        // Make sure we're not removing video player
                                        if (!el.closest('#player, #movie_player, .html5-video-player')) {
                                            el.remove();
                                        }
                                    });
                                });
                            };
        
                            // 4. CSS HIDING (Precise selectors)
                            const addAdBlockingCSS = () => {
                                const style = document.createElement('style');
                                style.id = 'aknova-youtube-ads';
                                style.textContent = `
                                    /* ONLY hide specific ad elements, NOT video player */
                
                                    /* Sidebar and banner ads */
                                    ytd-display-ad-renderer,
                                    ytd-promoted-sparkles-web-renderer,
                                    ytd-ad-slot-renderer,
                                    ytd-banner-promo-renderer,
                                    ytd-statement-banner-renderer,
                                    #masthead-ad,
                
                                    /* Companion ads */
                                    ytd-action-companion-ad-renderer,
                
                                    /* Merch and premium */
                                    ytd-merch-shelf-renderer,
                                    ytd-unlimited-offer-module-renderer,
                                    tp-yt-paper-dialog:has(yt-mealbar-promo-renderer),
                
                                    /* Ad overlay text */
                                    .ytp-ad-text,
                                    .ytp-ad-preview-text,
                
                                    /* Visit advertiser button */
                                    .ytp-ad-button-icon {
                                        display: none !important;
                                    }
                
                                    /* DO NOT hide video player elements */
                                    #player,
                                    #movie_player,
                                    .html5-video-player,
                                    .html5-main-video {
                                        display: block !important;
                                    }
                                `;
            
                                if (!document.getElementById('aknova-youtube-ads')) {
                                    document.head.appendChild(style);
                                }
                            };
        
                            // 5. Shorts URL redirect
                            document.addEventListener('click', e => {
                                const link = e.target.closest('a');
                                if (link?.href?.includes('/shorts/')) {
                                    e.preventDefault();
                                    const id = link.href.split('/shorts/')[1].split('?')[0];
                                    window.location.href = 'https://www.youtube.com/watch?v=' + id;
                                }
                            }, true);
        
                            // Initialize
                            hideShorts();
                            addAdBlockingCSS();
                            removeAdsOnly();
        
                            // Monitor for ads every second
                            setInterval(() => {
                                blockVideoAds();
                                removeAdsOnly();
                            }, 1000);
        
                            // Watch for dynamic content
                            const observer = new MutationObserver(() => {
                                hideShorts();
                                removeAdsOnly();
                            });
        
                            if (document.body) {
                                observer.observe(document.body, {
                                    childList: true,
                                    subtree: true
                                });
                            }
                        }
    
                        // ===== GENERAL SITES - SAFE AD BLOCKING =====
                        const safeAdBlocker = () => {
                            // ONLY block clearly identified ad containers
                            const safeAdSelectors = [
                                // Iframes containing ads
                                'iframe[src*=""doubleclick""]',
                                'iframe[src*=""googlesyndication""]',
                                'iframe[src*=""googleadservices""]',
            
                                // Common ad container IDs (exact matches)
                                '#google_ads_iframe',
                                '#aswift',
                                '[id^=""google_ads_iframe""]',
            
                                // Specific ad classes
                                '.adsbygoogle',
                                '.advertisement',
                                '.ad-container',
                                '.ad-wrapper',
                                '.sponsored-content',
            
                                // Pop-ups
                                '.popup-ad',
                                '.overlay-ad'
                            ];
        
                            const style = document.createElement('style');
                            style.id = 'aknova-safe-adblocker';
                            style.textContent = safeAdSelectors.join(',\n') + ' { display: none !important; }';
        
                            if (!document.getElementById('aknova-safe-adblocker')) {
                                document.head.appendChild(style);
                            }
                        };
    
                        if (!window.location.hostname.includes('youtube.com')) {
                            safeAdBlocker();
                        }
    
                        // ===== NEW TAB SUPPORT =====
                        document.addEventListener('click', function(e) {
                            const link = e.target.closest('a');
                            if (link && link.href && (e.ctrlKey || e.button === 1)) {
                                e.preventDefault();
                                window.open(link.href, '_blank');
                            }
                        }, true);
    
                        document.addEventListener('auxclick', function(e) {
                            if (e.button === 1) {
                                const link = e.target.closest('a');
                                if (link && link.href) {
                                    e.preventDefault();
                                    window.open(link.href, '_blank');
                                }
                            }
                        }, true);
    
                    })();
                ";

                await GetActiveBrowser().ExecuteScriptAsync(script);
                System.Diagnostics.Debug.WriteLine("[AKNOVA] Scripts injected successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Script injection failed: {ex.Message}");
            }
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

        private void SwitchToNextTab()
        {
            var tabIds = tabs.Keys.ToList();
            var currentIndex = tabIds.IndexOf(activeTabId);
            var nextIndex = (currentIndex + 1) % tabIds.Count;
            SwitchToTab(tabIds[nextIndex]);
        }

        private void SwitchToPreviousTab()
        {
            var tabIds = tabs.Keys.ToList();
            var currentIndex = tabIds.IndexOf(activeTabId);
            var prevIndex = currentIndex - 1;
            if (prevIndex < 0) prevIndex = tabIds.Count - 1;
            SwitchToTab(tabIds[prevIndex]);
        }

        private void SwitchToTabByIndex(int index)
        {
            var tabIds = tabs.Keys.ToList();
            if (index < tabIds.Count)
            {
                SwitchToTab(tabIds[index]);
            }
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

            // ADD THIS - Show blocked URLs
            menu.Items.Add(CreateMenuItem("View Blocked Ads", (s, a) =>
            {
                MessageBox.Show(
                    $"Total Ads Blocked: {adsBlocked}\n\n" +
                    $"Ad Blocker: {(adBlockEnabled ? "ENABLED ✅" : "DISABLED ❌")}\n\n" +
                    $"Blocking:\n" +
                    $"• Ad networks\n" +
                    $"• Trackers\n" +
                    $"• YouTube ads\n" +
                    $"• Pop-ups\n" +
                    $"• Analytics",
                    "Ad Blocker Stats"
                );
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