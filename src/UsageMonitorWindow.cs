using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace UsageMonitorQuickView;

public sealed class UsageMonitorWindow : Window
{
    private readonly UsageMonitorSettings settings;
    private readonly WebView2 browser = new();
    private readonly TextBlock status = new() { Text = "연결 준비 중…", FontSize = 11, Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center };
    private readonly CancellationTokenSource lifetime = new();
    private bool busy, closing;
    private TaskCompletionSource<bool>? navigation;
    private readonly bool demo;
    public Task<bool> Ready { get; private set; } = Task.FromResult(false);

    public UsageMonitorWindow(UsageMonitorSettings settings, string dataDirectory, bool demo = false)
    {
        this.settings = settings; this.demo = demo;
        Title = "Usage Monitor"; Width = 900; Height = 700; MinWidth = 520; MinHeight = 400;
        ShowInTaskbar = false; Background = Brushes.White;
        var area = SystemParameters.WorkArea;
        Width = Math.Min(Width, area.Width - 16); Height = Math.Min(Height, area.Height - 16);
        Left = area.Left + 8; Top = Math.Max(area.Top + 8, area.Bottom - Height - 8);
        var dock = new DockPanel(); Content = dock;
        var bar = new DockPanel { Margin = new Thickness(14, 8, 10, 8), LastChildFill = true };
        DockPanel.SetDock(bar, Dock.Top); dock.Children.Add(bar);
        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        DockPanel.SetDock(actions, Dock.Right); bar.Children.Add(actions);
        var refresh = new Button { Content = "새로고침", Margin = new Thickness(4, 0, 4, 0), Padding = new Thickness(10, 5, 10, 5) };
        refresh.Click += async (_, _) => await LoadPageAsync(); actions.Children.Add(refresh);
        var external = new Button { Content = "브라우저로 열기", Padding = new Thickness(10, 5, 10, 5) };
        external.Click += async (_, _) => await OpenBrowserAsync(); actions.Children.Add(external);
        bar.Children.Add(status);
        dock.Children.Add(browser);
        browser.CreationProperties = new CoreWebView2CreationProperties { UserDataFolder = Path.Combine(dataDirectory, "UsageMonitorBrowser") };
        Closing += (_, e) => { if (!closing) { e.Cancel = true; Hide(); } };
    }

    public void Toggle()
    {
        if (IsVisible) { Hide(); return; }
        Show(); Activate();
        Ready = LoadPageAsync();
    }

    private async Task<bool> LoadPageAsync()
    {
        if (busy || closing) return false;
        busy = true; status.Text = "서버에 연결 중…";
        try
        {
            var address = demo ? null : await DashboardLauncher.ResolveAsync(settings, lifetime.Token);
            if (browser.CoreWebView2 is null)
            {
                await browser.EnsureCoreWebView2Async();
                if (closing || browser.CoreWebView2 is null) return false;
                browser.CoreWebView2.Settings.IsStatusBarEnabled = false;
                browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
                browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                browser.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
                browser.CoreWebView2.NavigationCompleted += (_, e) => navigation?.TrySetResult(e.IsSuccess);
                browser.CoreWebView2.NewWindowRequested += (_, e) => { e.Handled = true; status.Text = "외부 링크는 브라우저로 열기에서 이용하세요."; };
                browser.CoreWebView2.DownloadStarting += (_, e) => e.Cancel = true;
                browser.CoreWebView2.NavigationStarting += (_, e) =>
                {
                    // NavigateToString uses an SDK-owned document address; allow it only for our built-in demo.
                    if (!demo && e.Uri != "about:blank" && (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var target) || target.Scheme is not ("http" or "https"))) e.Cancel = true;
                };
            }
            navigation = new(TaskCreationOptions.RunContinuationsAsynchronously);
            if (demo) browser.NavigateToString("<!doctype html><html><meta charset='utf-8'><style>body{font:16px Segoe UI;background:#fafafc;color:#30323b;margin:40px}small{color:#858591}.grid{display:flex;gap:16px}section{background:white;padding:24px;border:1px solid #e8e8ee;border-radius:12px;flex:1}h1{font-size:28px}b{font-size:32px}</style><h1>Usage Monitor</h1><p><small>예시 화면 · 외부 서버에 연결하지 않습니다</small></p><div class='grid'><section>Personal<p><b>42.8M</b></p>오늘 토큰</section><section>Research<p><b>18.6M</b></p>오늘 토큰</section><section>Backup<p><b>3.2M</b></p>오늘 토큰</section></div></html>");
            else browser.CoreWebView2.Navigate(address!.AbsoluteUri);
            var loaded = await navigation.Task.WaitAsync(TimeSpan.FromSeconds(30), lifetime.Token);
            status.Text = loaded ? "Usage Monitor · 연결됨" : "페이지를 열지 못했습니다 · 새로고침으로 다시 시도";
            return loaded;
        }
        catch (WebView2RuntimeNotFoundException) { status.Text = "WebView2 Runtime 설치가 필요합니다 · 브라우저로 열기 사용 가능"; return false; }
        catch (OperationCanceledException) { return false; }
        catch (Exception) { status.Text = "연결하지 못했습니다 · 기존 바로가기로 로그인·SSH 확인 후 다시 시도"; return false; }
        finally { busy = false; }
    }

    private async Task OpenBrowserAsync()
    {
        if (demo) { status.Text = "데모 화면은 외부 사이트를 열지 않습니다."; return; }
        try
        {
            var address = await DashboardLauncher.ResolveAsync(settings, lifetime.Token);
            Process.Start(new ProcessStartInfo(address.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception) { status.Text = "브라우저를 열지 못했습니다 · 기존 바로가기로 연결을 확인하세요."; }
    }

    internal async Task<bool> InspectPageAsync(string directory)
    {
        if (browser.CoreWebView2 is null) return false;
        var connected = await browser.ExecuteScriptAsync(demo ? "document.body.innerText.includes('예시 화면')" : "document.body.innerText.includes('모니터 서버 연결됨')");
        await using var file = File.Create(Path.Combine(directory, "usage-page.png"));
        await browser.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, file);
        return connected == "true";
    }

    public void DisposeWindow()
    {
        closing = true; lifetime.Cancel(); browser.Dispose(); Close(); lifetime.Dispose();
    }
}
