using System.Drawing;
using Forms = System.Windows.Forms;

namespace SeniorUtilities.Tools;

/// <summary>Снимок экрана в физических пикселях.</summary>
public static class ScreenCapture
{
    /// <summary>Границы всего виртуального экрана (все мониторы) в пикселях.</summary>
    public static Rectangle VirtualBounds => Forms.SystemInformation.VirtualScreen;

    /// <summary>Снимок всего виртуального экрана.</summary>
    public static Bitmap CaptureVirtualScreen()
    {
        var b = VirtualBounds;
        var bmp = new Bitmap(b.Width, b.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.CopyFromScreen(b.Left, b.Top, 0, 0, b.Size, CopyPixelOperation.SourceCopy);
        return bmp;
    }
}
