using System.Drawing.Drawing2D;

namespace GoodbyeDpiApp;

/// <summary>Draws the app icon (same design as app.ico) in a status color for the tray.</summary>
static class AppIcon
{
    public static Icon Create(Color background, int size)
    {
        using var bitmap = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float s = size - 1, d = size * 0.44f;
            using var shape = new GraphicsPath();
            shape.AddArc(0, 0, d, d, 180, 90);
            shape.AddArc(s - d, 0, d, d, 270, 90);
            shape.AddArc(s - d, s - d, d, d, 0, 90);
            shape.AddArc(0, s - d, d, d, 90, 90);
            shape.CloseFigure();
            using var brush = new SolidBrush(background);
            g.FillPath(brush, shape);

            using var pen = new Pen(Color.White, size * 0.11f)
            {
                StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round,
            };
            foreach (float x in new[] { 0.28f, 0.50f })
                g.DrawLines(pen, [
                    new PointF(size * x, size * 0.28f),
                    new PointF(size * (x + 0.2f), size * 0.5f),
                    new PointF(size * x, size * 0.72f),
                ]);
        }
        // The HICON is never freed; callers create a handful and keep them.
        return Icon.FromHandle(bitmap.GetHicon());
    }
}
