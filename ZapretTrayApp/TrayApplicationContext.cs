using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.Win32;

namespace ZapretTrayApp;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private const string StartupRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupValueName = "ZapretTray";
    private readonly NotifyIcon _trayIcon;
    private readonly Icon _appIcon;
    private readonly ContextMenuStrip _menu = new();
    private readonly ToolStripMenuItem _selectedMenu = new("Выбранная стратегия");
    private readonly ToolStripMenuItem _toggleMenu = new("Включить");
    private readonly System.Windows.Forms.Timer _statusTimer = new() { Interval = 1000 };
    private readonly string _statePath;
    private AppState _state;
    private string? _zapretRoot;
    private HashSet<string> _strategyPaths = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public TrayApplicationContext()
    {
        _statePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ZapretTray",
            "state.json");
        _state = LoadState();

        _zapretRoot = IsZapretDirectory(_state.ZapretRoot)
            ? Path.GetFullPath(_state.ZapretRoot!)
            : FindZapretRoot();
        ReloadStrategies();
        if (_state.SelectedStrategy is null || !_strategyPaths.Contains(_state.SelectedStrategy))
        {
            _state.SelectedStrategy = GetDefaultStrategy();
        }

        _appIcon = LoadAppIcon();
        BuildMenu();

        _trayIcon = new NotifyIcon
        {
            Icon = _appIcon,
            Text = BuildTrayText(),
            ContextMenuStrip = _menu,
            Visible = true
        };
        _trayIcon.MouseClick += TrayIcon_MouseClick;
        _statusTimer.Tick += (_, _) => UpdateIconAndMenu();
        _statusTimer.Start();
        UpdateIconAndMenu();
    }

    private void BuildMenu()
    {
        _toggleMenu.Click += (_, _) => Toggle();
        _menu.Items.Add(_toggleMenu);
        _menu.Items.Add(new ToolStripSeparator());

        _selectedMenu.Enabled = false;
        _menu.Items.Add(_selectedMenu);

        RebuildStrategyItems();
        _menu.Items.Add(new ToolStripSeparator());

        var settings = new ToolStripMenuItem("Настройки...");
        settings.Click += (_, _) => OpenSettings();
        _menu.Items.Add(settings);

        var openFolder = new ToolStripMenuItem("Открыть папку zapret");
        openFolder.Click += (_, _) => OpenZapretFolder();
        _menu.Items.Add(openFolder);

        var exit = new ToolStripMenuItem("Выход");
        exit.Click += (_, _) => ExitThread();
        _menu.Items.Add(exit);
    }

    private void RebuildStrategyItems()
    {
        foreach (var item in _menu.Items.OfType<ToolStripMenuItem>()
                     .Where(item => item.Tag is string)
                     .ToArray())
        {
            _menu.Items.Remove(item);
            item.Dispose();
        }

        var insertAt = _menu.Items.IndexOf(_selectedMenu) + 1;
        foreach (var strategy in _strategyPaths
                     .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
        {
            var item = new ToolStripMenuItem(FormatStrategyName(strategy))
            {
                Tag = strategy
            };
            item.Click += (_, _) => SelectStrategy(strategy);
            _menu.Items.Insert(insertAt++, item);
        }
    }

    private void TrayIcon_MouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            Toggle();
        }
    }

    private void Toggle()
    {
        try
        {
            if (IsRunning())
            {
                Stop();
            }
            else
            {
                StartSelected();
            }
        }
        catch (Exception ex)
        {
            ShowError(ex.Message, "Не удалось переключить zapret");
        }

        UpdateIconAndMenu();
    }

    private void SelectStrategy(string strategy)
    {
        try
        {
            var wasRunning = IsRunning();
            if (wasRunning)
            {
                Stop();
            }

            _state.SelectedStrategy = strategy;
            SaveState();
            if (wasRunning)
            {
                StartSelected();
            }
        }
        catch (Exception ex)
        {
            ShowError(ex.Message, "Не удалось сменить стратегию");
        }

        UpdateIconAndMenu();
    }

    private void StartSelected()
    {
        if (_zapretRoot is null || !IsZapretDirectory(_zapretRoot))
        {
            throw new InvalidOperationException("Папка zapret не настроена или не содержит нужные файлы.");
        }

        if (_state.SelectedStrategy is null || !_strategyPaths.Contains(_state.SelectedStrategy))
        {
            throw new InvalidOperationException("Выберите найденную стратегию general*.bat в меню.");
        }

        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            WorkingDirectory = _zapretRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            Arguments = $"/d /c call \"{_state.SelectedStrategy}\""
        };

        using var launcher = Process.Start(psi)
            ?? throw new InvalidOperationException("Windows не запустила командный файл стратегии.");
        SaveState();
    }

    private void Stop()
    {
        var processes = GetZapretProcesses();
        var failures = new List<string>();
        foreach (var process in processes)
        {
            using (process)
            {
                try
                {
                    process.Kill(true);
                    if (!process.WaitForExit(5000))
                    {
                        failures.Add($"Не удалось дождаться остановки winws.exe (PID {process.Id}).");
                    }
                }
                catch (Win32Exception ex)
                {
                    failures.Add($"Не удалось остановить winws.exe (PID {process.Id}): {ex.Message}");
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
        }
    }

    private bool IsRunning()
    {
        var processes = GetZapretProcesses();
        foreach (var process in processes)
        {
            process.Dispose();
        }

        return processes.Count > 0;
    }

    private List<Process> GetZapretProcesses()
    {
        var matches = new List<Process>();
        if (_zapretRoot is null)
        {
            return matches;
        }

        var expectedExe = Path.GetFullPath(Path.Combine(_zapretRoot, "bin", "winws.exe"));
        foreach (var process in Process.GetProcessesByName("winws"))
        {
            try
            {
                var executable = process.MainModule?.FileName;
                if (executable is not null
                    && string.Equals(Path.GetFullPath(executable), expectedExe, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(process);
                    continue;
                }
            }
            catch (Win32Exception)
            {
            }
            catch (InvalidOperationException)
            {
            }

            process.Dispose();
        }

        return matches;
    }

    private void UpdateIconAndMenu()
    {
        var running = IsRunning();
        _toggleMenu.Text = running ? "Выключить" : "Включить";
        _selectedMenu.Text = _state.SelectedStrategy is null
            ? "Стратегия не выбрана"
            : $"Стратегия: {FormatStrategyName(_state.SelectedStrategy)}";

        foreach (var item in _menu.Items.OfType<ToolStripMenuItem>().Where(item => item.Tag is string))
        {
            item.Checked = item.Tag is string strategy
                && string.Equals(strategy, _state.SelectedStrategy, StringComparison.OrdinalIgnoreCase);
        }

        _trayIcon.Text = BuildTrayText(running);
    }

    private string BuildTrayText()
    {
        return BuildTrayText(IsRunning());
    }

    private string BuildTrayText(bool running)
    {
        var state = running ? "включен" : "выключен";
        var strategy = _state.SelectedStrategy is null
            ? "нет стратегии"
            : FormatStrategyName(_state.SelectedStrategy);
        return $"Zapret: {state} — {strategy}";
    }

    private void OpenSettings()
    {
        using var form = new SettingsForm(_zapretRoot, _state.StartWithWindows, _state.StopOnExit);
        if (form.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        try
        {
            var newRoot = Path.GetFullPath(form.ZapretPath);
            if (!IsZapretDirectory(newRoot))
            {
                throw new InvalidOperationException(
                    "В выбранной папке должны находиться service.bat, bin\\winws.exe и стратегии general*.bat.");
            }

            if (!string.Equals(_zapretRoot, newRoot, StringComparison.OrdinalIgnoreCase) && IsRunning())
            {
                Stop();
            }

            SetStartup(form.StartWithWindows);
            _zapretRoot = newRoot;
            _state.ZapretRoot = newRoot;
            _state.StartWithWindows = form.StartWithWindows;
            _state.StopOnExit = form.StopOnExit;
            ReloadStrategies();
            if (_state.SelectedStrategy is null || !_strategyPaths.Contains(_state.SelectedStrategy))
            {
                _state.SelectedStrategy = GetDefaultStrategy();
            }

            SaveState();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message, "Не удалось сохранить настройки");
        }

        UpdateIconAndMenu();
    }

    private void OpenZapretFolder()
    {
        if (_zapretRoot is null || !Directory.Exists(_zapretRoot))
        {
            OpenSettings();
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{_zapretRoot}\"",
            UseShellExecute = true
        });
    }

    private void ReloadStrategies()
    {
        _strategyPaths = _zapretRoot is null || !Directory.Exists(_zapretRoot)
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : Directory
                .EnumerateFiles(_zapretRoot, "general*.bat", SearchOption.TopDirectoryOnly)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        RebuildStrategyItems();
    }

    private string? GetDefaultStrategy()
    {
        return _strategyPaths
            .OrderBy(path => string.Equals(
                Path.GetFileName(path),
                "general.bat",
                StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static string FormatStrategyName(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        if (name.Equals("general", StringComparison.OrdinalIgnoreCase))
        {
            return "General";
        }

        return name.StartsWith("general ", StringComparison.OrdinalIgnoreCase)
            ? name["general ".Length..].Trim().Trim('(', ')')
            : name;
    }

    private static bool IsZapretDirectory(string? path)
    {
        return !string.IsNullOrWhiteSpace(path)
            && Directory.Exists(path)
            && Directory.Exists(Path.Combine(path, "bin"))
            && File.Exists(Path.Combine(path, "bin", "winws.exe"))
            && File.Exists(Path.Combine(path, "service.bat"))
            && Directory.EnumerateFiles(path, "general*.bat", SearchOption.TopDirectoryOnly).Any();
    }

    private static string? FindZapretRoot()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
        {
            var directory = new DirectoryInfo(start);
            for (var index = 0; index < 8 && directory is not null; index++)
            {
                var candidate = Path.Combine(directory.FullName, "zapret-discord-youtube");
                if (IsZapretDirectory(candidate))
                {
                    return candidate;
                }

                if (IsZapretDirectory(directory.FullName))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        return null;
    }

    private static Icon LoadAppIcon()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("ZapretTrayApp.app.ico")
            ?? throw new InvalidOperationException("В сборке не найден файл иконки приложения.");
        return new Icon(stream);
    }

    private AppState LoadState()
    {
        if (!File.Exists(_statePath))
        {
            return new AppState();
        }

        try
        {
            return JsonSerializer.Deserialize<AppState>(File.ReadAllText(_statePath)) ?? new AppState();
        }
        catch (JsonException ex)
        {
            ShowError(ex.Message, "Не удалось прочитать настройки");
            return new AppState();
        }
        catch (IOException ex)
        {
            ShowError(ex.Message, "Не удалось прочитать настройки");
            return new AppState();
        }
    }

    private void SaveState()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_statePath)!);
        File.WriteAllText(_statePath, JsonSerializer.Serialize(_state));
    }

    private static void SetStartup(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(StartupRegistryKey, true)
            ?? throw new InvalidOperationException("Не удалось открыть ключ автозапуска Windows.");
        if (enabled)
        {
            key.SetValue(StartupValueName, $"\"{Application.ExecutablePath}\"");
        }
        else
        {
            key.DeleteValue(StartupValueName, false);
        }
    }

    private static void ShowError(string message, string title)
    {
        MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    protected override void ExitThreadCore()
    {
        if (_state.StopOnExit)
        {
            try
            {
                Stop();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message, "Не удалось выключить zapret");
            }
        }

        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _statusTimer.Dispose();
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _menu.Dispose();
                _appIcon.Dispose();
            }

            _disposed = true;
        }

        base.Dispose(disposing);
    }
}
