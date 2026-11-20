namespace ZapretTrayApp;

internal sealed class DashboardForm : Form
{
    private static readonly Color CanvasColor = Color.FromArgb(22, 22, 22);
    private static readonly Color SidebarColor = Color.FromArgb(27, 27, 27);
    private static readonly Color SurfaceColor = Color.FromArgb(33, 33, 33);
    private static readonly Color RaisedColor = Color.FromArgb(43, 43, 43);
    private static readonly Color BorderColor = Color.FromArgb(62, 62, 62);
    private static readonly Color PrimaryTextColor = Color.FromArgb(244, 244, 240);
    private static readonly Color SecondaryTextColor = Color.FromArgb(190, 190, 185);
    private static readonly Color MutedTextColor = Color.FromArgb(145, 145, 140);
    private static readonly Color ActiveColor = Color.FromArgb(244, 244, 240);

    private readonly Bitmap _zapretGlyph;
    private readonly Bitmap _happGlyph;
    private readonly Button _zapretButton;
    private readonly Button _happButton;
    private readonly Label _zapretStatus = CreateStatusLabel();
    private readonly Label _zapretDot = CreateStatusDot();
    private readonly Label _zapretDetail = CreateDetailLabel();
    private readonly Label _zapretPath = CreateDetailLabel();
    private readonly ComboBox _strategyBox = new()
    {
        Dock = DockStyle.Fill,
        DropDownStyle = ComboBoxStyle.DropDownList,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 10),
        BackColor = RaisedColor,
        ForeColor = PrimaryTextColor,
        IntegralHeight = false,
        MaxDropDownItems = 12,
        Margin = Padding.Empty
    };
    private string? _selectedStrategy;
    private bool _refreshing;

    public event Action? ToggleRequested;
    public event Action<string>? StrategySelectionRequested;
    public event Action? SettingsRequested;

    public DashboardForm()
    {
        Text = "Zapret Tray";
        Icon = AppBranding.LoadIcon();
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 600);
        Size = new Size(1040, 680);
        BackColor = CanvasColor;
        ForeColor = PrimaryTextColor;
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;

        _zapretGlyph = LoadLargeZapretGlyph();
        _happGlyph = CreateHappGlyph();
        _zapretButton = CreateActionButton(_zapretGlyph);
        _happButton = CreateActionButton(_happGlyph);
        _happButton.Text = "Подключить";
        _happButton.Enabled = false;

        BuildLayout();

        _zapretButton.Click += (_, _) => ToggleRequested?.Invoke();
        _strategyBox.SelectedIndexChanged += StrategyBox_SelectedIndexChanged;
    }

    public void UpdateState(
        string? zapretRoot,
        IEnumerable<string> strategies,
        string? selectedStrategy,
        bool running)
    {
        var strategyPaths = strategies
            .OrderBy(StrategyDisplay.FormatName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        _refreshing = true;
        try
        {
            if (!_strategyBox.Items
                    .Cast<StrategyChoice>()
                    .Select(choice => choice.Path)
                    .SequenceEqual(strategyPaths, StringComparer.OrdinalIgnoreCase))
            {
                _strategyBox.Items.Clear();
                foreach (var path in strategyPaths)
                {
                    _strategyBox.Items.Add(new StrategyChoice(path, StrategyDisplay.FormatName(path)));
                }
            }

            var selectedIndex = Array.FindIndex(
                strategyPaths,
                path => string.Equals(path, selectedStrategy, StringComparison.OrdinalIgnoreCase));
            _strategyBox.SelectedIndex = selectedIndex;
            _strategyBox.Enabled = strategyPaths.Length > 0;
            _selectedStrategy = selectedIndex >= 0 ? strategyPaths[selectedIndex] : null;

            _zapretStatus.Text = running ? "Включено" : "Выключено";
            _zapretDot.ForeColor = running ? ActiveColor : MutedTextColor;
            _zapretDetail.Text = running
                ? "Выбранная стратегия активна"
                : "Нажми кнопку, чтобы включить выбранную стратегию";
            _zapretButton.Text = running ? "Выключить" : "Включить";
            _zapretButton.Enabled = running || _selectedStrategy is not null;

            if (string.IsNullOrWhiteSpace(zapretRoot))
            {
                _zapretPath.Text = "Папка zapret не выбрана";
            }
            else if (strategyPaths.Length == 0)
            {
                _zapretPath.Text = $"Стратегии не найдены · {zapretRoot}";
            }
            else
            {
                _zapretPath.Text = $"Папка со стратегиями · {zapretRoot}";
            }
        }
        finally
        {
            _refreshing = false;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _zapretGlyph.Dispose();
            _happGlyph.Dispose();
        }

        base.Dispose(disposing);
    }

    private void BuildLayout()
    {
        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = CanvasColor
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        shell.Controls.Add(BuildSidebar(), 0, 0);
        shell.Controls.Add(BuildMainContent(), 1, 0);
        Controls.Add(shell);
    }

    private Control BuildSidebar()
    {
        var sidebar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = SidebarColor,
            Padding = new Padding(20, 28, 16, 20),
            ColumnCount = 1,
            RowCount = 4
        };
        sidebar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        sidebar.Controls.Add(new Label
        {
            Text = "Zapret Tray",
            AutoSize = true,
            Font = new Font("Segoe UI", 16),
            ForeColor = PrimaryTextColor,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        var divider = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BorderColor,
            Height = 1,
            Margin = new Padding(0, 4, 0, 4)
        };
        sidebar.Controls.Add(divider, 0, 1);
        sidebar.Controls.Add(CreateNavigationButton("Подключения", selected: true), 0, 2);

        var bottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = SidebarColor,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        bottom.Controls.Add(CreateNavigationButton("Настройки", selected: false));
        sidebar.Controls.Add(bottom, 0, 3);
        return sidebar;
    }

    private Control BuildMainContent()
    {
        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = CanvasColor,
            Padding = new Padding(32, 28, 32, 24),
            ColumnCount = 1,
            RowCount = 3
        };
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        var header = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = CanvasColor,
            Margin = Padding.Empty
        };
        header.Controls.Add(new Label
        {
            Text = "Подключения",
            AutoSize = true,
            Font = new Font("Segoe UI", 21),
            ForeColor = PrimaryTextColor,
            Margin = Padding.Empty
        });
        header.Controls.Add(new Label
        {
            Text = "Выбери инструмент для управления",
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = MutedTextColor,
            Margin = Padding.Empty
        });
        main.Controls.Add(header, 0, 0);

        var modules = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = CanvasColor,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty
        };
        modules.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        modules.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        modules.Controls.Add(BuildZapretCard(), 0, 0);
        modules.Controls.Add(BuildHappCard(), 1, 0);
        main.Controls.Add(modules, 0, 1);

        main.Controls.Add(new Label
        {
            Text = "Для запуска Zapret Windows запросит подтверждение UAC",
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9),
            ForeColor = MutedTextColor,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty
        }, 0, 2);
        return main;
    }

    private Control BuildZapretCard()
    {
        var card = CreateModuleCard();
        var content = CreateModuleContent();

        content.Controls.Add(CreateModuleTitle("Zapret"), 0, 0);

        var actionPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = SurfaceColor,
            Padding = new Padding(0, 8, 0, 8)
        };
        actionPanel.Controls.Add(_zapretButton);
        content.Controls.Add(actionPanel, 0, 1);

        var status = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = SurfaceColor,
            Margin = Padding.Empty,
            Padding = new Padding(0, 8, 0, 0)
        };
        status.Controls.Add(_zapretDot);
        status.Controls.Add(_zapretStatus);
        content.Controls.Add(status, 0, 2);
        content.Controls.Add(_zapretDetail, 0, 3);
        content.Controls.Add(new Label
        {
            Text = "Стратегия",
            AutoSize = true,
            Dock = DockStyle.Bottom,
            Font = new Font("Segoe UI", 9),
            ForeColor = SecondaryTextColor,
            Margin = new Padding(0, 0, 0, 6)
        }, 0, 4);
        content.Controls.Add(_strategyBox, 0, 5);
        _zapretPath.AutoEllipsis = true;
        _zapretPath.Dock = DockStyle.Fill;
        _zapretPath.TextAlign = ContentAlignment.MiddleLeft;
        content.Controls.Add(_zapretPath, 0, 6);

        card.Controls.Add(content);
        return card;
    }

    private Control BuildHappCard()
    {
        var card = CreateModuleCard();
        var content = CreateModuleContent();

        content.Controls.Add(CreateModuleTitle("Happ"), 0, 0);

        var actionPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = SurfaceColor,
            Padding = new Padding(0, 8, 0, 8)
        };
        actionPanel.Controls.Add(_happButton);
        content.Controls.Add(actionPanel, 0, 1);

        var status = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = SurfaceColor,
            Margin = Padding.Empty,
            Padding = new Padding(0, 8, 0, 0)
        };
        status.Controls.Add(CreateStatusDot());
        status.Controls.Add(new Label
        {
            Text = "Интеграция не подключена",
            AutoSize = true,
            Font = new Font("Segoe UI", 15),
            ForeColor = SecondaryTextColor,
            Margin = Padding.Empty
        });
        content.Controls.Add(status, 0, 2);
        content.Controls.Add(new Label
        {
            Text = "Здесь будет управление VPN из Happ",
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = MutedTextColor,
            Margin = Padding.Empty,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 3);
        content.Controls.Add(new Label
        {
            Text = "Подключение Happ будет добавлено отдельно",
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9),
            ForeColor = MutedTextColor,
            Margin = Padding.Empty,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 5);

        card.Controls.Add(content);
        return card;
    }

    private static Panel CreateModuleCard()
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = SurfaceColor,
            Padding = new Padding(20),
            Margin = new Padding(0, 0, 12, 0)
        };
    }

    private static TableLayoutPanel CreateModuleContent()
    {
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = SurfaceColor,
            ColumnCount = 1,
            RowCount = 7
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        return content;
    }

    private static Label CreateModuleTitle(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 17),
            ForeColor = PrimaryTextColor,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty
        };
    }

    private static Button CreateActionButton(Image image)
    {
        var button = new Button
        {
            Text = "Включить",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.FromArgb(22, 22, 22),
            BackColor = ActiveColor,
            Image = image,
            ImageAlign = ContentAlignment.TopCenter,
            TextImageRelation = TextImageRelation.ImageAboveText,
            TextAlign = ContentAlignment.BottomCenter,
            Cursor = Cursors.Hand,
            Padding = new Padding(12),
            Margin = Padding.Empty
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private Button CreateNavigationButton(string text, bool selected)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 40,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = selected ? PrimaryTextColor : SecondaryTextColor,
            BackColor = selected ? RaisedColor : SidebarColor,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 0, 4)
        };
        button.FlatAppearance.BorderSize = 0;
        if (!selected)
        {
            button.Click += (_, _) => SettingsRequested?.Invoke();
        }

        return button;
    }

    private static Label CreateStatusLabel()
    {
        return new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 15),
            ForeColor = PrimaryTextColor,
            Margin = Padding.Empty
        };
    }

    private static Label CreateStatusDot()
    {
        return new Label
        {
            Text = "●",
            AutoSize = true,
            Font = new Font("Segoe UI", 12),
            ForeColor = MutedTextColor,
            Margin = new Padding(0, 4, 8, 0)
        };
    }

    private static Label CreateDetailLabel()
    {
        return new Label
        {
            AutoEllipsis = true,
            AutoSize = false,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = SecondaryTextColor,
            Margin = Padding.Empty,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private static Bitmap LoadLargeZapretGlyph()
    {
        using var icon = AppBranding.LoadIcon(new Size(96, 96));
        return icon.ToBitmap();
    }

    private static Bitmap CreateHappGlyph()
    {
        var bitmap = new Bitmap(96, 96);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var font = new Font("Segoe UI", 56, FontStyle.Regular, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(Color.FromArgb(22, 22, 22));
        var size = graphics.MeasureString("H", font);
        graphics.DrawString("H", font, brush, (96 - size.Width) / 2, (96 - size.Height) / 2 - 2);
        return bitmap;
    }

    private void StrategyBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_refreshing
            || _strategyBox.SelectedItem is not StrategyChoice choice
            || string.Equals(choice.Path, _selectedStrategy, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _selectedStrategy = choice.Path;
        StrategySelectionRequested?.Invoke(choice.Path);
    }

    private sealed record StrategyChoice(string Path, string Name)
    {
        public override string ToString() => Name;
    }
}
