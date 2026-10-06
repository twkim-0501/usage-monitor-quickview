using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UsageMonitorQuickView;

public sealed class UsageMonitorSettings
{
    public string? Url { get; set; }
    public string? HelperExecutable { get; set; }
    public List<string> HelperArguments { get; set; } = [];
    public bool StartWithWindows { get; set; }
    public bool DockButton { get; set; } = true;
    [JsonIgnore] public bool IsConfigured => !string.IsNullOrWhiteSpace(Url) || !string.IsNullOrWhiteSpace(HelperExecutable);
}

public sealed class SettingsStore
{
    public string DirectoryPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UsageMonitorQuickView");
    public string SettingsPath { get; }
    public SettingsStore(string? customPath = null) { SettingsPath = customPath ?? Path.Combine(DirectoryPath, "settings.json"); }
    public UsageMonitorSettings Load() => File.Exists(SettingsPath)
        ? JsonSerializer.Deserialize<UsageMonitorSettings>(File.ReadAllText(SettingsPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new() : new();
    public void Save(UsageMonitorSettings value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath + ".tmp", JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(SettingsPath + ".tmp", SettingsPath, overwrite: true);
    }
}
