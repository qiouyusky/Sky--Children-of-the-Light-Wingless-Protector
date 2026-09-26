namespace WingDetector;

partial class MainForm
{
    private Button btnStart = null!;
    private Button btnStop = null!;
    private Button btnRestore = null!;
    private RichTextBox logBox = null!;

    private void InitializeComponent()
    {
        this.Text = "光之翼检测器";
        this.Size = new Size(520, 360);
        this.MinimumSize = new Size(420, 280);
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

        btnStart.Click += BtnStart_Click;
        btnStop.Click += BtnStop_Click;
        btnRestore.Click += BtnRestore_Click;

        panel.Controls.AddRange(new Control[] { btnStart, btnStop, btnRestore });

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