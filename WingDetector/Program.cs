using System.Threading;
using System.Windows.Forms;

namespace WingDetector;

static class Program
{
    /// <summary>
    /// 用于通知已有实例显示主窗口的自定义 Windows 消息
    /// </summary>
    public static readonly int WM_SHOW_MAIN_WINDOW =
        NativeMethods.RegisterWindowMessage("WingDetector_ShowMainWindow_Message");

    private const string MutexName = "Local\\WingDetector_SingleInstance_Mutex";

    [STAThread]
    static void Main()
    {
        // 尝试创建/打开命名 Mutex
        using var mutex = new Mutex(true, MutexName, out bool isFirstInstance);

        if (!isFirstInstance)
        {
            // 已有实例在运行：广播消息，让已有实例显示窗口，然后自己退出
            NativeMethods.PostMessage(
                NativeMethods.HWND_BROADCAST,
                WM_SHOW_MAIN_WINDOW,
                IntPtr.Zero,
                IntPtr.Zero
            );
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());

        // 程序退出时释放 Mutex
        try { mutex.ReleaseMutex(); } catch { /* 忽略 */ }
    }
}