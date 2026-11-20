namespace ZapretTrayApp;

internal sealed class SettingsForm : Form
{
    private readonly TextBox _pathBox = new() { Dock = DockStyle.Fill };
    private readonly CheckBox _startupCheckBox = new() { Text = "Запускать вместе с Windows", AutoSize = true };
    private readonly CheckBox _stopOnExitCheckBox = new() { Text = "Выключать zapret при выходе из приложения", AutoSize = true };

    public SettingsForm(string? zapretPath, bool startWithWindows, bool stopOnExit = false)
    {
        Text = "Настройки Zapret Tray";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(520, 190);

        _pathBox.Text = zapretPath ?? string.Empty;
        _startupCheckBox.Checked = startWithWindows;
        _stopOnExitCheckBox.Checked = stopOnExit;

        var pathLabel = new Label
        {
            Text = "Папка zapret:",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };
        var browseButton = new Button { Text = "Обзор...", AutoSize = true };
        browseButton.Click += (_, _) => BrowseForFolder();

        var pathPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(12),
            AutoSize = true
        };
        pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pathPanel.Controls.Add(pathLabel, 0, 0);
        pathPanel.SetColumnSpan(pathLabel, 2);
        pathPanel.Controls.Add(_pathBox, 0, 1);
        pathPanel.Controls.Add(browseButton, 1, 1);

        var optionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(12, 0, 12, 0),
            AutoSize = true,
            WrapContents = false
        };
        optionsPanel.Controls.Add(_startupCheckBox);
        optionsPanel.Controls.Add(_stopOnExitCheckBox);

        var saveButton = new Button { Text = "Сохранить", DialogResult = DialogResult.OK, AutoSize = true };
        var cancelButton = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, AutoSize = true };
        var buttonsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(12),
            AutoSize = true
        };
        buttonsPanel.Controls.Add(cancelButton);
        buttonsPanel.Controls.Add(saveButton);

        Controls.Add(optionsPanel);
        Controls.Add(pathPanel);
        Controls.Add(buttonsPanel);
        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    public string ZapretPath => _pathBox.Text.Trim();
    public bool StartWithWindows => _startupCheckBox.Checked;
    public bool StopOnExit => _stopOnExitCheckBox.Checked;

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
