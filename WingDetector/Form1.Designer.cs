namespace WingDetector;

partial class Form1
{
    private Button btnStart = null!;
    private Button btnStop = null!;
    private Button btnRestore = null!;
    private Button btnRestart = null!;
    private RichTextBox logBox = null!;

    private void InitializeComponent()
    {
        this.Text = "光遇无翼拯救者";
        this.Size = new Size(560, 360);
        this.MinimumSize = new Size(460, 280);
        this.StartPosition = FormStartPosition.CenterScreen;

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 50,
            Padding = new Padding(10),
            FlowDirection = FlowDirection.LeftToRight
        };

        btnStart = new Button { Text = "启动保护", Width = 100, Height = 32 };
        btnStop = new Button { Text = "关闭保护", Width = 100, Height = 32, Enabled = false };
        btnRestore = new Button { Text = "打开网络", Width = 100, Height = 32 };
        btnRestart = new Button { Text = "重启网卡", Width = 100, Height = 32 };

        btnStart.Click += BtnStart_Click;
        btnStop.Click += BtnStop_Click;
        btnRestore.Click += BtnRestore_Click;
        btnRestart.Click += BtnRestart_Click;

        panel.Controls.AddRange(new Control[] { btnStart, btnStop, btnRestore, btnRestart });

        logBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.LightGreen,
            Font = new Font("Consolas", 10),
            BorderStyle = BorderStyle.None
        };

        this.Controls.Add(logBox);
        this.Controls.Add(panel);
    }
}