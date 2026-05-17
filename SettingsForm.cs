using System;
using System.Drawing;
using System.Windows.Forms;

namespace AutoAudioSwitch;

internal sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly TextBox     _hotkeyBox;
    private readonly Button      _changeBtn;
    private readonly Label       _hintLabel;
    private readonly Button      _saveBtn;

    private bool _capturing;
    private uint _pendingModifiers;
    private uint _pendingVk;

    // TrayApp이 구독해서 핫키를 재등록
    public event Action? SettingsApplied;

    public SettingsForm(AppSettings settings)
    {
        _settings = settings;

        // ── 폼 기본 설정 ──────────────────────────────────────────────────
        Text            = "AutoAudioSwitch 설정";
        ClientSize      = new Size(420, 195);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        StartPosition   = FormStartPosition.CenterScreen;
        KeyPreview      = true; // ProcessCmdKey가 모든 키 이벤트를 받도록
        Font            = new Font("Segoe UI", 9.5f);
        BackColor       = Color.White;

        // ── 단축키 섹션 레이블 ────────────────────────────────────────────
        var sectionLabel = new Label
        {
            Text      = "장치 순환 단축키",
            Location  = new Point(24, 22),
            AutoSize  = true,
            Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 30, 30),
        };

        // ── 핫키 표시 박스 ────────────────────────────────────────────────
        _hotkeyBox = new TextBox
        {
            ReadOnly   = true,
            Text       = _settings.FormatHotkey(),
            Location   = new Point(24, 50),
            Width      = 270,
            Height     = 32,
            Font       = new Font("Segoe UI", 11f),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor  = Color.FromArgb(245, 245, 245),
            Cursor     = Cursors.Arrow,
            TabStop    = false,
        };

        // ── 변경 버튼 ────────────────────────────────────────────────────
        _changeBtn = new Button
        {
            Text      = "변경",
            Location  = new Point(308, 48),
            Size      = new Size(88, 34),
            FlatStyle = FlatStyle.System,
        };
        _changeBtn.Click += (_, _) => StartCapture();

        // ── 힌트 레이블 ──────────────────────────────────────────────────
        _hintLabel = new Label
        {
            Text      = "변경 버튼을 클릭한 뒤 원하는 키 조합을 누르세요.  (ESC: 취소)",
            Location  = new Point(24, 92),
            Size      = new Size(375, 38),
            ForeColor = Color.Gray,
            Font      = new Font("Segoe UI", 8.5f),
        };

        // ── 구분선 ───────────────────────────────────────────────────────
        var separator = new Label
        {
            Location    = new Point(0, 138),
            Size        = new Size(420, 1),
            BorderStyle = BorderStyle.Fixed3D,
        };

        // ── 저장 / 닫기 버튼 ──────────────────────────────────────────────
        _saveBtn = new Button
        {
            Text      = "저장",
            Location  = new Point(220, 152),
            Size      = new Size(88, 32),
            FlatStyle = FlatStyle.System,
        };
        _saveBtn.Click += SaveBtn_Click;

        var closeBtn = new Button
        {
            Text      = "닫기",
            Location  = new Point(318, 152),
            Size      = new Size(88, 32),
            FlatStyle = FlatStyle.System,
        };
        closeBtn.Click += (_, _) => Close();

        AcceptButton = _saveBtn;

        Controls.AddRange([sectionLabel, _hotkeyBox, _changeBtn,
                           _hintLabel, separator, _saveBtn, closeBtn]);
    }

    // ── 캡처 시작 ─────────────────────────────────────────────────────────
    private void StartCapture()
    {
        _capturing        = true;
        _pendingModifiers = 0;
        _pendingVk        = 0;

        _hotkeyBox.BackColor = Color.FromArgb(255, 252, 210);
        _hotkeyBox.Text      = "키를 누르세요...";
        _changeBtn.Enabled   = false;
        _saveBtn.Enabled     = false;
        _hintLabel.Text      = "Ctrl / Alt / Shift 와 함께 키를 입력하세요.   (ESC: 취소)";
        _hintLabel.ForeColor = Color.FromArgb(0, 100, 180);
    }

    // ── 캡처 종료 ─────────────────────────────────────────────────────────
    private void EndCapture(bool apply)
    {
        _capturing = false;
        _changeBtn.Enabled   = true;
        _saveBtn.Enabled     = true;
        _hotkeyBox.BackColor = Color.FromArgb(245, 245, 245);
        _hintLabel.ForeColor = Color.Gray;
        _hintLabel.Text      = "변경 버튼을 클릭한 뒤 원하는 키 조합을 누르세요.  (ESC: 취소)";

        if (apply && _pendingVk != 0)
        {
            _settings.HotkeyModifiers  = _pendingModifiers;
            _settings.HotkeyVirtualKey = _pendingVk;
        }
        _hotkeyBox.Text = _settings.FormatHotkey();
    }

    // ── 키 캡처 (폼 레벨에서 모든 키 이벤트 가로챔) ───────────────────────
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (!_capturing) return base.ProcessCmdKey(ref msg, keyData);

        var keyCode = keyData & Keys.KeyCode;
        var modKeys = keyData & Keys.Modifiers;

        // ESC → 취소
        if (keyCode == Keys.Escape)
        {
            EndCapture(apply: false);
            return true;
        }

        // 수정자 키만 눌린 경우 → 무시, 계속 대기
        if (keyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu
                    or Keys.LWin     or Keys.RWin    or Keys.Apps)
            return true;

        // 수정자 없이 일반 문자/숫자 키 → 무시 (함수 키는 허용)
        bool isFunctionKey = keyCode >= Keys.F1 && keyCode <= Keys.F24;
        if (modKeys == Keys.None && !isFunctionKey)
            return true;

        // WinAPI 형식으로 변환
        _pendingModifiers = 0;
        if (modKeys.HasFlag(Keys.Control)) _pendingModifiers |= AppSettings.MOD_CONTROL;
        if (modKeys.HasFlag(Keys.Alt))     _pendingModifiers |= AppSettings.MOD_ALT;
        if (modKeys.HasFlag(Keys.Shift))   _pendingModifiers |= AppSettings.MOD_SHIFT;
        _pendingVk = (uint)keyCode;

        EndCapture(apply: true);
        return true;
    }

    // ── 저장 ──────────────────────────────────────────────────────────────
    private void SaveBtn_Click(object? sender, EventArgs e)
    {
        if (_capturing) EndCapture(apply: false);
        _settings.Save();
        SettingsApplied?.Invoke();
        Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_capturing) EndCapture(apply: false);
        base.OnFormClosing(e);
    }
}
