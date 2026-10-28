using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace LayFlip.App;

/// <summary>Draws the tray icon in code so no external icon file is needed.</summary>
internal static class TrayIconFactory
{
    public static Icon Create()
    {
        var size = SystemInformation.SmallIconSize.Width >= 32 ? 64 : 32;
        using var bmp = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            using var background = new SolidBrush(Color.FromArgb(0x1F, 0x5F, 0xBF));
            using var path = RoundedRect(new RectangleF(0, 0, size - 1, size - 1), size / 5f);
            g.FillPath(background, path);

            using var font = new Font("Segoe UI", size * 0.36f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var format = new StringFormat(StringFormatFlags.NoWrap)
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            };
            // Left half shows the Hebrew letter, right half the Latin one.
            g.DrawString("א", font, Brushes.White, new RectangleF(0, 0, size / 2f, size), format);
            g.DrawString("A", font, Brushes.White, new RectangleF(size / 2f, 0, size / 2f, size), format);
        }

        var hIcon = bmp.GetHicon();
        try
        {
            return (Icon)Icon.FromHandle(hIcon).Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(hIcon);
        }
    }

    private static GraphicsPath RoundedRect(RectangleF r, float radius)
    {
        var d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
