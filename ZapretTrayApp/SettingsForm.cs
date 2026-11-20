namespace ZapretTrayApp;

internal sealed class SettingsForm : Form
{
    private static readonly Color CanvasColor = Color.FromArgb(22, 22, 22);
    private static readonly Color SurfaceColor = Color.FromArgb(33, 33, 33);
    private static readonly Color RaisedColor = Color.FromArgb(43, 43, 43);
    private static readonly Color BorderColor = Color.FromArgb(62, 62, 62);
    private static readonly Color TextColor = Color.FromArgb(244, 244, 240);
    private static readonly Color SecondaryTextColor = Color.FromArgb(190, 190, 185);

    private readonly TextBox _pathBox = new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.FixedSingle,
        BackColor = RaisedColor,
        ForeColor = TextColor,
        Font = new Font("Segoe UI", 9.5f),
        Margin = new Padding(0, 0, 8, 0)
    };
    private readonly CheckBox _startupCheckBox = new()
    {
        Text = "Запускать вместе с Windows",
        AutoSize = true,
        ForeColor = TextColor,
        BackColor = SurfaceColor,
        FlatStyle = FlatStyle.Flat,
        Margin = new Padding(0, 0, 0, 12)
    };
    private readonly CheckBox _stopOnExitCheckBox = new()
    {
        Text = "Выключать zapret при выходе из приложения",
        AutoSize = true,
        ForeColor = TextColor,
        BackColor = SurfaceColor,
        FlatStyle = FlatStyle.Flat
    };

    public SettingsForm(string? zapretPath, bool startWithWindows, bool stopOnExit = false)
    {
        Text = "Настройки";
        Icon = AppBranding.LoadIcon();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(600, 300);
        BackColor = CanvasColor;
        ForeColor = TextColor;
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;

        _pathBox.Text = zapretPath ?? string.Empty;
        _startupCheckBox.Checked = startWithWindows;
        _stopOnExitCheckBox.Checked = stopOnExit;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = CanvasColor,
            Padding = new Padding(24),
            ColumnCount = 1,
            RowCount = 5
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        layout.Controls.Add(new Label
        {
            Text = "Настройки приложения",
            AutoSize = true,
            Font = new Font("Segoe UI", 16),
            ForeColor = TextColor,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        layout.Controls.Add(new Label
        {
            Text = "Папка zapret",
            AutoSize = true,
            Font = new Font("Segoe UI", 9),
            ForeColor = SecondaryTextColor,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 1);
        layout.Controls.Add(BuildPathRow(), 0, 2);
        layout.Controls.Add(BuildOptions(), 0, 3);
        layout.Controls.Add(BuildButtons(), 0, 4);
        Controls.Add(layout);

        AcceptButton = _saveButton;
        CancelButton = _cancelButton;
    }

    private readonly Button _saveButton = new()
    {
        Text = "Сохранить",
        DialogResult = DialogResult.OK,
        AutoSize = true,
        FlatStyle = FlatStyle.Flat,
        BackColor = TextColor,
        ForeColor = CanvasColor,
        Font = new Font("Segoe UI", 9.5f),
        Cursor = Cursors.Hand,
        Margin = new Padding(8, 0, 0, 0),
        Padding = new Padding(12, 4, 12, 4)
    };

    private readonly Button _cancelButton = new()
    {
        Text = "Отмена",
        DialogResult = DialogResult.Cancel,
        AutoSize = true,
        FlatStyle = FlatStyle.Flat,
        BackColor = SurfaceColor,
        ForeColor = TextColor,
        Font = new Font("Segoe UI", 9.5f),
        Cursor = Cursors.Hand,
        Padding = new Padding(12, 4, 12, 4)
    };

    public string ZapretPath => _pathBox.Text.Trim();
    public bool StartWithWindows => _startupCheckBox.Checked;
    public bool StopOnExit => _stopOnExitCheckBox.Checked;

    private Control BuildPathRow()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = CanvasColor,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var browseButton = new Button
        {
            Text = "Обзор",
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = SurfaceColor,
            ForeColor = TextColor,
            Font = new Font("Segoe UI", 9.5f),
            Cursor = Cursors.Hand,
            Padding = new Padding(10, 4, 10, 4),
            Margin = Padding.Empty
        };
        browseButton.FlatAppearance.BorderColor = BorderColor;
        browseButton.Click += (_, _) => BrowseForFolder();

        row.Controls.Add(_pathBox, 0, 0);
        row.Controls.Add(browseButton, 1, 0);
        return row;
    }

    private Control BuildOptions()
    {
        var options = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = SurfaceColor,
            Padding = new Padding(16, 12, 16, 8),
            Margin = new Padding(0, 8, 0, 0)
        };
        options.Controls.Add(_startupCheckBox);
        options.Controls.Add(_stopOnExitCheckBox);
        return options;
    }

    private Control BuildButtons()
    {
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = CanvasColor,
            Padding = new Padding(0, 8, 0, 0),
            Margin = Padding.Empty
        };
        _saveButton.FlatAppearance.BorderSize = 0;
        _cancelButton.FlatAppearance.BorderColor = BorderColor;
        buttons.Controls.Add(_saveButton);
        buttons.Controls.Add(_cancelButton);
        return buttons;
    }

    private void BrowseForFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Выберите папку zapret-discord-youtube",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(_pathBox.Text) ? _pathBox.Text : string.Empty
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _pathBox.Text = dialog.SelectedPath;
        }
    }
}
