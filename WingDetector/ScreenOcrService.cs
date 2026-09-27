using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace WingDetector;

public class ScreenOcrService
{
    private System.Threading.Timer? _timer;
    private OcrEngine? _engine;
    private const string Keyword = "光之翼";
    private bool _isRunning;
    private IntPtr _excludeHandle = IntPtr.Zero;

    public event Action? OnKeywordDetected;
    public event Action<string>? OnStatusChanged;

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public void SetExcludeHandle(IntPtr handle)
    {
        _excludeHandle = handle;
    }

    public void Start()
    {
        if (_isRunning) return;

        _engine = OcrEngine.TryCreateFromLanguage(new Language("zh-CN"));
        if (_engine == null)
        {
            OnStatusChanged?.Invoke("[错误] 中文 OCR 引擎创建失败，请确认已安装中文 OCR 语言包");
            return;
        }

        _isRunning = true;
        _timer = new System.Threading.Timer(async _ => await ScanOnce(), null, 0, 1500);
    }

    public void Stop()
    {
        _isRunning = false;
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        _timer?.Dispose();
        _timer = null;
    }

    private async Task ScanOnce()
    {
        if (!_isRunning) return;
        try
        {
            using var bmp = CaptureScreen();
            var stream = new InMemoryRandomAccessStream();
            bmp.Save(stream.AsStream(), ImageFormat.Bmp);
            var decoder = await BitmapDecoder.CreateAsync(stream);
            var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            var result = await _engine!.RecognizeAsync(softwareBitmap);
            string rawText = result.Text ?? "";

            if (!string.IsNullOrWhiteSpace(rawText))
            {
                var preview = rawText.Replace("\n", " ").Replace("\r", " ").Trim();
                if (preview.Length > 50) preview = preview.Substring(0, 50) + "...";
                OnStatusChanged?.Invoke($"[OCR 识别到] {preview}");
            }

            var cleanText = Regex.Replace(rawText, @"\s+", "");
            if (cleanText.Contains(Keyword))
            {
                Stop();
                OnKeywordDetected?.Invoke();
            }
        }
        catch (Exception ex)
        {
            OnStatusChanged?.Invoke($"[错误] OCR 扫描异常: {ex.Message}");
        }
    }

    private Bitmap CaptureScreen()
    {
        var bounds = SystemInformation.VirtualScreen;
        var bmp = new Bitmap(bounds.Width, bounds.Height);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);

            // 把自己窗口区域涂黑，避免 OCR 检测到本程序 UI
            if (_excludeHandle != IntPtr.Zero && IsWindowVisible(_excludeHandle))
            {
                if (GetWindowRect(_excludeHandle, out var rect))
                {
                    int x = rect.Left - bounds.Left;
                    int y = rect.Top - bounds.Top;
                    int w = rect.Right - rect.Left;
                    int h = rect.Bottom - rect.Top;
                    if (w > 0 && h > 0)
                    {
                        using var black = new SolidBrush(Color.Black);
                        g.FillRectangle(black, x, y, w, h);
                    }
                }
            }
        }
        return bmp;
    }
}