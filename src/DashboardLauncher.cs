using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.Json;

namespace UsageMonitorQuickView;

/// <summary>Obtains a dashboard address without storing or logging a helper's temporary launch key.</summary>
public static class DashboardLauncher
{
    public static Uri ValidateAddress(string? value, bool localHelper = false)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo)
            || (localHelper && !uri.IsLoopback))
            throw new InvalidOperationException("Usage Monitor 주소를 확인하세요. HTTP/HTTPS 주소만 사용할 수 있습니다.");
        return uri;
    }

    public static async Task<Uri> ResolveAsync(UsageMonitorSettings settings, CancellationToken cancellation = default)
    {
        if (!settings.IsConfigured) throw new InvalidOperationException("설정에서 Usage Monitor 주소를 등록하세요.");
        if (!string.IsNullOrWhiteSpace(settings.Url)) return ValidateAddress(settings.Url);
        if (!Path.IsPathFullyQualified(settings.HelperExecutable!) || !File.Exists(settings.HelperExecutable))
            throw new InvalidOperationException("Usage Monitor 접속 도우미 경로를 확인하세요.");
        var start = new ProcessStartInfo(settings.HelperExecutable!)
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (var argument in settings.HelperArguments) start.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = start };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timeout.CancelAfter(TimeSpan.FromSeconds(40));
        try
        {
            process.Start();
            // The helper's diagnostics are intentionally discarded; they may contain private server information.
            _ = DiscardDiagnosticsAsync(process, timeout.Token);
            var line = await process.StandardOutput.ReadLineAsync(timeout.Token);
            if (line is null || line.Length > 8192) throw new InvalidOperationException();
            using var response = JsonDocument.Parse(line);
            if (!response.RootElement.TryGetProperty("ready", out var ready) || ready.ValueKind != JsonValueKind.True) throw new InvalidOperationException();
            return ValidateAddress(response.RootElement.GetProperty("url").GetString(), localHelper: true);
        }
        catch (Exception)
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            cancellation.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Usage Monitor에 연결하지 못했습니다. 기존 바로가기로 로그인·SSH 연결을 확인한 뒤 다시 시도하세요.");
        }
        // Disposing Process closes our pipe, not the helper. Its shared server manages its own idle lifetime.
    }

    private static async Task DiscardDiagnosticsAsync(Process process, CancellationToken cancellation)
    {
        try { await process.StandardError.ReadToEndAsync(cancellation); }
        catch (Exception) { /* The pipe closes when the helper address has been obtained. */ }
    }
}
