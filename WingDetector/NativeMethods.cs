using System.Runtime.InteropServices;

namespace WingDetector;

internal static class NativeMethods
{
    public static readonly IntPtr HWND_BROADCAST = new IntPtr(0xFFFF);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int RegisterWindowMessage(string lpString);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
}