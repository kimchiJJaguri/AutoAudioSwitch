> 🇰🇷 [한국어 버전 보기](README.ko.md)
# 🔊 AutoAudioSwitch

> A lightweight Windows utility that lets you switch audio output devices instantly from the system tray.

![C#](https://img.shields.io/badge/C%23-.NET_10-purple?logo=csharp) ![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey?logo=windows) ![License](https://img.shields.io/badge/License-MIT-blue)

---

## ✨ Features

| Action | Description |
|--------|-------------|
| 🖱️ Left click tray icon | Instantly cycle to the next audio device |
| 🖱️ Right click tray icon | Select a device directly from the list |
| ⌨️ Global hotkey | Cycle devices from anywhere (default: `Ctrl + Alt + F11`) |
| ⚙️ Custom hotkey | Set your own key combination via the settings window |

Settings are automatically saved to `%AppData%\AutoAudioSwitch\settings.json`

---

## 🚀 How to Use

1. Run `AutoAudioSwitch.exe`
2. The app will appear in your system tray
3. **Left click** to switch to the next device, or **right click** to choose one
4. To change the hotkey: right click → Settings → click "Change" → press your key combo → Save

---

## 🛠️ Build from Source

**.NET 10 SDK** or higher required

```bash
dotnet publish -c Release
```

Output: `bin\Release\net10.0-windows\win-x64\publish\AutoAudioSwitch.exe`

Single executable — no external dependencies.

---

## 💡 Background

Built as a personal utility to replace third-party audio switcher apps. Wanted something minimal, fast, and fully customizable — so I made it.

---

## 🔧 Tech Stack

- **Language:** C# / .NET 10 / WinForms
- **Audio API:** Windows Core Audio API (`IMMDeviceEnumerator`)
- **Device switching:** `IPolicyConfig` COM interface (undocumented API)
- **Hotkey:** `RegisterHotKey` WinAPI
