using System.Diagnostics;
using System.Text.Json;
using UsageMonitorQuickView;

if (args.Contains("--helper"))
{
    Console.WriteLine(args.Contains("--malformed") ? "not json" : JsonSerializer.Serialize(new
    {
        ready = !args.Contains("--failure"),
        url = args.Contains("--external") ? "https://example.com/private-key" : "http://127.0.0.1:18000/_access_launch/fictional-key"
    }));
    return;
}
int count = 0;
void Check(bool value, string label) { if (!value) throw new Exception(label); count++; Console.WriteLine("PASS: " + label); }
Check(DashboardLauncher.ValidateAddress("https://example.com/dashboard").Scheme == "https", "HTTPS dashboard accepted");
foreach (var address in new[] { "file:///C:/private", "javascript:alert(1)", "https://user:password@example.com/" })
{
    try { DashboardLauncher.ValidateAddress(address); throw new Exception("Unsafe address accepted"); }
    catch (InvalidOperationException) { Check(true, "Unsafe scheme or embedded credentials rejected"); }
}
var start = new UsageMonitorSettings { HelperExecutable = Environment.ProcessPath };
if (string.Equals(Path.GetFileNameWithoutExtension(start.HelperExecutable), "dotnet", StringComparison.OrdinalIgnoreCase)) start.HelperArguments.Add(typeof(UsageMonitorSettings).Assembly.Location);
start.HelperArguments.Add("--helper");
Check((await DashboardLauncher.ResolveAsync(start)).IsLoopback, "Local helper handshake accepted");
foreach (var mode in new[] { "--external", "--malformed", "--failure" })
{
    start.HelperArguments.Add(mode);
    try { await DashboardLauncher.ResolveAsync(start); throw new Exception("Invalid helper accepted"); }
    catch (InvalidOperationException error) { Check(!error.Message.Contains("private-key"), "Invalid helper fails without exposing output"); }
    start.HelperArguments.Remove(mode);
}
Console.WriteLine($"{count} checks passed.");
