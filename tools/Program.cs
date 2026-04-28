using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

if (args.Length < 1)
{
    Console.WriteLine("Usage: IconGen <output-dir>");
    return;
}

var assetsDir = args[0];
Directory.CreateDirectory(assetsDir);

GenerateIcon(Path.Combine(assetsDir, "mic-idle.ico"), Color.FromArgb(60, 130, 220), 0);
GenerateIcon(Path.Combine(assetsDir, "mic-recording.ico"), Color.FromArgb(220, 50, 50), 1);
GenerateIcon(Path.Combine(assetsDir, "mic-error.ico"), Color.FromArgb(220, 170, 30), 2);

Console.WriteLine($"Icons generated in {assetsDir}/");

void GenerateIcon(string path, Color micColor, int state)
{
    var sizes = new[] { 16, 32, 48, 256 };
    var images = new List<Bitmap>();

    foreach (var size in sizes)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        var s = size / 48f;

        // Microphone capsule body
        using var brush = new SolidBrush(micColor);
        using var pen = new Pen(Color.FromArgb(40, 40, 40), Math.Max(1, 1.5f * s));
        var rect = new RectangleF(14f * s, 2f * s, 20f * s, 30f * s);
        var r = 10f * s;

        using var path2 = new GraphicsPath();
        path2.AddArc(rect.X, rect.Y, r * 2, r * 2, 180, 90);
        path2.AddArc(rect.X, rect.Y, r * 2, r * 2, 270, 90);
        path2.AddArc(rect.X, rect.Bottom - r * 2, r * 2, r * 2, 0, 90);
        path2.AddArc(rect.X, rect.Bottom - r * 2, r * 2, r * 2, 90, 90);
        path2.CloseFigure();

        g.FillPath(brush, path2);
        g.DrawPath(pen, path2);

        // Grill lines
        using var grill = new Pen(Color.White, Math.Max(1, 1f * s));
        for (int i = 0; i < 3; i++)
        {
            var ly = (12 + i * 6) * s;
            g.DrawLine(grill, (18 - i) * s, ly, (30 + i) * s, ly);
        }

        // Stand arc + base
        using var stand = new Pen(Color.FromArgb(80, 80, 80), Math.Max(1, 2f * s));
        g.DrawArc(stand, 12f * s, 34f * s, 24f * s, 10f * s, 0, 180);
        g.DrawLine(stand, 18f * s, 44f * s, 30f * s, 44f * s);

        // State indicator
        if (state == 1) // Recording
        {
            g.FillEllipse(Brushes.Red, 25f * s, 1f * s, 10f * s, 10f * s);
            g.FillEllipse(Brushes.White, 28f * s, 4f * s, 4f * s, 4f * s);
        }
        else if (state == 2) // Error
        {
            var cx = 32f * s; var cy = 6f * s; var sz = 6f * s;
            g.FillPolygon(new SolidBrush(Color.FromArgb(220, 170, 30)),
                new PointF[] { new(cx, cy - sz), new(cx - sz, cy + sz * 0.6f), new(cx + sz, cy + sz * 0.6f) });
            using var ep = new Pen(Color.Black, Math.Max(1, 1.5f * s));
            g.DrawLine(ep, cx, cy - sz * 0.3f, cx, cy + sz * 0.1f);
            g.FillEllipse(Brushes.Black, cx - 0.8f * s, cy + sz * 0.2f, 1.6f * s, 1.6f * s);
        }

        images.Add(bmp);
    }

    // Write ICO
    using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
    using var w = new BinaryWriter(fs);
    w.Write((short)0); w.Write((short)1); w.Write((short)images.Count);
    var dataOffset = 6 + images.Count * 16;
    var pngData = new List<byte[]>();
    foreach (var img in images)
    {
        using var ms = new MemoryStream();
        img.Save(ms, ImageFormat.Png);
        var bytes = ms.ToArray();
        pngData.Add(bytes);
        w.Write((byte)(img.Width >= 256 ? 0 : img.Width));
        w.Write((byte)(img.Height >= 256 ? 0 : img.Height));
        w.Write((byte)0); w.Write((byte)0);
        w.Write((short)1); w.Write((short)32);
        w.Write(bytes.Length); w.Write(dataOffset);
        dataOffset += bytes.Length;
    }
    foreach (var d in pngData) w.Write(d);
    foreach (var i in images) i.Dispose();
}
