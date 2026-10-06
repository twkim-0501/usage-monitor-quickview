using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace UsageMonitorQuickView;

public static class ButtonRenderer
{
    public const int Width = 40;
    public static Bitmap Render(Size size, double scale, bool light)
    {
        var image = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
        using var graphics = Graphics.FromImage(image);
        graphics.Clear(Color.Transparent); graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var s = (float)scale;
        var height = Math.Min(26 * s, size.Height - 4 * s);
        var top = (size.Height - height) / 2;
        var box = new RectangleF(4 * s, top, 32 * s, height);
        using var shape = new GraphicsPath();
        var d = 16 * s;
        shape.AddArc(box.X, box.Y, d, d, 180, 90);
        shape.AddArc(box.Right - d, box.Y, d, d, 270, 90);
        shape.AddArc(box.Right - d, box.Bottom - d, d, d, 0, 90);
        shape.AddArc(box.X, box.Bottom - d, d, d, 90, 90); shape.CloseFigure();
        using var chip = new SolidBrush(light ? Color.FromArgb(9, 20, 24, 35) : Color.FromArgb(17, 255, 255, 255));
        using var ink = new SolidBrush(light ? Color.FromArgb(245, 66, 69, 89) : Color.FromArgb(250, 233, 233, 247));
        graphics.FillPath(chip, shape);
        for (var i = 0; i < 3; i++)
        {
            var barHeight = (6 + i * 4) * s;
            graphics.FillRectangle(ink, (12 + i * 6) * s, top + height / 2 + 7 * s - barHeight, 4 * s, barHeight);
        }
        return image;
    }
}
