namespace WingDetector;

public partial class MainForm : Form
{
    private readonly ScreenOcrService _ocr = new();
    private readonly NetworkController _net = new();
    private readonly TrayManager _tray;

    public MainForm()
    {
        InitializeComponent();
        _tray = new TrayManager(this);

        _ocr.OnKeywordDetected += OnKeywordDetected;
        _ocr.OnStatusChanged += AppendLog;

        AppendLog("[系统] 程序已启动，当前为管理员权限运行");
    }

    public void BtnStart_Click(object? sender, EventArgs e)
    {
        _ocr.Start();
        AppendLog("[保护] 已启动，正在检测屏幕中的\"光之翼\"");
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
        AppendLog("[检测] 检测到\"光之翼\"，正在断网...");
        _net.DisableAllAdapters();
        AppendLog("[网络] 已禁用所有网卡");
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
            _tray.ShowBalloon("光之翼检测器", "程序已最小化到托盘，仍在后台运行");
        }
        base.OnFormClosing(e);
    }
}