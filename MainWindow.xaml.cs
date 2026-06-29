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
    private readonly bool _snapshotMode; // запуск ярлыком «Снимок» (--snapshot)

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

        var args = Environment.GetCommandLineArgs();

        // Запуск с ключом --tray (автозапуск): сразу свернуть в трей
        if (args.Any(a => a.Equals("--tray", StringComparison.OrdinalIgnoreCase)))
        {
            Loaded += (_, _) => Hide();
        }

        // Запуск ярлыком «Снимок» (--snapshot): сразу захват экрана → текст, затем выход
        _snapshotMode = args.Any(a => a.Equals("--snapshot", StringComparison.OrdinalIgnoreCase));
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
        if (_snapshotMode) RunSnapshotMode();
    }

    // Режим ярлыка «Снимок»: окно не показываем, делаем захват → текст, затем выходим
    private async void RunSnapshotMode()
    {
        Hide();
        if (!OcrService.IsAvailable)
        {
            System.Windows.MessageBox.Show(App.Res("OcrUnavailable"), App.Res("AppTitle"),
                MessageBoxButton.OK, MessageBoxImage.Information);
            ExitApp();
            return;
        }
        await Task.Delay(200);                  // дать окну исчезнуть из снимка
        await CaptureRecognizeNotifyAsync();
        await Task.Delay(2600);                  // показать уведомление в трее
        ExitApp();
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

    // ── Плитка «Снимок → текст» (OCR) ────────────────────────────────────────

    private async void RunOcr(object sender, RoutedEventArgs e)
    {
        if (!OcrService.IsAvailable)
        {
            System.Windows.MessageBox.Show(App.Res("OcrUnavailable"), App.Res("AppTitle"),
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        bool wasVisible = IsVisible;
        Hide();                       // убрать своё окно из снимка
        await Task.Delay(180);
        await CaptureRecognizeNotifyAsync();
        if (wasVisible) ShowFromTray();
    }

    // Общая логика: захват области экрана → OCR → буфер обмена + уведомление
    private async Task CaptureRecognizeNotifyAsync()
    {
        Drawing.Bitmap? full = null, crop = null;
        try
        {
            full = ScreenCapture.CaptureVirtualScreen();
            var overlay = new CaptureOverlay(full, App.Res("OcrHint"));
            bool? picked = overlay.ShowDialog();
            crop = overlay.Result;

            if (picked == true && crop is not null)
            {
                string text = await OcrService.RecognizeAsync(crop, Settings.Current.OcrLanguage);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    SetClipboard(text);
                    _tray?.ShowBalloonTip(2500, App.Res("AppTitle"),
                        string.Format(App.Res("OcrDone"), text.Length), Forms.ToolTipIcon.Info);
                }
                else
                {
                    _tray?.ShowBalloonTip(2500, App.Res("AppTitle"),
                        App.Res("OcrEmpty"), Forms.ToolTipIcon.Warning);
                }
            }
        }
        catch (Exception ex)
        {
            _tray?.ShowBalloonTip(2500, App.Res("AppTitle"), ex.Message, Forms.ToolTipIcon.Error);
        }
        finally
        {
            crop?.Dispose();
            full?.Dispose();
        }
    }

    private static void SetClipboard(string text)
    {
        for (int i = 0; i < 5; i++)
        {
            try { System.Windows.Clipboard.SetText(text); return; }
            catch { System.Threading.Thread.Sleep(20); }
        }
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
