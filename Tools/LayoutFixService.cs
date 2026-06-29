using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Clipboard = System.Windows.Clipboard;

namespace SeniorUtilities.Tools;

/// <summary>
/// Глобальная горячая клавиша Pause/Break: копирует выделенный текст,
/// меняет раскладку и вставляет обратно. Регистрируется через RegisterHotKey
/// на хэндле главного окна — это НЕ кейлоггер (перехватывается одна клавиша).
/// Порт логики из layoutfix.py.
/// </summary>
public sealed class LayoutFixService : IDisposable
{
    private const int HotkeyId = 0xB001;
    private const int WmHotkey = 0x0312;
    private const uint ModNoRepeat = 0x4000;
    private const uint VkPause = 0x13;

    private const byte VkControl = 0x11;
    private const byte VkC = 0x43;
    private const byte VkV = 0x56;
    private const uint KeyEventFKeyUp = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private readonly IntPtr _hwnd;
    private readonly HwndSource _source;
    private bool _enabled;
    private bool _busy;

    public LayoutFixService(Window owner)
    {
        _hwnd = new WindowInteropHelper(owner).EnsureHandle();
        _source = HwndSource.FromHwnd(_hwnd)
                  ?? throw new InvalidOperationException("Не удалось получить HwndSource окна.");
        _source.AddHook(WndProc);
    }

    public bool IsEnabled => _enabled;

    public bool Enable()
    {
        if (_enabled) return true;
        _enabled = RegisterHotKey(_hwnd, HotkeyId, ModNoRepeat, VkPause);
        return _enabled;
    }

    public void Disable()
    {
        if (!_enabled) return;
        UnregisterHotKey(_hwnd, HotkeyId);
        _enabled = false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            _ = DoConvertAsync();
        }
        return IntPtr.Zero;
    }

    private static void Tap(byte vk)
    {
        keybd_event(vk, 0, 0, UIntPtr.Zero);
        keybd_event(vk, 0, KeyEventFKeyUp, UIntPtr.Zero);
    }

    private static void CtrlCombo(byte vk)
    {
        keybd_event(VkControl, 0, 0, UIntPtr.Zero);
        Tap(vk);
        keybd_event(VkControl, 0, KeyEventFKeyUp, UIntPtr.Zero);
    }

    private static string GetClipboardText()
    {
        for (int i = 0; i < 5; i++)
        {
            try { return Clipboard.ContainsText() ? Clipboard.GetText() : ""; }
            catch { System.Threading.Thread.Sleep(20); }
        }
        return "";
    }

    private static void SetClipboardText(string text)
    {
        for (int i = 0; i < 5; i++)
        {
            try { if (string.IsNullOrEmpty(text)) Clipboard.Clear(); else Clipboard.SetText(text); return; }
            catch { System.Threading.Thread.Sleep(20); }
        }
    }

    /// <summary>Копирует выделение, меняет раскладку, вставляет обратно.</summary>
    private async Task DoConvertAsync()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            string original = GetClipboardText();   // запомним буфер пользователя
            SetClipboardText("");                    // очистим — чтобы понять, есть ли выделение
            CtrlCombo(VkC);                           // копируем выделенный текст
            await Task.Delay(120);

            string selected = GetClipboardText();
            if (string.IsNullOrEmpty(selected))
            {
                if (!string.IsNullOrEmpty(original)) SetClipboardText(original);
                return;
            }

            string fixedText = LayoutConverter.Convert(selected);
            if (fixedText == selected)
            {
                if (!string.IsNullOrEmpty(original)) SetClipboardText(original);
                return;
            }

            SetClipboardText(fixedText);
            await Task.Delay(50);
            CtrlCombo(VkV);                           // вставляем исправленный текст

            await Task.Delay(250);
            if (!string.IsNullOrEmpty(original)) SetClipboardText(original); // вернём буфер
        }
        catch { /* не падаем из-за одной осечки буфера */ }
        finally { _busy = false; }
    }

    public void Dispose()
    {
        Disable();
        _source.RemoveHook(WndProc);
    }
}
