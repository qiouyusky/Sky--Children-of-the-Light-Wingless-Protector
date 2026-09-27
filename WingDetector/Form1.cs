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
        _net.OnLog += AppendLog;

        AppendLog("[系统] 程序已启动，当前为管理员权限运行");
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
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

    public void BtnRestore_Click(object? sender, EventArgs e)
    {
        AppendLog("[网络] 用户点击「打开网络」");
        Task.Run(() => _net.EnableAllAdapters());
    }

    public void BtnRestart_Click(object? sender, EventArgs e)
    {
        AppendLog("[网络] 用户点击「重启网卡」");
        Task.Run(() => _net.RestartAllAdapters());
    }

    private void OnKeywordDetected()
    {
        AppendLog("[检测] 检测到关键词，正在断网...");
        Task.Run(() => _net.DisableAllAdapters());
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

    /// <summary>
    /// 处理自定义 Windows 消息：当第二个实例启动时，显示已运行实例的主窗口
    /// </summary>
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == Program.WM_SHOW_MAIN_WINDOW)
        {
            ShowMainWindow();
        }
        base.WndProc(ref m);
    }

    private void ShowMainWindow()
    {
        if (InvokeRequired)
        {
            Invoke(ShowMainWindow);
            return;
        }
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();
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