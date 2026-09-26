namespace WingDetector;

public class TrayManager
{
    private readonly NotifyIcon _notifyIcon;
    private readonly MainForm _form;

    public TrayManager(MainForm form)
    {
        _form = form;
        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "光之翼检测器",
            Visible = true
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("显示主窗口", null, (_, _) => ShowForm());
        menu.Items.Add("启动保护", null, (_, _) => _form.BtnStart_Click(null, EventArgs.Empty));
        menu.Items.Add("关闭保护", null, (_, _) => _form.BtnStop_Click(null, EventArgs.Empty));
        menu.Items.Add("-");
        menu.Items.Add("退出", null, (_, _) => Application.Exit());

        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.DoubleClick += (_, _) => ShowForm();
    }

    private void ShowForm()
    {
        _form.Show();
        _form.WindowState = FormWindowState.Normal;
        _form.Activate();
    }

    public void ShowBalloon(string title, string text)
    {
        _notifyIcon.ShowBalloonTip(3000, title, text, ToolTipIcon.Info);
    }
}