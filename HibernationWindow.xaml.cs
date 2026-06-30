using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;

namespace SeniorUtilities;

public partial class HibernationWindow : Window
{
    public HibernationWindow()
    {
        InitializeComponent();
    }

    // «Включить и настроить» — запускаем фикс с правами администратора (UAC),
    // в отдельном консольном окне с отчётом (как старый _fix_hibernation.ps1).
    private void OnFixClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = Path.Combine(Path.GetTempPath(), "Utilities_fix_hibernation.ps1");
            File.WriteAllText(path, FixScript, new UTF8Encoding(true)); // BOM — для кириллицы в powershell.exe

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{path}\"",
                UseShellExecute = true,
                Verb = "runas",            // запрос прав администратора (UAC)
            };
            Process.Start(psi);
            TxtStatus.Text = App.Res("HibFixStarted");
        }
        catch (Win32Exception)
        {
            // пользователь отклонил UAC
            TxtStatus.Text = App.Res("HibCancelled");
        }
        catch (Exception ex)
        {
            TxtStatus.Text = ex.Message;
        }
    }

    // «Гибернировать сейчас» — shutdown /h
    private void OnHibernateNowClick(object sender, RoutedEventArgs e)
    {
        var ok = System.Windows.MessageBox.Show(this, App.Res("HibNowConfirm"), App.Res("HibTitle"),
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (ok != MessageBoxResult.Yes) { TxtStatus.Text = App.Res("HibCancelled"); return; }

        try
        {
            Process.Start(new ProcessStartInfo("shutdown.exe", "/h") { UseShellExecute = false, CreateNoWindow = true });
        }
        catch (Exception ex)
        {
            TxtStatus.Text = ex.Message;
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    // Встроенный фикс гибернации (выполняется уже с правами администратора).
    private const string FixScript = @"
$ErrorActionPreference = 'SilentlyContinue'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
Clear-Host
Write-Host ''
Write-Host ' =====================================================' -ForegroundColor Cyan
Write-Host '  Гибернация: включение и отключение «будилок»' -ForegroundColor Cyan
Write-Host ' =====================================================' -ForegroundColor Cyan
Write-Host ''
Write-Host '[1/4] Включаю гибернацию (powercfg /hibernate on)...' -ForegroundColor Yellow
& powercfg /hibernate on
Write-Host ''
Write-Host '[2/4] Устройства, которым сейчас разрешено будить ПК:' -ForegroundColor Yellow
Write-Host ' -----------------------------------------------------'
$armed = @(& powercfg /devicequery wake_armed)
if ($armed -and $armed.Count -gt 0) { $armed | ForEach-Object { Write-Host ""  $_"" } } else { Write-Host '  (никто)' -ForegroundColor Green }
Write-Host ' -----------------------------------------------------'
Write-Host ''
$disabled = 0
foreach ($d in $armed) {
    if (-not $d) { continue }
    $name = $d.Trim()
    if (-not $name) { continue }
    if ($name -match '^(NONE|None|НЕТ)$') { continue }
    Write-Host ""  отключаю: $name""
    & powercfg /devicedisablewake $name 2>&1 | Out-Null
    $disabled++
}
Write-Host ""  Отключено устройств: $disabled"" -ForegroundColor Green
Write-Host ''
Write-Host '[3/4] Активные wake-таймеры:' -ForegroundColor Yellow
Write-Host ' -----------------------------------------------------'
& powercfg /waketimers
Write-Host ' -----------------------------------------------------'
Write-Host ''
Write-Host '[4/4] Запрещаю wake timers в активной схеме питания (AC и DC)...' -ForegroundColor Yellow
& powercfg /setacvalueindex SCHEME_CURRENT SUB_SLEEP bd3b718a-0680-4d9d-8ab2-e1d2b4ac806d 0
& powercfg /setdcvalueindex SCHEME_CURRENT SUB_SLEEP bd3b718a-0680-4d9d-8ab2-e1d2b4ac806d 0
& powercfg /setactive SCHEME_CURRENT
Write-Host '  Готово.' -ForegroundColor Green
Write-Host ''
Write-Host ' =====================================================' -ForegroundColor Green
Write-Host '  Финальное состояние' -ForegroundColor Green
Write-Host ' =====================================================' -ForegroundColor Green
Write-Host ''
Write-Host 'Доступные режимы сна:'
& powercfg /a
Write-Host ''
Write-Host 'Кто теперь может будить ПК (должно быть пусто):'
Write-Host ' -----------------------------------------------------'
$leftover = @(& powercfg /devicequery wake_armed)
if ($leftover -and $leftover.Count -gt 0 -and -not ($leftover.Count -eq 1 -and $leftover[0] -match '^(NONE|None|НЕТ)$')) { $leftover | ForEach-Object { Write-Host ""  $_"" -ForegroundColor Yellow } } else { Write-Host '  (пусто — отлично)' -ForegroundColor Green }
Write-Host ' -----------------------------------------------------'
Write-Host ''
Write-Host '  Готово! ПК должен будиться только кнопкой питания.' -ForegroundColor Green
Write-Host ''
Read-Host 'Нажмите Enter для закрытия'
";
}
