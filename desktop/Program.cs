using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("Shinobu Oshino Pet")]
[assembly: AssemblyDescription("忍野忍 · 独立 Windows 桌宠")]
[assembly: AssemblyProduct("Shinobu Oshino Pet")]
[assembly: AssemblyVersion("2.0.0.0")]
[assembly: AssemblyFileVersion("2.0.0.0")]

namespace ShinobuPet
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool first;
            using (Mutex instance = new Mutex(true, "Local\\ShinobuOshinoPet.v2", out first))
            {
                if (!first)
                {
                    MessageBox.Show("忍已经在桌面上啦。\n请从任务栏右下角的托盘图标找到她，也可以选择「回到主屏幕」。", "忍野忍");
                    return;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
                try { using (PetForm pet = new PetForm(Preferences.Load(Preferences.DefaultPath), true)) Application.Run(pet); }
                catch (Exception ex)
                {
                    MessageBox.Show("桌宠未能继续运行。请重新启动。\n错误类型：" + ex.GetType().Name,
                        "忍野忍", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally { instance.ReleaseMutex(); }
            }
        }
    }
}
