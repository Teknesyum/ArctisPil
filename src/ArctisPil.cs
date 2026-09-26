using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Text;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

static class Hid
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr sec, uint disp, uint flags, IntPtr tmpl);
    [DllImport("hid.dll")]
    static extern void HidD_GetHidGuid(out Guid g);
    [DllImport("hid.dll", SetLastError = true)]
    static extern bool HidD_GetPreparsedData(SafeFileHandle h, out IntPtr p);
    [DllImport("hid.dll")]
    static extern bool HidD_FreePreparsedData(IntPtr p);
    [DllImport("hid.dll")]
    static extern int HidP_GetCaps(IntPtr p, ref Caps caps);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr SetupDiGetClassDevs(ref Guid g, IntPtr en, IntPtr hwnd, int flags);
    [DllImport("setupapi.dll", SetLastError = true)]
    static extern bool SetupDiEnumDeviceInterfaces(IntPtr set, IntPtr info, ref Guid g, int idx, ref IfData d);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr set, ref IfData d, IntPtr detail, int size, out int req, IntPtr info);
    [DllImport("setupapi.dll")]
    static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);

    [StructLayout(LayoutKind.Sequential)]
    struct IfData { public int cbSize; public Guid g; public int flags; public IntPtr r; }

    [StructLayout(LayoutKind.Sequential)]
    public struct Caps
    {
        public ushort Usage, UsagePage, InLen, OutLen, FeatLen;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)] public ushort[] R;
        public ushort a, b, c, d, e, f, g, h, i, j;
    }

    public class Arayuz { public string Yol; public Caps C; }

    public static List<Arayuz> Bul()
    {
        List<Arayuz> l = new List<Arayuz>();
        Guid g; HidD_GetHidGuid(out g);
        IntPtr set = SetupDiGetClassDevs(ref g, IntPtr.Zero, IntPtr.Zero, 0x12);
        try
        {
            IfData d = new IfData(); d.cbSize = Marshal.SizeOf(d);
            for (int i = 0; SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref g, i, ref d); i++)
            {
                int req;
                SetupDiGetDeviceInterfaceDetail(set, ref d, IntPtr.Zero, 0, out req, IntPtr.Zero);
                IntPtr buf = Marshal.AllocHGlobal(req);
                try
                {
                    Marshal.WriteInt32(buf, IntPtr.Size == 8 ? 8 : 6);
                    if (!SetupDiGetDeviceInterfaceDetail(set, ref d, buf, req, out req, IntPtr.Zero)) continue;
                    string yol = Marshal.PtrToStringUni(buf + 4);
                    string k = yol.ToLowerInvariant();
                    if (!k.Contains("vid_1038&pid_12e0&mi_04")) continue;
                    Arayuz a = new Arayuz { Yol = yol };
                    if (Oku(yol, out a.C)) l.Add(a);
                }
                finally { Marshal.FreeHGlobal(buf); }
            }
        }
        finally { SetupDiDestroyDeviceInfoList(set); }
        return l;
    }

    static bool Oku(string yol, out Caps c)
    {
        c = new Caps();
        using (SafeFileHandle h = CreateFile(yol, 0, 3, IntPtr.Zero, 3, 0, IntPtr.Zero))
        {
            if (h.IsInvalid) return false;
            IntPtr p;
            if (!HidD_GetPreparsedData(h, out p)) return false;
            HidP_GetCaps(p, ref c);
            HidD_FreePreparsedData(p);
            return true;
        }
    }

    public static FileStream Ac(string yol, bool yaz)
    {
        SafeFileHandle h = CreateFile(yol, yaz ? 0xC0000000 : 0x80000000, 3, IntPtr.Zero, 3, 0x40000000, IntPtr.Zero);
        if (h.IsInvalid) throw new IOException("CreateFile " + Marshal.GetLastWin32Error());
        return new FileStream(h, yaz ? FileAccess.ReadWrite : FileAccess.Read, 1, true);
    }
}

static class Ses
{
    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    class EnumCo { }

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IEnum
    {
        int EnumAudioEndpoints(int f, int m, out IntPtr c);
        int GetDefaultAudioEndpoint(int f, int r, out IDev d);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDev
    {
        int Activate(ref Guid iid, int ctx, IntPtr p, [MarshalAs(UnmanagedType.IUnknown)] out object o);
    }

    [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IVol
    {
        int RegisterControlChangeNotify(IntPtr n);
        int UnregisterControlChangeNotify(IntPtr n);
        int GetChannelCount(out uint c);
        int SetMasterVolumeLevel(float db, ref Guid ctx);
        int SetMasterVolumeLevelScalar(float l, ref Guid ctx);
        int GetMasterVolumeLevel(out float db);
        int GetMasterVolumeLevelScalar(out float l);
    }

    static IVol Al()
    {
        IEnum en = (IEnum)new EnumCo();
        IDev d; en.GetDefaultAudioEndpoint(0, 1, out d);
        Guid iid = typeof(IVol).GUID;
        object o; d.Activate(ref iid, 23, IntPtr.Zero, out o);
        return (IVol)o;
    }

    public static float Seviye() { float v; Al().GetMasterVolumeLevelScalar(out v); return v; }
    public static void Ayarla(float v) { Guid g = Guid.Empty; Al().SetMasterVolumeLevelScalar(Math.Max(0f, Math.Min(1f, v)), ref g); }
}

class Uygulama : ApplicationContext
{
    const string RunAnahtar = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunAd = "ArctisPil";
    static readonly Color Basari = ColorTranslator.FromHtml("#34d399");
    static readonly Color Uyari = ColorTranslator.FromHtml("#fbbf24");
    static readonly Color Tehlike = ColorTranslator.FromHtml("#ff54eb");
    static readonly Color Mavi = ColorTranslator.FromHtml("#00f3ff");
    static readonly Color Yazi = ColorTranslator.FromHtml("#ffffff");

    readonly NotifyIcon tepsi = new NotifyIcon();
    readonly System.Windows.Forms.Timer pilSaat = new System.Windows.Forms.Timer();
    readonly SynchronizationContext ui;
    readonly ToolStripMenuItem aktarmaMenu;
    readonly ToolStripMenuItem baslangicMenu;
    readonly ToolStripMenuItem durumMenu;
    readonly string logYol;
    readonly string durumYol;

    FileStream yazici, okuyucu, dugmeOkuyucu;
    int cikisLen;
    volatile bool calisiyor = true;
    int seviye = -1, durum = -1;
    bool dusukUyarildi;
    int dugme = -1;
    Icon simge;
    readonly SesPaneli panel;

    public Uygulama()
    {
        ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        logYol = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "ArctisPil.log");
        durumYol = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "aktarma.txt");
        AcilistaGeriAl();

        ContextMenuStrip m = new ContextMenuStrip();
        durumMenu = new ToolStripMenuItem("Bağlanıyor…") { Enabled = false };
        aktarmaMenu = new ToolStripMenuItem("Ses aktarma (20-80)", null, (s, e) => aktarmaMenu.Checked = !aktarmaMenu.Checked) { Checked = true };
        baslangicMenu = new ToolStripMenuItem("Windows ile başlat", null, (s, e) => Baslangic(!baslangicMenu.Checked));
        m.Items.Add(durumMenu);
        m.Items.Add(new ToolStripMenuItem("Pili şimdi yenile", null, (s, e) => Sorgula()));
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add(aktarmaMenu);
        m.Items.Add(baslangicMenu);
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add(new ToolStripMenuItem("Sorun bildir", null, (s, e) => Git(Depo + "/issues/new")));
        m.Items.Add(new ToolStripMenuItem("GitHub sayfası", null, (s, e) => Git(Depo)));
        m.Items.Add(new ToolStripMenuItem("Sponsor ol", null, (s, e) => Git("https://github.com/sponsors/Teknesyum")));
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add(new ToolStripMenuItem("Çıkış", null, (s, e) => Kapat()));
        m.Opening += (s, e) => baslangicMenu.Checked = BaslangicVar();

        tepsi.ContextMenuStrip = m;
        tepsi.Text = "Arctis: bağlanıyor";
        Ciz("?", Yazi);
        tepsi.Visible = true;
        panel = new SesPaneli();
        panel.Acildi = Sorgula;
        panel.KulaklikAyar = v => { dugme = v; Gonder(0x06, 0x25, (byte)v); };
        panel.AncAyar = i => Gonder(0x06, 0xBD, (byte)i);
        panel.SeffafAyar = l => Gonder(0x06, 0xB9, (byte)l);
        panel.MikAyar = l => Gonder(0x06, 0x37, (byte)l);
        panel.Kaydet = () => { Gonder(0x06, 0x09); Log("ayarlar cihaza kaydedildi"); };
        tepsi.MouseClick += (s, e) => { if (e.Button == MouseButtons.Left) panel.Ac(); };

        if (!BaslangicVar()) Baslangic(true);

        pilSaat.Interval = 60000;
        pilSaat.Tick += (s, e) => Sorgula();
        pilSaat.Start();


        Thread t = new Thread(Baglanti) { IsBackground = true };
        t.Start();
    }

    void Log(string s)
    {
        try { File.AppendAllText(logYol, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + s + Environment.NewLine); } catch { }
    }

    void Baglanti()
    {
        while (calisiyor)
        {
            try
            {
                List<Hid.Arayuz> l = Hid.Bul();
                Hid.Arayuz komut = l.FirstOrDefault(a => a.C.UsagePage == 0xFFC0 && a.C.OutLen > 0);
                Hid.Arayuz olay = l.FirstOrDefault(a => a.C.UsagePage == 0xFF00 && a.C.InLen > 0);
                if (komut == null) throw new IOException("taban istasyonu bulunamadı");
                cikisLen = komut.C.OutLen;
                okuyucu = Hid.Ac(komut.Yol, true);
                yazici = Hid.Ac(komut.Yol, true);
                if (olay != null)
                {
                    dugmeOkuyucu = Hid.Ac(olay.Yol, false);
                    int olen = olay.C.InLen;
                    new Thread(() => Dinle(dugmeOkuyucu, olen)) { IsBackground = true }.Start();
                }
                Log("bağlandı");
                ui.Post(_ => Sorgula(), null);
                Dinle(okuyucu, komut.C.InLen);
            }
            catch (Exception e) { Log("bağlantı: " + e.Message); }
            Kapat2();
            ui.Post(_ => { durumMenu.Text = "Taban istasyonu yok"; tepsi.Text = "Arctis: bağlı değil"; Ciz("–", Yazi); }, null);
            for (int i = 0; i < 50 && calisiyor; i++) Thread.Sleep(100);
        }
    }

    void Kapat2()
    {
        foreach (FileStream f in new[] { yazici, okuyucu, dugmeOkuyucu }) try { if (f != null) f.Dispose(); } catch { }
        yazici = okuyucu = dugmeOkuyucu = null;
    }

    void Dinle(FileStream fs, int len)
    {
        byte[] b = new byte[len];
        while (calisiyor)
        {
            int n = fs.Read(b, 0, b.Length);
            if (n <= 0) continue;
            if (n > 15 && b[0] == 0x06 && b[1] == 0xB0)
            {
                int sv = b[6], du = b[15], sf = b[8], an = b[10];
                ui.Post(_ => { PilGeldi(sv, du); panel.Ayarlar(an, sf, -1); }, null);
            }
            else if (n > 18 && b[0] == 0x06 && b[1] == 0x20)
            {
                int v = b[3], mk = b[17];
                ui.Post(_ => { if (dugme < 0) dugme = v; panel.Dugme = v; panel.Ayarlar(-1, -1, mk); }, null);
            }
            else if (n > 1 && b[0] == 0x07 && b[1] != 0x25 && b[1] != 0x45)
            {
                ui.Post(_ => Sorgula(), null);
            }
            else if (n > 2 && b[0] == 0x07 && b[1] == 0x25)
            {
                int v = b[2];
                ui.Post(_ => DugmeGeldi(v), null);
            }
        }
    }

    readonly object yazKilit = new object();

    void Gonder(params byte[] k)
    {
        FileStream w = yazici;
        if (w == null) return;
        byte[] o = new byte[cikisLen];
        Array.Copy(k, o, Math.Min(k.Length, o.Length));
        try { lock (yazKilit) w.Write(o, 0, o.Length); } catch (Exception e) { Log("gönder " + BitConverter.ToString(k) + ": " + e.Message); }
    }

    void Sorgula()
    {
        Gonder(0x06, 0xB0);
        Gonder(0x06, 0x20);
    }

    void PilGeldi(int sv, int du)
    {
        if (sv != seviye || du != durum) Log(string.Format("pil: seviye {0}/8, durum 0x{1:X2}", sv, du));
        seviye = Math.Min(sv, 8); durum = du;
        int yuzde = seviye * 100 / 8;
        string hal = du == 0x02 ? "şarjda" : du == 0x01 ? "kulaklık kapalı" : "";
        if (du == 0x01)
        {
            Ciz("–", Yazi);
            tepsi.Text = "Arctis: kulaklık kapalı";
            panel.PilAyarla(-1, "kapalı", Yazi);
            durumMenu.Text = "Kulaklık kapalı";
            return;
        }
        Color c = du == 0x02 ? Mavi : yuzde >= 50 ? Basari : yuzde >= 25 ? Uyari : Tehlike;
        Ciz(yuzde.ToString(), c);
        string metin = "Arctis pil: %" + yuzde + (hal != "" ? " (" + hal + ")" : "");
        tepsi.Text = metin;
        durumMenu.Text = metin;
        panel.PilAyarla(yuzde, hal, c);
        if (du != 0x02 && yuzde <= 25 && !dusukUyarildi)
        {
            dusukUyarildi = true;
            tepsi.ShowBalloonTip(5000, "Arctis pili azaldı", "Kalan: %" + yuzde + ". Yedek pili tak.", ToolTipIcon.Warning);
        }
        if (yuzde > 25 || du == 0x02) dusukUyarildi = false;
    }

    const int Ust = 11, Alt = 45;

    void DugmeGeldi(int v)
    {
        int once = dugme;
        dugme = v;
        panel.Dugme = v;
        if (!aktarmaMenu.Checked) return;
        try
        {
            float win = Ses.Seviye();
            int hedef = v;
            if (once >= 0 && v > once && once <= Ust && win > 0.505f)
            {
                float fark = (v - once) / 56f, al = Math.Min(fark, win - 0.5f);
                win -= al;
                hedef = once + (int)Math.Round((fark - al) * 56);
            }
            else if (once >= 0 && v < once && once >= Alt && win < 0.495f)
            {
                float fark = (once - v) / 56f, ver = Math.Min(fark, 0.5f - win);
                win += ver;
                hedef = once - (int)Math.Round((fark - ver) * 56);
            }
            else if (v < Ust && win < 0.995f) { win += (Ust - v) / 56f; hedef = Ust; }
            else if (v > Alt && win > 0.005f) { win -= (v - Alt) / 56f; hedef = Alt; }
            else return;
            Ses.Ayarla(Math.Max(0f, Math.Min(1f, (float)Math.Round(win * 100) / 100f)));
            if (hedef == v) return;
            Gonder(0x06, 0x25, (byte)hedef);
            dugme = hedef;
            panel.Dugme = hedef;
        }
        catch (Exception e) { Log("aktarma: " + e.Message); }
    }

    const string Depo = "https://github.com/Teknesyum/ArctisPil";

    static void Git(string adres)
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(adres) { UseShellExecute = true }); } catch { }
    }

    [DllImport("user32.dll")]
    static extern bool DestroyIcon(IntPtr h);

    void Ciz(string yazi, Color renk)
    {
        using (Bitmap b = new Bitmap(32, 32))
        {
            using (Graphics g = Graphics.FromImage(b))
            using (System.Drawing.Drawing2D.GraphicsPath yol = new System.Drawing.Drawing2D.GraphicsPath())
            using (SolidBrush br = new SolidBrush(renk))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                FontFamily aile = FontFamily.Families.Any(f => f.Name == "Bahnschrift") ? new FontFamily("Bahnschrift") : new FontFamily("Segoe UI");
                yol.AddString(yazi, aile, (int)FontStyle.Bold, 100f, new PointF(0, 0), StringFormat.GenericTypographic);
                RectangleF k = yol.GetBounds();
                float gen = yazi.Length >= 2 ? 32f : 22f;
                float yuk = 30f;
                float olc = Math.Min(gen / k.Width, yuk / k.Height);
                float sx = yazi.Length >= 2 ? gen / k.Width : olc;
                float sy = yazi.Length >= 2 ? yuk / k.Height : olc;
                using (System.Drawing.Drawing2D.Matrix mt = new System.Drawing.Drawing2D.Matrix())
                {
                    mt.Translate((32f - k.Width * sx) / 2f, (32f - k.Height * sy) / 2f);
                    mt.Scale(sx, sy);
                    mt.Translate(-k.X, -k.Y);
                    yol.Transform(mt);
                }
                g.FillPath(br, yol);
            }
            IntPtr h = b.GetHicon();
            Icon yeni = (Icon)Icon.FromHandle(h).Clone();
            DestroyIcon(h);
            tepsi.Icon = yeni;
            if (simge != null) simge.Dispose();
            simge = yeni;
        }
    }

    static bool BaslangicVar()
    {
        using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunAnahtar))
            return k != null && k.GetValue(RunAd) != null;
    }

    static void Baslangic(bool ac)
    {
        using (RegistryKey k = Registry.CurrentUser.CreateSubKey(RunAnahtar))
        {
            if (ac) k.SetValue(RunAd, "\"" + Application.ExecutablePath + "\"");
            else if (k.GetValue(RunAd) != null) k.DeleteValue(RunAd);
        }
    }

    void Kapat()
    {
        calisiyor = false;
        Kapat2();
        tepsi.Visible = false;
        tepsi.Dispose();
        ExitThread();
    }

    void Sil() { try { File.Delete(durumYol); } catch { } }

    void AcilistaGeriAl()
    {
        try
        {
            if (!File.Exists(durumYol)) return;
            float v = float.Parse(File.ReadAllText(durumYol), System.Globalization.CultureInfo.InvariantCulture);
            if (Ses.Seviye() >= 0.99f) { Ses.Ayarla(v); Log(string.Format("açılış: yarım kalan aktarma geri alındı → %{0:F0}", v * 100)); }
        }
        catch (Exception e) { Log("açılış: " + e.Message); }
        Sil();
    }

    [STAThread]
    static void Main()
    {
        bool yeni;
        using (Mutex mx = new Mutex(true, "ArctisPil-tek", out yeni))
        {
            if (!yeni) return;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Uygulama());
        }
    }
}

class SesPaneli : Form
{
    static readonly Color Zemin = ColorTranslator.FromHtml("#08090a");
    static readonly Color Mavi = ColorTranslator.FromHtml("#00f3ff");
    static readonly Color Pembe = ColorTranslator.FromHtml("#ff00ea");
    static readonly Color Yazi = ColorTranslator.FromHtml("#ffffff");
    static readonly System.Globalization.CultureInfo Tr = new System.Globalization.CultureInfo("tr-TR");
    const int Genislik = 300, Kenar = 20, Bar = 12;

    class Oge
    {
        public string Ad;
        public string[] Secenek;
        public float Deger = -1;
        public int Secili = -1;
        public Color Renk;
        public int Y;
        public Rectangle Alan;
        public Func<float, string> Bicim;
        public Action<float> Ayar;
        public Action<int> Sec;
        public float Adim = 0.02f;
        public bool Kalici;
        public bool Kilitli;
        public bool Gizli;
    }

    readonly List<Oge> ogeler = new List<Oge>();
    readonly Oge pil, kul, win, anc, seffaf, mik;
    int ayrac, yukseklik, kulV = -1;
    Oge surukle;
    DateTime kapanis;
    readonly System.Windows.Forms.Timer yenile = new System.Windows.Forms.Timer();
    readonly System.Windows.Forms.Timer kaydetSaat = new System.Windows.Forms.Timer();

    public Action<int> KulaklikAyar, AncAyar, SeffafAyar, MikAyar;
    public Action Kaydet, Acildi;

    public SesPaneli()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Zemin;
        DoubleBuffered = true;

        pil = Cubuk("PİL", Basari(), f => "%" + Math.Round(f * 100));
        pil.Kilitli = true;
        kul = Cubuk("KULAKLIK SES", Mavi, KulMetin);
        kul.Adim = 0.05f;
        kul.Ayar = f => { int v = (int)Math.Round((1 - Bes(f)) * 56); kul.Deger = (56 - v) / 56f; if (v == kulV) return; kulV = v; if (KulaklikAyar != null) KulaklikAyar(v); };
        win = Cubuk("WINDOWS SES", Pembe, f => "%" + Math.Round(f * 100));
        win.Adim = 0.05f;
        win.Ayar = f => { float p = Bes(f); try { Ses.Ayarla(p); win.Deger = p; } catch { } };
        anc = Secim("GÜRÜLTÜ ENGELLEME", new[] { "KAPALI", "ŞEFFAF", "ANC" });
        anc.Sec = i => { Yerlestir(); if (AncAyar != null) AncAyar(i); };
        seffaf = Cubuk("ŞEFFAFLIK", Mavi, f => Math.Round(f * 10) + "/10");
        seffaf.Adim = 0.1f; seffaf.Kalici = true;
        seffaf.Ayar = f => { int l = Math.Max(1, Math.Min(10, (int)Math.Round(f * 10))); seffaf.Deger = l / 10f; if (SeffafAyar != null) SeffafAyar(l); };
        mik = Cubuk("MİKROFON", Mavi, f => Math.Round(f * 10) + "/10");
        mik.Adim = 0.1f; mik.Kalici = true;
        mik.Ayar = f => { int l = Math.Max(1, Math.Min(10, (int)Math.Round(f * 10))); mik.Deger = l / 10f; if (MikAyar != null) MikAyar(l); };
        anc.Kalici = true;
        Yerlestir();

        yenile.Interval = 200;
        yenile.Tick += (s, e) => { if (surukle != win) Oku(); Invalidate(); };
        kaydetSaat.Interval = 800;
        kaydetSaat.Tick += (s, e) => { kaydetSaat.Stop(); if (Kaydet != null) Kaydet(); };
    }

    static Color Basari() { return ColorTranslator.FromHtml("#34d399"); }

    static float Bes(float f) { return Math.Max(0f, Math.Min(1f, (float)Math.Round(f * 20) / 20f)); }

    static string KulMetin(float f)
    {
        int v = (int)Math.Round((1 - f) * 56);
        for (int k = 0; k <= 20; k++) if ((int)Math.Round((1 - k / 20f) * 56) == v) return "%" + k * 5;
        return "%" + Math.Round(f * 100);
    }

    Oge Cubuk(string ad, Color renk, Func<float, string> bicim)
    {
        Oge o = new Oge { Ad = ad, Renk = renk, Bicim = bicim };
        ogeler.Add(o);
        return o;
    }

    Oge Secim(string ad, string[] secenek)
    {
        Oge o = new Oge { Ad = ad, Secenek = secenek, Renk = Mavi };
        ogeler.Add(o);
        return o;
    }

    void Yerlestir()
    {
        seffaf.Gizli = anc.Secili != 1;
        int y = 18;
        foreach (Oge o in ogeler)
        {
            if (o == anc) { ayrac = y - 6; y += 12; }
            if (o.Gizli) continue;
            o.Y = y;
            if (o.Secenek == null) { o.Alan = new Rectangle(Kenar, y + 32, Genislik - 2 * Kenar, Bar); y += 58; }
            else { o.Alan = new Rectangle(Kenar, y + 26, Genislik - 2 * Kenar, 28); y += 66; }
        }
        if (y + 4 == yukseklik) return;
        yukseklik = y + 4;
        int alt = Bottom;
        ClientSize = new Size(Genislik, yukseklik);
        using (System.Drawing.Drawing2D.GraphicsPath p = Yuvarlak(new Rectangle(0, 0, Genislik, yukseklik), 12)) Region = new Region(p);
        if (Visible) Top = alt - yukseklik;
        Invalidate();
    }

    protected override CreateParams CreateParams { get { CreateParams c = base.CreateParams; c.ExStyle |= 0x80; return c; } }

    public void Ac()
    {
        if ((DateTime.Now - kapanis).TotalMilliseconds < 300) return;
        Rectangle alan = Screen.FromPoint(Cursor.Position).WorkingArea;
        int x = Math.Min(Math.Max(alan.Left + 8, Cursor.Position.X - Genislik / 2), alan.Right - Genislik - 8);
        Location = new Point(x, alan.Bottom - yukseklik - 8);
        Oku();
        if (Acildi != null) Acildi();
        Show();
        Activate();
        yenile.Start();
    }

    protected override void OnDeactivate(EventArgs e) { base.OnDeactivate(e); Hide(); yenile.Stop(); kapanis = DateTime.Now; surukle = null; }

    void Oku() { try { win.Deger = Ses.Seviye(); } catch { win.Deger = -1; } }

    string pilHal = "";
    public void PilAyarla(int yuzde, string hal, Color renk)
    {
        pil.Deger = yuzde < 0 ? -1 : yuzde / 100f;
        pil.Renk = renk;
        pilHal = hal;
        if (Visible) Invalidate();
    }

    public int Dugme { set { kulV = value; if (surukle != kul) kul.Deger = value < 0 ? -1 : (56 - Math.Min(value, 56)) / 56f; if (Visible) Invalidate(); } }

    public void Ayarlar(int ancMod, int seffafSeviye, int mikSeviye)
    {
        if (ancMod >= 0 && ancMod <= 2 && ancMod != anc.Secili) { anc.Secili = ancMod; Yerlestir(); }
        if (surukle != seffaf && seffafSeviye >= 1 && seffafSeviye <= 10) seffaf.Deger = seffafSeviye / 10f;
        if (surukle != mik && mikSeviye >= 1 && mikSeviye <= 10) mik.Deger = mikSeviye / 10f;
        if (Visible) Invalidate();
    }

    static System.Drawing.Drawing2D.GraphicsPath Yuvarlak(Rectangle r, int y)
    {
        System.Drawing.Drawing2D.GraphicsPath p = new System.Drawing.Drawing2D.GraphicsPath();
        int d = Math.Min(y * 2, Math.Min(r.Width, r.Height));
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    static void Doldur(Graphics g, Rectangle r, Color renk)
    {
        using (System.Drawing.Drawing2D.GraphicsPath p = Yuvarlak(r, 6))
        using (SolidBrush b = new SolidBrush(renk))
            g.FillPath(b, p);
    }

    void CubukCiz(Graphics g, Oge o)
    {
        Doldur(g, o.Alan, Color.FromArgb(51, Mavi));
        if (o.Deger <= 0) return;
        int w = Math.Max(Bar, (int)Math.Round(o.Alan.Width * Math.Min(1f, o.Deger)));
        Doldur(g, new Rectangle(o.Alan.X, o.Alan.Y, w, o.Alan.Height), o.Renk);
    }

    void SecimCiz(Graphics g, Oge o, Font f)
    {
        int n = o.Secenek.Length, ara = 4;
        int w = (o.Alan.Width - ara * (n - 1)) / n;
        for (int i = 0; i < n; i++)
        {
            Rectangle r = new Rectangle(o.Alan.X + i * (w + ara), o.Alan.Y, i == n - 1 ? o.Alan.Right - (o.Alan.X + i * (w + ara)) : w, o.Alan.Height);
            bool secili = i == o.Secili;
            Doldur(g, r, secili ? Mavi : Color.FromArgb(51, Mavi));
            TextRenderer.DrawText(g, o.Secenek[i], f, r, secili ? Zemin : Yazi, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using (Font f = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Pixel))
        using (Font fb = new Font("Segoe UI", 20f, FontStyle.Bold, GraphicsUnit.Pixel))
        using (Pen cizgi = new Pen(Color.FromArgb(51, Mavi)))
        {
            g.DrawLine(cizgi, Kenar, ayrac, Genislik - Kenar, ayrac);
            foreach (Oge o in ogeler)
            {
                if (o.Gizli) continue;
                string ad = o == pil && pilHal != "" ? "PİL · " + pilHal.ToUpper(Tr) : o.Ad;
                TextRenderer.DrawText(g, ad, f, new Point(Kenar - 1, o.Y + 6), Mavi, TextFormatFlags.NoPadding);
                if (o.Secenek != null) { SecimCiz(g, o, f); continue; }
                string deger = o.Deger < 0 ? "—" : o.Bicim(o.Deger);
                Size s = TextRenderer.MeasureText(g, deger, fb, Size.Empty, TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, deger, fb, new Point(Genislik - Kenar - s.Width, o.Y - 2), Yazi, TextFormatFlags.NoPadding);
                CubukCiz(g, o);
            }
        }
    }

    Oge Bul(Point p)
    {
        foreach (Oge o in ogeler)
        {
            Rectangle r = o.Alan;
            r.Inflate(0, o.Secenek == null ? 10 : 2);
            if (!o.Kilitli && !o.Gizli && r.Contains(p)) return o;
        }
        return null;
    }

    void Degisti(Oge o) { if (o.Kalici) { kaydetSaat.Stop(); kaydetSaat.Start(); } Invalidate(); }

    void Surukle(Oge o, int x)
    {
        float f = Math.Max(0f, Math.Min(1f, (x - o.Alan.X) / (float)o.Alan.Width));
        o.Ayar(f);
        Degisti(o);
    }

    void Tikla(Oge o, int x)
    {
        int n = o.Secenek.Length;
        int i = Math.Max(0, Math.Min(n - 1, (x - o.Alan.X) * n / o.Alan.Width));
        if (i == o.Secili) return;
        o.Secili = i;
        o.Sec(i);
        Degisti(o);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        Oge o = Bul(e.Location);
        if (o == null) return;
        if (o.Secenek != null) { Tikla(o, e.X); return; }
        surukle = o;
        Surukle(o, e.X);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        Cursor = Bul(e.Location) != null || surukle != null ? Cursors.Hand : Cursors.Default;
        if (surukle != null) Surukle(surukle, e.X);
    }

    protected override void OnMouseUp(MouseEventArgs e) { surukle = null; }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        Oge o = Bul(e.Location);
        if (o == null) return;
        int yon = e.Delta > 0 ? 1 : -1;
        if (o.Secenek != null)
        {
            int i = Math.Max(0, Math.Min(o.Secenek.Length - 1, (o.Secili < 0 ? 0 : o.Secili) + yon));
            if (i == o.Secili) return;
            o.Secili = i; o.Sec(i); Degisti(o);
            return;
        }
        if (o.Deger < 0) return;
        o.Ayar(Math.Max(0f, Math.Min(1f, o.Deger + yon * o.Adim)));
        Degisti(o);
    }
}
