using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;

namespace AKNOVABROW
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int adsBlocked;
        private bool adBlockEnabled;
        private List<string> adPatterns;
        //private int adsBlocked;
        //private bool adsBlockEnabled;
        //private readonly List<string> adPatterns;
        //{
        //    "doubleclick.net", "googlesyndication.com", "googleadservices.com",
        //    "youtube.com/api/stats/ads", "youtube.com/ptracking", "youtube.com/pagead",
        //    "google-analytics.com", "googletagmanager.com", "/ads/", "advertising"
        //};
        //public MainWindow()
        //{
        //    InitializeComponent();
        //    InitBrowser();
        //}
        public MainWindow()
        {
            InitializeComponent();

            // Initialize variables
            adsBlocked = 0;
            adBlockEnabled = true;
            adPatterns = new List<string>
            {
                "doubleclick.net", "googlesyndication.com", "googleadservices.com",
                "youtube.com/api/stats/ads", "youtube.com/ptracking", "youtube.com/pagead",
                "google-analytics.com", "googletagmanager.com", "/ads/", "advertising"
            };

            InitBrowser();
        }

        private async void InitBrowser()
        {
            try
            {
                await Browser.EnsureCoreWebView2Async();
                Browser.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
                Browser.CoreWebView2.SourceChanged += OnSourceChanged;
                Browser.CoreWebView2.WebResourceRequested += OnWebResourceRequested;
                Browser.CoreWebView2.NewWindowRequested += OnNewWindowRequested;
                Browser.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
                StatusText.Text = "Welcome to AKNOVA Browser";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            StatusText.Text = "Ready";
            UpdateNavButtons();
            await InjectScripts();
        }

        private void OnSourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
        {
            AddressBar.Text = Browser.Source?.ToString() ?? "";
            SecureIcon.Text = Browser.Source?.ToString().StartsWith("https://") == true ? "🔒" : "⚠️";
            Title = $"{Browser.CoreWebView2?.DocumentTitle ?? "AKNOVA"} - AKNOVA Browser";
        }

        private void OnWebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            if (!adBlockEnabled) return;
            if (adPatterns.Any(p => e.Request.Uri.ToLower().Contains(p)))
            {
                adsBlocked++;
                AdCountText.Text = adsBlocked.ToString();
                StatsText.Text = $"Ads: {adsBlocked}";
                e.Response = Browser.CoreWebView2.Environment.CreateWebResourceResponse(null, 403, "Blocked", "");
            }
        }

        private void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;
            Browser.CoreWebView2.Navigate(e.Uri);
        }

        private async System.Threading.Tasks.Task InjectScripts()
        {
            try
            {
                await Browser.ExecuteScriptAsync(@"
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

        private void BackButton_Click(object sender, RoutedEventArgs e) => Browser.GoBack();
        private void ForwardButton_Click(object sender, RoutedEventArgs e) => Browser.GoForward();
        private void RefreshButton_Click(object sender, RoutedEventArgs e) => Browser.Reload();
        private void HomeButton_Click(object sender, RoutedEventArgs e) => Browser.Source = new Uri("https://www.google.com");
        private void GoButton_Click(object sender, RoutedEventArgs e) => Navigate(AddressBar.Text);
        private void AddressBar_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) Navigate(AddressBar.Text); }

        private void Navigate(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            url = url.Trim();
            if (!url.StartsWith("http")) url = url.Contains(".") ? "https://" + url : "https://www.google.com/search?q=" + Uri.EscapeDataString(url);
            if (url.Contains("/shorts/")) url = "https://www.youtube.com/watch?v=" + url.Split("/shorts/")[1].Split('?')[0];
            Browser.Source = new Uri(url);
        }

        private void AdBlockButton_Click(object sender, RoutedEventArgs e)
        {
            adBlockEnabled = !adBlockEnabled;
            MessageBox.Show(adBlockEnabled ? "Enabled" : "Disabled", "Ad Blocker");
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            var m = new ContextMenu();
            m.Items.Add(CreateMenuItem("New Window", (s, a) => new MainWindow().Show()));
            m.Items.Add(new Separator());
            m.Items.Add(CreateMenuItem("Zoom +", (s, a) => Browser.ZoomFactor += 0.1));
            m.Items.Add(CreateMenuItem("Zoom -", (s, a) => Browser.ZoomFactor -= 0.1));
            m.Items.Add(CreateMenuItem("Zoom Reset", (s, a) => Browser.ZoomFactor = 1.0));
            m.Items.Add(new Separator());
            m.Items.Add(CreateMenuItem("Clear Cache", async (s, a) => { await Browser.CoreWebView2.Profile.ClearBrowsingDataAsync(); MessageBox.Show("Done!"); }));
            m.Items.Add(new Separator());
            m.Items.Add(CreateMenuItem("Stats", (s, a) => MessageBox.Show($"Ads: {adsBlocked}\nZoom: {Browser.ZoomFactor * 100:F0}%")));
            m.Items.Add(CreateMenuItem("About", (s, a) => MessageBox.Show("AKNOVA Browser v1.0\n\n© 2025")));
            m.PlacementTarget = MenuButton;
            m.IsOpen = true;
        }

        private MenuItem CreateMenuItem(string h, RoutedEventHandler r) { var i = new MenuItem { Header = h }; i.Click += r; return i; }
        private void UpdateNavButtons() { BackButton.IsEnabled = Browser.CanGoBack; ForwardButton.IsEnabled = Browser.CanGoForward; }
    }
}