using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace UsageMonitorQuickView;

public partial class App : Application
{
    private Mutex? instance;
    private TaskbarButton? button;
    private Forms.NotifyIcon? tray;
    private UsageMonitorWindow? window;
    private SettingsStore store = new();
    private UsageMonitorSettings settings = new();
    private bool demo;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) => { MessageBox.Show("연결을 확인한 뒤 다시 시도하세요.", "Usage Monitor Quick View"); args.Handled = true; };
        demo = e.Args.Contains("--demo");
        var checkIndex = Array.IndexOf(e.Args, "--check");
        var checkDirectory = checkIndex >= 0 && checkIndex + 1 < e.Args.Length ? Path.GetFullPath(e.Args[checkIndex + 1]) : null;
        if (!(demo && checkDirectory is not null))
        {
            instance = new Mutex(true, "Local\\UsageMonitorQuickView", out var created);
            if (!created) { MessageBox.Show("이미 실행 중입니다. 계정 위젯 오른쪽의 차트 아이콘을 클릭하세요.", "Usage Monitor Quick View"); Shutdown(); return; }
        }
        var settingsIndex = Array.IndexOf(e.Args, "--settings");
        if (settingsIndex >= 0 && settingsIndex + 1 < e.Args.Length) store = new(e.Args[settingsIndex + 1]);
        try { settings = store.Load(); }
        catch (Exception) { MessageBox.Show("설정 파일을 읽지 못했습니다. 기존 파일은 보존됩니다.", "Usage Monitor Quick View"); Shutdown(1); return; }
        if (demo) settings = new() { Url = "https://example.com" };
        var resource = GetResourceStream(new Uri("pack://application:,,,/app.ico"));
        using var iconStream = resource!.Stream;
        tray = new Forms.NotifyIcon { Icon = new Drawing.Icon(iconStream), Text = "Usage Monitor Quick View", Visible = true };
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Usage Monitor 열기 / 숨기기", null, (_, _) => Dispatcher.Invoke(Toggle));
        menu.Items.Add("설정", null, (_, _) => Dispatcher.Invoke(EditSettings));
        menu.Items.Add("종료", null, (_, _) => Dispatcher.Invoke(() => Shutdown()));
        tray.ContextMenuStrip = menu;
        tray.MouseClick += (_, args) => { if (args.Button == Forms.MouseButtons.Left) Dispatcher.Invoke(Toggle); };
        button = new TaskbarButton();
        button.Click += Toggle;
        button.RightClick += () => menu.Show(Forms.Cursor.Position);
        button.Configure(true, settings.DockButton);
        if (checkDirectory is not null) RunCheck(checkDirectory);
        else if (!settings.IsConfigured) EditSettings();
        else if (e.Args.Contains("--open")) Toggle();
    }

    private void Toggle()
    {
        if (!settings.IsConfigured) { EditSettings(); return; }
        window ??= new(settings, store.DirectoryPath, demo);
        window.Toggle();
    }

    private void EditSettings()
    {
        if (demo) return;
        var editor = new SettingsWindow(settings);
        if (editor.ShowDialog() != true) return;
        store.Save(settings);
        using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        if (settings.StartWithWindows && Environment.ProcessPath is { } path) key.SetValue("UsageMonitorQuickView", "\"" + path + "\"");
        else key.DeleteValue("UsageMonitorQuickView", throwOnMissingValue: false);
        window?.DisposeWindow(); window = null;
        button?.Configure(true, settings.DockButton);
    }

    private async void RunCheck(string directory)
    {
        Directory.CreateDirectory(directory);
        var checks = new Dictionary<string, bool>();
        try
        {
            await Task.Delay(500);
            checks["buttonReceivesPointer"] = button!.HitTestCenter();
            checks["buttonVisible"] = button.CaptureVisible(Path.Combine(directory, "taskbar-button.png"));
            button.SendTestClick(rightButton: true); await Task.Delay(200);
            checks["rightClickOpensMenu"] = tray!.ContextMenuStrip!.Visible;
            tray.ContextMenuStrip.Close();
            button.SendTestClick(); await Task.Delay(300);
            checks["buttonOpensWindow"] = window?.IsVisible == true;
            checks["navigationSucceeded"] = window is not null && await window.Ready;
            if (checks["navigationSucceeded"])
            {
                // Dashboard data arrives asynchronously after the HTML document.
                for (var retry = 0; retry < 8; retry++)
                {
                    await Task.Delay(1000);
                    if (await window!.InspectPageAsync(directory)) { checks["dashboardReportsConnected"] = true; break; }
                }
                checks.TryAdd("dashboardReportsConnected", false);
            }
            window?.Close(); await Task.Delay(150);
            checks["closePreservesIndependentButton"] = window?.IsVisible == false && button.Handle != 0;
            button.SendTestClick(); await Task.Delay(300);
            checks["buttonReopensWindow"] = window?.IsVisible == true;
            if (window is not null) await window.Ready;
            button.SendTestClick(); await Task.Delay(150);
            checks["buttonTogglesWindow"] = window?.IsVisible == false;
            button.Configure(true, false); await Task.Delay(300);
            checks["overlayAvailable"] = !button.IsDocked && button.CaptureVisible(Path.Combine(directory, "overlay-button.png"));
            button.Configure(true, true); await Task.Delay(300);
            File.WriteAllText(Path.Combine(directory, "check.json"), JsonSerializer.Serialize(new { checks, placement = button.Inspect() }, new JsonSerializerOptions { WriteIndented = true }));
            Shutdown(checks.Values.All(x => x) ? 0 : 1);
        }
        catch (Exception)
        {
            File.WriteAllText(Path.Combine(directory, "check-error.txt"), "Quick view check failed. No private URL or helper output recorded."); Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        window?.DisposeWindow(); button?.Dispose(); tray?.Dispose(); instance?.Dispose(); base.OnExit(e);
    }
}
