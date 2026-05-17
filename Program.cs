using System;
using System.Threading;
using System.Windows.Forms;
using AudioSwitcher;

using var mutex = new Mutex(true, "AudioSwitcher_SingleInstance", out bool isNew);
if (!isNew)
{
    MessageBox.Show("AudioSwitcher가 이미 실행 중입니다.",
                    "AudioSwitcher", MessageBoxButtons.OK, MessageBoxIcon.Information);
    return;
}

Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
Application.Run(new TrayApp(AppSettings.Load()));
