// Standalone script to generate VoiceClip tray icons
// Run: dotnet script icon-gen.csx  OR copy into a console project

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

#pragma warning disable CA1416

var assetsDir = args.Length > 0 ? args[0] : "src/VoiceClip/Assets";
Directory.CreateDirectory(assetsDir);

GenerateIcon("mic-idle", Color.FromArgb(60, 130, 220));      // Blue
GenerateIcon("mic-recording", Color.FromArgb(220, 50, 50));   // Red
GenerateIcon("mic-error", Color.FromArgb(220, 170, 30));      // Yellow/amber

Console.WriteLine($"Icons generated in {assetsDir}/");

void GenerateIcon(string name, Color micColor)
{
    var sizes = new[] { 16, 32, 48, 256 };
    var images = new List<Bitmap>();

    foreach (var size in sizes)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        var scale = size / 48f;

        // Microphone body (rounded capsule)
        var micX = 14f * scale;
        var micY = 2f * scale;
        var micW = 20f * scale;
        var micH = 30f * scale;
        var micR = 10f * scale;

        using var micBrush = new SolidBrush(micColor);
        using var micPen = new Pen(Color.FromArgb(40, 40, 40), Math.Max(1, 1.5f * scale));

        var micRect = new RectangleF(micX, micY, micW, micH);
        using var micPath = new GraphicsPath();
        micPath.AddArc(micRect.X, micRect.Y, micR * 2, micR * 2, 180, 90);
        micPath.AddArc(micRect.X, micRect.Y, micR * 2, micR * 2, 270, 90);
        micPath.AddArc(micRect.X, micRect.Bottom - micR * 2, micR * 2, micR * 2, 0, 90);
        micPath.AddArc(micRect.X, micRect.Bottom - micR * 2, micR * 2, micR * 2, 90, 90);
        micPath.CloseFigure();

        g.FillPath(micBrush, micPath);
        g.DrawPath(micPen, micPath);

        // Microphone grill lines (3 horizontal lines on the body)
        using var grillPen = new Pen(Color.White, Math.Max(1, 1f * scale));
        for (int i = 0; i < 3; i++)
        {
            var ly = (12 + i * 6) * scale;
            var lx1 = (18 - i) * scale;
            var lx2 = (30 + i) * scale;
            g.DrawLine(grillPen, lx1, ly, lx2, ly);
        }

        // Stand (arc below mic)
        var arcY = 34f * scale;
        using var standPen = new Pen(Color.FromArgb(80, 80, 80), Math.Max(1, 2f * scale));
        g.DrawArc(standPen, 12f * scale, arcY, 24f * scale, 10f * scale, 0, 180);

        // Base line
        g.DrawLine(standPen, 18f * scale, 44f * scale, 30f * scale, 44f * scale);

        // Recording indicator dot (only for recording state)
        if (name == "mic-recording")
        {
            var dotX = 30f * scale;
            var dotY = 4f * scale;
            var dotR = 5f * scale;
            g.FillEllipse(Brushes.Red, dotX - dotR, dotY - dotR, dotR * 2, dotR * 2);
            g.FillEllipse(Brushes.White, dotX - dotR * 0.4f, dotY - dotR * 0.4f, dotR * 0.8f, dotR * 0.8f);
        }

        // Warning triangle (only for error state)
        if (name == "mic-error")
        {
            var triCx = 32f * scale;
            var triCy = 6f * scale;
            var triS = 6f * scale;
            using var triBrush = new SolidBrush(Color.FromArgb(220, 170, 30));
            var points = new PointF[]
            {
                new(triCx, triCy - triS),
                new(triCx - triS, triCy + triS * 0.6f),
                new(triCx + triS, triCy + triS * 0.6f)
            };
            g.FillPolygon(triBrush, points);
            using var exclPen = new Pen(Color.Black, Math.Max(1, 1.5f * scale));
            g.DrawLine(exclPen, triCx, triCy - triS * 0.3f, triCx, triCy + triS * 0.1f);
            g.FillEllipse(Brushes.Black, triCx - 0.8f * scale, triCy + triS * 0.2f, 1.6f * scale, 1.6f * scale);
        }

        images.Add(bmp);
    }

    // Combine into .ico file
    var iconPath = Path.Combine(assetsDir, $"{name}.ico");
    SaveAsIcon(images, iconPath);

    foreach (var img in images) img.Dispose();
}

void SaveAsIcon(List<Bitmap> images, string path)
{
    using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
    using var writer = new BinaryWriter(fs);

    // ICONDIR header
    writer.Write((short)0);       // Reserved
    writer.Write((short)1);       // Type: icon
    writer.Write((short)images.Count);

    var entrySize = 16;
    var dataOffset = 6 + images.Count * entrySize;
    var imageData = new List<byte[]>();

    foreach (var img in images)
    {
        var size = Math.Max(img.Width, img.Height);
        var pngData = ImageToPngBytes(img);
        imageData.Add(pngData);

        byte w = size >= 256 ? (byte)0 : (byte)size;
        byte h = size >= 256 ? (byte)0 : (byte)size;

        writer.Write(w);
        writer.Write(h);
        writer.Write((byte)0);    // Color palette
        writer.Write((byte)0);    // Reserved
        writer.Write((short)1);   // Color planes
        writer.Write((short)32);  // Bits per pixel
        writer.Write(pngData.Length);
        writer.Write(dataOffset);
        dataOffset += pngData.Length;
    }

    foreach (var data in imageData)
    {
        writer.Write(data);
    }
}

byte[] ImageToPngBytes(Bitmap bmp)
{
    using var ms = new MemoryStream();
    bmp.Save(ms, ImageFormat.Png);
    return ms.ToArray();
}
