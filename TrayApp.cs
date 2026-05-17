using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AutoAudioSwitch;

internal sealed class TrayApp : Form
{
    // ── WinAPI ──────────────────────────────────────────────────────────────
    [DllImport("user32.dll")] private static extern bool RegisterHotKey(
        IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(
        IntPtr hWnd, int id);

    private const int WM_HOTKEY  = 0x0312;
    private const int HOTKEY_ID  = 9001;

    // ── Fields ───────────────────────────────────────────────────────────────
    private readonly AppSettings        _settings;
    private readonly AudioDeviceManager _manager  = new();
    private readonly NotifyIcon         _tray;
    private readonly ContextMenuStrip   _menu;
    private SettingsForm?               _settingsForm;

    // ── Constructor ───────────────────────────────────────────────────────────
    public TrayApp(AppSettings settings)
    {
        _settings = settings;

        Text            = "AutoAudioSwitch";
        ShowInTaskbar   = false;
        WindowState     = FormWindowState.Minimized;
        FormBorderStyle = FormBorderStyle.None;
        Size            = new Size(1, 1);

        _menu = BuildMenu();
        _tray = new NotifyIcon
        {
            Icon             = CreateIcon(),
            Text             = "Audio Switcher\n우클릭: 장치 선택\n좌클릭: 다음 장치",
            ContextMenuStrip = _menu,
            Visible          = true,
        };
        _tray.MouseClick += OnTrayClick;
        _menu.Opening    += OnMenuOpening;
    }

    // ── 핸들 생성 + 핫키 등록 ────────────────────────────────────────────────
    protected override void SetVisibleCore(bool value)
    {
        if (!IsHandleCreated)
        {
            CreateHandle();
            RegisterCurrentHotKey();
        }
        base.SetVisibleCore(false);
    }

    // ── 시작 시 설정창 자동 오픈 ─────────────────────────────────────────────
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        BeginInvoke(OpenSettings);
    }

    // ── Tray 이벤트 ─────────────────────────────────────────────────────────
    private void OnTrayClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
            CycleAndNotify();
    }

    private void OnMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // 장치 목록 항목만 다시 채움 (고정 항목은 유지)
        while (_menu.Items[0] is ToolStripMenuItem m && m.Tag is string)
            _menu.Items.RemoveAt(0);

        var devices   = _manager.GetPlaybackDevices();
        var defaultId = _manager.GetDefaultDeviceId();

        for (int i = devices.Count - 1; i >= 0; i--)
        {
            var dev  = devices[i];
            var item = new ToolStripMenuItem(dev.Name)
            {
                Tag          = dev.Id,
                Checked      = dev.Id == defaultId,
                CheckOnClick = false,
            };
            item.Click += OnDeviceItemClick;
            _menu.Items.Insert(0, item);
        }
    }

    private void OnDeviceItemClick(object? sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem { Tag: string id })
            SwitchTo(id);
    }

    // ── 핫키 (WM_HOTKEY) ─────────────────────────────────────────────────────
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            CycleAndNotify();
        base.WndProc(ref m);
    }

    // ── 전환 로직 ─────────────────────────────────────────────────────────────
    private void CycleAndNotify()
    {
        try
        {
            var dev = _manager.CycleToNextDevice();
            if (dev is not null) ShowSuccess(dev.Name);
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void SwitchTo(string deviceId)
    {
        try
        {
            _manager.SetDefaultDevice(deviceId);
            var name = _manager.GetPlaybackDevices().Find(d => d.Id == deviceId)?.Name ?? deviceId;
            ShowSuccess(name);
        }
        catch (Exception ex) { ShowError(ex); }
    }

    // ── 설정 창 ──────────────────────────────────────────────────────────────
    public void OpenSettings()
    {
        // 이미 열려 있으면 포커스만 이동
        if (_settingsForm is { IsDisposed: false })
        {
            _settingsForm.Activate();
            return;
        }

        _settingsForm = new SettingsForm(_settings);
        _settingsForm.SettingsApplied += OnSettingsApplied;
        _settingsForm.Show(); // 비모달 — 트레이 조작과 병행 가능
    }

    private void OnSettingsApplied()
    {
        RegisterCurrentHotKey();
        UpdateMenuHotkeyLabel();
    }

    // ── 핫키 등록/재등록 ─────────────────────────────────────────────────────
    private void RegisterCurrentHotKey()
    {
        UnregisterHotKey(Handle, HOTKEY_ID);
        bool ok = RegisterHotKey(Handle, HOTKEY_ID,
                                 _settings.HotkeyModifiers,
                                 _settings.HotkeyVirtualKey);
        if (!ok)
            _tray.ShowBalloonTip(3000, "경고",
                $"단축키({_settings.FormatHotkey()}) 등록 실패.\n다른 프로그램과 충돌합니다.",
                ToolTipIcon.Warning);
    }

    // ── 알림 ─────────────────────────────────────────────────────────────────
    private void ShowSuccess(string deviceName)
    {
        var display = deviceName.Length > 63 ? deviceName[..60] + "..." : deviceName;
        _tray.Text  = $"Audio: {display}";
        _tray.ShowBalloonTip(2500, "오디오 장치 전환", deviceName, ToolTipIcon.Info);
    }

    private void ShowError(Exception ex)
    {
        string detail = ex is COMException ce
            ? $"COM 오류 0x{ce.HResult:X8}\n{ce.Message}"
            : ex.Message;
        _tray.ShowBalloonTip(5000, "전환 실패", detail, ToolTipIcon.Error);
    }

    // ── 정리 ──────────────────────────────────────────────────────────────────
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        UnregisterHotKey(Handle, HOTKEY_ID);
        _tray.Visible = false;
        _tray.Dispose();
        _manager.Dispose();
        base.OnFormClosed(e);
    }

    // ── 메뉴 ─────────────────────────────────────────────────────────────────
    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();

        // 장치 목록은 Opening 이벤트에서 동적으로 채움
        menu.Items.Add(new ToolStripSeparator());

        var hotkeyLabel = new ToolStripMenuItem($"단축키: {_settings.FormatHotkey()}")
        {
            Enabled = false,
            Name    = "hotkeyLabel",
        };
        menu.Items.Add(hotkeyLabel);

        var settingsItem = new ToolStripMenuItem("설정...");
        settingsItem.Click += (_, _) => OpenSettings();
        menu.Items.Add(settingsItem);

        menu.Items.Add(new ToolStripSeparator());

        var exit = new ToolStripMenuItem("종료 (Exit)");
        exit.Click += (_, _) => Application.Exit();
        menu.Items.Add(exit);

        return menu;
    }

    private void UpdateMenuHotkeyLabel()
    {
        if (_menu.Items["hotkeyLabel"] is ToolStripMenuItem item)
            item.Text = $"단축키: {_settings.FormatHotkey()}";
    }

    // ── 트레이 아이콘 ─────────────────────────────────────────────────────────
    private static Icon CreateIcon()
    {
        using var bmp = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g   = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        using var bg = new SolidBrush(Color.FromArgb(0, 120, 215));
        g.FillEllipse(bg, 0, 0, 31, 31);

        using var white = new SolidBrush(Color.White);
        Point[] body = [
            new(6, 11), new(13, 11), new(19, 5),
            new(19, 27), new(13, 21), new(6, 21),
        ];
        g.FillPolygon(white, body);

        using var pen1 = new Pen(Color.White, 2f);
        g.DrawArc(pen1, 20, 9, 5, 14, -70, 140);
        using var pen2 = new Pen(Color.White, 2f);
        g.DrawArc(pen2, 22, 5, 7, 22, -70, 140);

        return Icon.FromHandle(bmp.GetHicon());
    }
}
