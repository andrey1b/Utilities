using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using SeniorUtilities.Tools;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using Application = System.Windows.Application;

namespace SeniorUtilities;

public partial class MainWindow : Window
{
    private LayoutFixService? _layoutFix;
    private Forms.NotifyIcon? _tray;
    private bool _reallyClose;
    private bool _trayHintShown;

    private static readonly Brush OnBrush  = new SolidColorBrush(Color.FromRgb(0x2C, 0x5F, 0x2D)); // зелёный
    private static readonly Brush OffBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x8A, 0x8A)); // серый

    public MainWindow()
    {
        InitializeComponent();
        OnBrush.Freeze();
        OffBrush.Freeze();
        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        Closing += OnClosing;

        // Запуск с ключом --tray (автозапуск): сразу свернуть в трей
        if (Environment.GetCommandLineArgs().Any(a => a.Equals("--tray", StringComparison.OrdinalIgnoreCase)))
        {
            Loaded += (_, _) => Hide();
        }
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _layoutFix = new LayoutFixService(this);
        if (Settings.Current.LayoutFixEnabled)
            _layoutFix.Enable();
        UpdateLayoutTile();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RefreshVersionText();
        SetupTray();
    }

    private void RefreshVersionText()
    {
        var ver = Assembly.GetExecutingAssembly().GetName().Version!;
        tbVersion.Text = $"{App.Res("VersionLabel")} {ver.Major}.{ver.Minor}.{ver.Build}";
    }

    // ── Плитка «Раскладка» ───────────────────────────────────────────────────

    private void ToggleLayoutFix(object sender, RoutedEventArgs e)
    {
        if (_layoutFix is null) return;
        if (_layoutFix.IsEnabled) _layoutFix.Disable();
        else _layoutFix.Enable();

        Settings.Current.LayoutFixEnabled = _layoutFix.IsEnabled;
        Settings.Save();
        UpdateLayoutTile();
    }

    private void UpdateLayoutTile()
    {
        bool on = _layoutFix?.IsEnabled == true;
        TileLayoutBtn.Background = on ? OnBrush : OffBrush;
        LayoutStateText.Text = on ? App.Res("StateOn") : App.Res("StateOff");
        LayoutStateText.Foreground = on ? OnBrush : OffBrush;
    }

    // ── Настройки ──────────────────────────────────────────────────────────────

    private void OpenSettings(object sender, RoutedEventArgs e)
    {
        new SettingsWindow { Owner = this }.ShowDialog();
        RefreshVersionText();
        UpdateLayoutTile(); // язык мог смениться
        if (_tray is not null) _tray.Text = App.Res("TrayTip");
    }

    // ── Трей ─────────────────────────────────────────────────────────────────

    private void SetupTray()
    {
        _tray = new Forms.NotifyIcon
        {
            Icon = LoadTrayIcon(),
            Visible = true,
            Text = App.Res("TrayTip"),
        };
        _tray.DoubleClick += (_, _) => ShowFromTray();

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(App.Res("TrayShow"), null, (_, _) => ShowFromTray());
        menu.Items.Add(App.Res("TrayExit"), null, (_, _) => ExitApp());
        _tray.ContextMenuStrip = menu;
    }

    private static Drawing.Icon LoadTrayIcon()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/app.ico");
            var stream = Application.GetResourceStream(uri)?.Stream;
            if (stream is not null) return new Drawing.Icon(stream);
        }
        catch { }
        return Drawing.SystemIcons.Application;
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ExitApp()
    {
        _reallyClose = true;
        Close();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!_reallyClose && Settings.Current.MinimizeToTray)
        {
            e.Cancel = true;
            Hide();
            if (!_trayHintShown)
            {
                _trayHintShown = true;
                _tray?.ShowBalloonTip(3000, App.Res("AppTitle"),
                    App.Res("TrayMinimized"), Forms.ToolTipIcon.Info);
            }
            return;
        }

        _layoutFix?.Dispose();
        if (_tray is not null) { _tray.Visible = false; _tray.Dispose(); }
    }
}
