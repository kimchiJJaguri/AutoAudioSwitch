using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace AutoAudioSwitch;

internal sealed class AppSettings
{
    // WinAPI modifier flags
    internal const uint MOD_ALT     = 0x0001;
    internal const uint MOD_CONTROL = 0x0002;
    internal const uint MOD_SHIFT   = 0x0004;
    internal const uint MOD_WIN     = 0x0008;

    public uint HotkeyModifiers  { get; set; } = MOD_CONTROL | MOD_ALT;
    public uint HotkeyVirtualKey { get; set; } = 0x7A; // VK_F11

    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AutoAudioSwitch", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new();
        }
        catch { }
        return new();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this,
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    public string FormatHotkey()
    {
        var parts = new List<string>();
        if ((HotkeyModifiers & MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((HotkeyModifiers & MOD_ALT)     != 0) parts.Add("Alt");
        if ((HotkeyModifiers & MOD_SHIFT)   != 0) parts.Add("Shift");
        if ((HotkeyModifiers & MOD_WIN)     != 0) parts.Add("Win");
        parts.Add(((Keys)HotkeyVirtualKey).ToString());
        return string.Join(" + ", parts);
    }
}
