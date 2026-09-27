namespace WingDetector;

public partial class Form1 : Form
{
    private readonly ScreenOcrService _ocr = new();
    private readonly NetworkController _net = new();
    private readonly TrayManager _tray;

    public Form1()
    {
        InitializeComponent();
        _tray = new TrayManager(this);

        _ocr.OnKeywordDetected += OnKeywordDetected;
        _ocr.OnStatusChanged += AppendLog;
        _net.OnLog += AppendLog;   // 让网络模块也能打日志

        AppendLog("[系统] 程序已启动，当前为管理员权限运行");
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        // 把主窗口句柄传给 OCR 服务，截图时排除本窗口，避免自检测
        _ocr.SetExcludeHandle(this.Handle);
        AppendLog("[系统] 已排除本程序窗口，避免自我检测");
    }

    public void BtnStart_Click(object? sender, EventArgs e)
    {
        _ocr.Start();
        AppendLog("[保护] 已启动，正在检测屏幕中的关键词");
        UpdateButtonState(protecting: true);
    }

    public void BtnStop_Click(object? sender, EventArgs e)
    {
        _ocr.Stop();
        AppendLog("[保护] 已关闭");
        UpdateButtonState(protecting: false);
    }

    private void BtnRestore_Click(object? sender, EventArgs e)
    {
        _net.EnableAllAdapters();
        AppendLog("[网络] 已尝试恢复所有网卡");
    }

    private void OnKeywordDetected()
    {
        AppendLog("[检测] 检测到关键词，正在断网...");
        _net.DisableAllAdapters();
        UpdateButtonState(protecting: false);
    }

    private void AppendLog(string msg)
    {
        if (InvokeRequired)
        {
            Invoke(() => AppendLog(msg));
            return;
        }
        logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n");
        logBox.ScrollToCaret();
    }

    private void UpdateButtonState(bool protecting)
    {
        btnStart.Enabled = !protecting;
        btnStop.Enabled = protecting;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            _tray.ShowBalloon("检测器", "程序已最小化到托盘，仍在后台运行");
        }
        base.OnFormClosing(e);
    }
}