using System.Drawing.Imaging;
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

    public event Action? OnKeywordDetected;
    public event Action<string>? OnStatusChanged;

    public void Start()
    {
        if (_isRunning) return;

        _engine = OcrEngine.TryCreateFromLanguage(new Language("zh-CN"));
        if (_engine == null)
        {
            OnStatusChanged?.Invoke("[错误] 中文 OCR 引擎创建失败，请确认已在系统设置中安装中文 OCR 语言包");
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

            // ================= 调试日志 =================
            if (!string.IsNullOrWhiteSpace(rawText))
            {
                var preview = rawText.Replace("\n", " ").Replace("\r", " ").Trim();
                if (preview.Length > 50) preview = preview.Substring(0, 50) + "...";
                OnStatusChanged?.Invoke($"[OCR 识别到] {preview}");
            }
            else
            {
                OnStatusChanged?.Invoke("[OCR 识别到] (空白，请检查屏幕分辨率或缩放)");
            }
            // ===========================================

            // 去除所有空格和换行后再匹配，防止出现"光 之 翼"
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
        // 使用 VirtualScreen 确保截取全屏（支持多显示器）
        var bounds = System.Windows.Forms.SystemInformation.VirtualScreen;
        var bmp = new Bitmap(bounds.Width, bounds.Height);
        using var g = Graphics.FromImage(bmp);
        g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        return bmp;
    }
}