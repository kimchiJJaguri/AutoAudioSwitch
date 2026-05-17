using System;
using System.Threading;
using System.Windows.Forms;
using AutoAudioSwitch;

using var mutex = new Mutex(true, "AutoAudioSwitch_SingleInstance", out bool isNew);
if (!isNew)
{
    MessageBox.Show("AutoAudioSwitch가 이미 실행 중입니다.",
                    "AutoAudioSwitch", MessageBoxButtons.OK, MessageBoxIcon.Information);
    return;
}

Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
Application.Run(new TrayApp(AppSettings.Load()));
