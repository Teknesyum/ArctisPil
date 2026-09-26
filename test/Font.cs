using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public static class FontDene
{
    public static Bitmap Simge(string yazi, string aile, FontStyle stil, int n, Color renk)
    {
        Bitmap b = new Bitmap(n, n);
        using (Graphics g = Graphics.FromImage(b))
        using (GraphicsPath yol = new GraphicsPath())
        using (SolidBrush br = new SolidBrush(renk))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);
            yol.AddString(yazi, new FontFamily(aile), (int)stil, 100f, new PointF(0, 0), StringFormat.GenericTypographic);
            RectangleF k = yol.GetBounds();
            float gen = n, yuk = n;
            float sx = gen / k.Width, sy = yuk / k.Height;
            if (sx > sy * 1.35f) sx = sy * 1.35f;
            using (Matrix mt = new Matrix())
            {
                mt.Translate((n - k.Width * sx) / 2f, (n - k.Height * sy) / 2f);
                mt.Scale(sx, sy);
                mt.Translate(-k.X, -k.Y);
                yol.Transform(mt);
            }
            g.FillPath(br, yol);
        }
        return b;
    }

    public static void Tablo(string[] aileler, int[] stiller, string[] yazilar, int n, string dosya)
    {
        int z = 8, hucre = n * z + 10;
        using (Bitmap t = new Bitmap(260 + yazilar.Length * (hucre + n + 8), aileler.Length * (hucre + 4) + 4))
        using (Graphics g = Graphics.FromImage(t))
        using (Font f = new Font("Segoe UI", 13f, GraphicsUnit.Pixel))
        {
            g.Clear(Color.FromArgb(32, 32, 32));
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            for (int i = 0; i < aileler.Length; i++)
            {
                int y = 4 + i * (hucre + 4);
                g.DrawString((i + 1) + ". " + aileler[i] + ((FontStyle)stiller[i] == FontStyle.Bold ? " B" : ""), f, Brushes.White, 6, y + hucre / 2 - 8);
                for (int j = 0; j < yazilar.Length; j++)
                {
                    using (Bitmap s = Simge(yazilar[j], aileler[i], (FontStyle)stiller[i], n, Color.FromArgb(52, 211, 153)))
                    {
                        int x = 260 + j * (hucre + n + 8);
                        g.DrawImage(s, new Rectangle(x, y, n * z, n * z));
                        g.DrawImageUnscaled(s, x + n * z + 6, y + hucre / 2 - n / 2);
                    }
                }
            }
            t.Save(dosya, ImageFormat.Png);
        }
    }
}
