using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Drawing = System.Drawing;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace SeniorUtilities;

/// <summary>
/// Полноэкранный оверлей: показывает снимок экрана и даёт выделить
/// прямоугольную область. Возвращает вырезанный фрагмент (в пикселях).
/// </summary>
public partial class CaptureOverlay : Window
{
    private readonly Drawing.Bitmap _full;
    private Point _start;
    private bool _dragging;

    /// <summary>Вырезанная область (null — отмена или пустое выделение).</summary>
    public Drawing.Bitmap? Result { get; private set; }

    public CaptureOverlay(Drawing.Bitmap fullScreen, string hint)
    {
        InitializeComponent();
        _full = fullScreen;
        Hint.Text = hint;

        // Растянуть окно на весь виртуальный экран (в единицах WPF)
        Left   = SystemParameters.VirtualScreenLeft;
        Top    = SystemParameters.VirtualScreenTop;
        Width  = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;

        Shot.Source = ToBitmapSource(fullScreen);

        Loaded += (_, _) => { ResetDim(); Activate(); };
        MouseLeftButtonDown += OnDown;
        MouseMove += OnMove;
        MouseLeftButtonUp += OnUp;
        KeyDown += (_, e) => { if (e.Key == Key.Escape) { DialogResult = false; } };
    }

    private void ResetDim()
    {
        Dim.Data = new RectangleGeometry(new Rect(0, 0, Board.ActualWidth, Board.ActualHeight));
    }

    private void OnDown(object sender, MouseButtonEventArgs e)
    {
        _start = e.GetPosition(Board);
        _dragging = true;
        SelRect.Visibility = Visibility.Visible;
        CaptureMouse();
    }

    private void OnMove(object sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        var p = e.GetPosition(Board);
        double x = Math.Min(_start.X, p.X), y = Math.Min(_start.Y, p.Y);
        double w = Math.Abs(p.X - _start.X), h = Math.Abs(p.Y - _start.Y);

        System.Windows.Controls.Canvas.SetLeft(SelRect, x);
        System.Windows.Controls.Canvas.SetTop(SelRect, y);
        SelRect.Width = w;
        SelRect.Height = h;

        // «Дырка» в затемнении вокруг выделения
        var full = new RectangleGeometry(new Rect(0, 0, Board.ActualWidth, Board.ActualHeight));
        var hole = new RectangleGeometry(new Rect(x, y, w, h));
        Dim.Data = new GeometryGroup
        {
            FillRule = FillRule.EvenOdd,
            Children = { full, hole }
        };
    }

    private void OnUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false;
        ReleaseMouseCapture();

        var p = e.GetPosition(Board);
        double x = Math.Min(_start.X, p.X), y = Math.Min(_start.Y, p.Y);
        double w = Math.Abs(p.X - _start.X), h = Math.Abs(p.Y - _start.Y);

        if (w < 5 || h < 5) { DialogResult = false; return; }

        // Перевод единиц WPF → пиксели исходного снимка
        double sx = _full.Width  / Board.ActualWidth;
        double sy = _full.Height / Board.ActualHeight;
        int px = (int)Math.Round(x * sx);
        int py = (int)Math.Round(y * sy);
        int pw = (int)Math.Round(w * sx);
        int ph = (int)Math.Round(h * sy);

        px = Math.Clamp(px, 0, _full.Width - 1);
        py = Math.Clamp(py, 0, _full.Height - 1);
        pw = Math.Clamp(pw, 1, _full.Width - px);
        ph = Math.Clamp(ph, 1, _full.Height - py);

        var crop = new Drawing.Bitmap(pw, ph);
        using (var g = Drawing.Graphics.FromImage(crop))
            g.DrawImage(_full, new Drawing.Rectangle(0, 0, pw, ph),
                new Drawing.Rectangle(px, py, pw, ph), Drawing.GraphicsUnit.Pixel);

        Result = crop;
        DialogResult = true;
    }

    // ── System.Drawing.Bitmap → BitmapSource ─────────────────────────────────

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    private static BitmapSource ToBitmapSource(Drawing.Bitmap bmp)
    {
        IntPtr h = bmp.GetHbitmap();
        try
        {
            var src = Imaging.CreateBitmapSourceFromHBitmap(
                h, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            src.Freeze();
            return src;
        }
        finally { DeleteObject(h); }
    }
}
