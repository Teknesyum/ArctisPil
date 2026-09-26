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

[assembly: System.Reflection.AssemblyTitle(Uygulama.Ad)]
[assembly: System.Reflection.AssemblyProduct(Uygulama.Ad)]
[assembly: System.Reflection.AssemblyCompany("Teknesyum")]
[assembly: System.Reflection.AssemblyVersion(Uygulama.Surum + ".0")]
[assembly: System.Reflection.AssemblyFileVersion(Uygulama.Surum + ".0")]
[assembly: System.Reflection.AssemblyInformationalVersion(Uygulama.Surum)]

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

static class Bluetooth
{
    [StructLayout(LayoutKind.Sequential)]
    struct Anahtar { public Guid G; public int Pid; }

    [StructLayout(LayoutKind.Sequential)]
    struct Bilgi { public int cbSize; public Guid Sinif; public int Ornek; public IntPtr R; }

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr SetupDiGetClassDevs(IntPtr g, string en, IntPtr hwnd, int flags);
    [DllImport("setupapi.dll", SetLastError = true)]
    static extern bool SetupDiEnumDeviceInfo(IntPtr set, int i, ref Bilgi d);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool SetupDiGetDevicePropertyW(IntPtr set, ref Bilgi d, ref Anahtar k, out int tip, byte[] buf, int size, out int req, int flags);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool SetupDiGetDeviceInstanceId(IntPtr set, ref Bilgi d, System.Text.StringBuilder id, int size, out int req);
    [DllImport("setupapi.dll")]
    static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);

    static readonly string[] Profiller = { "{0000111E", "{0000110B", "{00001108" };

    public static bool Pil(out string ad, out int yuzde, bool hepsi = false)
    {
        ad = null; yuzde = -1;
        IntPtr set = SetupDiGetClassDevs(IntPtr.Zero, "BTHENUM", IntPtr.Zero, 0x6);
        if (set == new IntPtr(-1)) return false;
        try
        {
            Anahtar pil = new Anahtar { G = new Guid("104EA319-6EE2-4701-BD47-8DDBF425BBE5"), Pid = 2 };
            Anahtar bagli = new Anahtar { G = new Guid("83DA6326-97A6-4088-9453-A1923F573B29"), Pid = 15 };
            Anahtar isim = new Anahtar { G = new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"), Pid = 14 };
            byte[] b = new byte[512];
            System.Text.StringBuilder id = new System.Text.StringBuilder(512);
            HashSet<string> acik = new HashSet<string>();
            List<Tuple<string, int, string>> adaylar = new List<Tuple<string, int, string>>();
            Bilgi d = new Bilgi(); d.cbSize = Marshal.SizeOf(d);
            for (int i = 0; SetupDiEnumDeviceInfo(set, i, ref d); i++)
            {
                int tip, req;
                if (!SetupDiGetDeviceInstanceId(set, ref d, id, id.Capacity, out req)) continue;
                string k = id.ToString().ToUpperInvariant();
                int dv = k.IndexOf(@"\DEV_");
                if (dv >= 0 && k.Length >= dv + 17)
                {
                    if (SetupDiGetDevicePropertyW(set, ref d, ref bagli, out tip, b, b.Length, out req, 0) && req > 0 && b[0] != 0) acik.Add(k.Substring(dv + 5, 12));
                    continue;
                }
                if (!Profiller.Any(x => k.Contains(x))) continue;
                if (!SetupDiGetDevicePropertyW(set, ref d, ref pil, out tip, b, b.Length, out req, 0) || req < 1 || b[0] > 100) continue;
                int v = b[0];
                int alt = k.LastIndexOf('_');
                if (alt < 12) continue;
                string n = SetupDiGetDevicePropertyW(set, ref d, ref isim, out tip, b, b.Length, out req, 0) ? System.Text.Encoding.Unicode.GetString(b, 0, req).TrimEnd('\0') : "";
                foreach (string ek in new[] { " Hands-Free AG", " Hands-Free", " Stereo", " Avrcp Transport" }) if (n.EndsWith(ek)) n = n.Substring(0, n.Length - ek.Length);
                adaylar.Add(Tuple.Create(k.Substring(alt - 12, 12), v, n));
            }
            foreach (var a in adaylar)
            {
                if (!hepsi && !acik.Contains(a.Item1)) continue;
                yuzde = a.Item2;
                ad = a.Item3 == "" ? "Bluetooth kulaklık" : a.Item3;
                return true;
            }
        }
        finally { SetupDiDestroyDeviceInfoList(set); }
        return false;
    }
}

static class HeadsetControl
{
    public const string Surum = "4.1.0";

    static string Yol()
    {
        string yan = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "headsetcontrol.exe");
        if (File.Exists(yan)) return yan;
        using (Stream k = typeof(HeadsetControl).Assembly.GetManifestResourceStream("headsetcontrol.exe"))
        {
            if (k == null) return null;
            string klasor = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Uygulama.Ad);
            string yol = Path.Combine(klasor, "headsetcontrol-" + Surum + ".exe");
            if (File.Exists(yol) && new FileInfo(yol).Length == k.Length) return yol;
            Directory.CreateDirectory(klasor);
            using (FileStream f = File.Create(yol)) k.CopyTo(f);
            return yol;
        }
    }

    public static string Calistir(string arg, int ms)
    {
        string yol = Yol();
        if (yol == null) return null;
        System.Diagnostics.ProcessStartInfo b = new System.Diagnostics.ProcessStartInfo(yol, arg) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        using (System.Diagnostics.Process p = System.Diagnostics.Process.Start(b))
        {
            System.Threading.Tasks.Task<string> cikti = p.StandardOutput.ReadToEndAsync();
            p.StandardError.ReadToEndAsync();
            if (!p.WaitForExit(ms)) { try { p.Kill(); } catch { } return null; }
            return cikti.Result;
        }
    }

    public static bool Pil(out string ad, out int yuzde, out bool sarj)
    {
        ad = null; yuzde = -1; sarj = false;
        return Coz(Calistir("-b -o json", 8000), out ad, out yuzde, out sarj);
    }

    public static bool Coz(string j, out string ad, out int yuzde, out bool sarj)
    {
        ad = null; yuzde = -1; sarj = false;
        if (string.IsNullOrEmpty(j)) return false;
        var kok = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Dictionary<string, object>>(j);
        object o;
        if (!kok.TryGetValue("devices", out o) || !(o is System.Collections.ArrayList)) return false;
        foreach (object x in (System.Collections.ArrayList)o)
        {
            var d = x as Dictionary<string, object>;
            if (d == null || !d.TryGetValue("battery", out o)) continue;
            var pil = o as Dictionary<string, object>;
            if (pil == null) continue;
            string durum = pil.ContainsKey("status") ? pil["status"] as string : "";
            if (durum != "BATTERY_AVAILABLE" && durum != "BATTERY_CHARGING") continue;
            int v = pil.ContainsKey("level") ? Convert.ToInt32(pil["level"]) : -1;
            if (v < 0 || v > 100) continue;
            yuzde = v;
            sarj = durum == "BATTERY_CHARGING";
            ad = d.ContainsKey("device") ? d["device"] as string : null;
            if (string.IsNullOrEmpty(ad)) ad = "Kulaklık";
            return true;
        }
        return false;
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

    [ComImport, Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IOlcer
    {
        int GetPeakValue(out float p);
    }

    static object Etkinlestir(Guid iid)
    {
        IEnum en = (IEnum)new EnumCo();
        IDev d; en.GetDefaultAudioEndpoint(0, 1, out d);
        object o; d.Activate(ref iid, 23, IntPtr.Zero, out o);
        return o;
    }

    static IVol Al() { return (IVol)Etkinlestir(typeof(IVol).GUID); }

    public static float Seviye() { float v; Al().GetMasterVolumeLevelScalar(out v); return v; }
    public static void Ayarla(float v) { Guid g = Guid.Empty; Al().SetMasterVolumeLevelScalar(Math.Max(0f, Math.Min(1f, v)), ref g); }

    static IOlcer olcer;

    public static void OlcerYenile() { olcer = null; }

    public static float Tepe()
    {
        try
        {
            if (olcer == null) olcer = (IOlcer)Etkinlestir(typeof(IOlcer).GUID);
            float p; olcer.GetPeakValue(out p);
            return p;
        }
        catch { olcer = null; return 0f; }
    }
}

static class PilKaydi
{
    const long Sinir = 8L * 1024 * 1024;
    const string Baslik = "zaman,tur,ham,hal,sesli_sn,anc,kulaklik_ses,windows_ses";

    public static string Klasor { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Uygulama.Ad); } }

    public static string Yol(string anahtar)
    {
        string temiz = System.Text.RegularExpressions.Regex.Replace(anahtar.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        return Path.Combine(Klasor, "pil-" + temiz + ".csv");
    }

    public static void Yaz(string anahtar, char tur, int ham, string hal, int sesli, int anc, int kulaklik, float windows)
    {
        try
        {
            Directory.CreateDirectory(Klasor);
            string yol = Yol(anahtar);
            if (File.Exists(yol) && new FileInfo(yol).Length > Sinir)
            {
                string eski = Path.ChangeExtension(yol, ".1.csv");
                File.Delete(eski);
                File.Move(yol, eski);
            }
            if (!File.Exists(yol)) File.WriteAllText(yol, Baslik + "\n");
            File.AppendAllText(yol, string.Join(",", new[] {
                DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"), tur.ToString(), ham.ToString(), hal,
                sesli.ToString(), anc.ToString(), kulaklik.ToString(),
                windows < 0 ? "-1" : windows.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) }) + "\n");
        }
        catch { }
    }
}

class Uygulama : ApplicationContext
{
    public const string Ad = "HeadsetBatteryTray", Surum = "0.3.1";
    const string RunAnahtar = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunAd = Ad, EskiRunAd = "ArctisPil";
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
        logYol = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), Ad + ".log");
        durumYol = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "aktarma.txt");
        AcilistaGeriAl();

        ContextMenuStrip m = new ContextMenuStrip { Renderer = new MenuTemasi(), ForeColor = Yazi, ShowImageMargin = false, ShowCheckMargin = true };
        durumMenu = new ToolStripMenuItem("Bağlanıyor…") { Enabled = false };
        aktarmaMenu = new ToolStripMenuItem("Ses aktarma (20-80)", null, (s, e) => aktarmaMenu.Checked = !aktarmaMenu.Checked) { Checked = true };
        baslangicMenu = new ToolStripMenuItem("Windows ile başlat", null, (s, e) => Baslangic(!baslangicMenu.Checked));
        m.Items.Add(new ToolStripMenuItem(Ad + " v" + Surum) { Enabled = false });
        m.Items.Add(durumMenu);
        m.Items.Add(new ToolStripMenuItem("Pili şimdi yenile", null, (s, e) => Sorgula()));
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add(aktarmaMenu);
        m.Items.Add(baslangicMenu);
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add(new ToolStripMenuItem("Bize ulaşın", null, (s, e) => Git(Depo + "/issues/new/choose")));
        m.Items.Add(new ToolStripMenuItem("Teknesyum", null, (s, e) => Git(Depo)) { ForeColor = Mavi });
        m.Items.Add(new ToolStripMenuItem("Destekle", null, (s, e) => Git("https://github.com/sponsors/Teknesyum")));
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add(new ToolStripMenuItem("Çıkış", null, (s, e) => Kapat()));
        m.Opening += (s, e) => baslangicMenu.Checked = BaslangicVar();

        tepsi.ContextMenuStrip = m;
        tepsi.Text = Ad + ": bağlanıyor";
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
        pilSaat.Tick += (s, e) => { Sorgula(); DakikaKaydi(); };
        pilSaat.Start();
        sesSaat.Interval = 2000;
        sesSaat.Tick += (s, e) => { if (kayitHal == "acik" && Ses.Tepe() > 0.001f) sesliSaniye += 2; };
        sesSaat.Start();


        Thread t = new Thread(Baglanti) { IsBackground = true };
        t.Start();
    }

    readonly System.Windows.Forms.Timer sesSaat = new System.Windows.Forms.Timer();
    string kayitAnahtar, kayitHal;
    int kayitHam = -1, sesliSaniye, ancMod = -1;

    void PilKaydet(string anahtar, int ham, string hal)
    {
        bool degisti = anahtar != kayitAnahtar || ham != kayitHam || hal != kayitHal;
        kayitAnahtar = anahtar; kayitHam = ham; kayitHal = hal;
        if (degisti) Kayit('s');
    }

    void DakikaKaydi()
    {
        Ses.OlcerYenile();
        if (kayitAnahtar != null) Kayit('d');
        sesliSaniye = 0;
    }

    void Kayit(char tur)
    {
        bool nova = kayitAnahtar == NovaAnahtar;
        float w = -1f;
        try { w = Ses.Seviye(); } catch { }
        PilKaydi.Yaz(kayitAnahtar, tur, kayitHam, kayitHal, Math.Min(sesliSaniye, 60), nova ? ancMod : -1, nova ? dugme : -1, w);
    }

    const string NovaAnahtar = "1038-12e0";

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
                yedekte = false; yedekSayac = 0;
                ui.Post(_ => panel.Kisitli = false, null);
                ui.Post(_ => Sorgula(), null);
                Dinle(okuyucu, komut.C.InLen);
            }
            catch (Exception e) { if (!yedekte) Log("bağlantı: " + e.Message); }
            Kapat2();
            if (yedekSayac++ % 12 == 0) YedekBak();
            for (int i = 0; i < 50 && calisiyor; i++) Thread.Sleep(100);
        }
    }

    bool yedekte;
    int yedekSayac;

    void YedekBak()
    {
        string ad = null; int yuzde = -1; bool sarj = false;
        bool var = false;
        try { var = HeadsetControl.Pil(out ad, out yuzde, out sarj); } catch (Exception e) { Log("headsetcontrol: " + e.Message); }
        if (!var) { sarj = false; var = Bluetooth.Pil(out ad, out yuzde); }
        if (var && !yedekte) Log("yedek aygıt: " + ad);
        yedekte = var;
        ui.Post(_ =>
        {
            panel.Kisitli = true;
            if (var) PilKaydet(ad, yuzde, sarj ? "sarj" : "acik");
            else if (kayitAnahtar != null) PilKaydet(kayitAnahtar, -1, "yok");
            if (var) PilGoster(ad, yuzde, sarj);
            else { durumMenu.Text = "Kulaklık bulunamadı"; tepsi.Text = Ad + ": kulaklık yok"; Ciz("–", Yazi); panel.PilAyarla(-1, "yok", Yazi); }
        }, null);
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
                ui.Post(_ => { ancMod = an; PilGeldi(sv, du); panel.Ayarlar(an, sf, -1); }, null);
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
        PilKaydet(NovaAnahtar, seviye, du == 0x01 ? "kapali" : du == 0x02 ? "sarj" : "acik");
        int yuzde = seviye * 100 / 8;
        if (du == 0x01)
        {
            Ciz("–", Yazi);
            tepsi.Text = "Arctis: kulaklık kapalı";
            panel.PilAyarla(-1, "kapalı", Yazi);
            durumMenu.Text = "Kulaklık kapalı";
            return;
        }
        PilGoster("Arctis", yuzde, du == 0x02);
    }

    void PilGoster(string ad, int yuzde, bool sarj)
    {
        string hal = sarj ? "şarjda" : "";
        Color c = sarj ? Mavi : yuzde >= 50 ? Basari : yuzde >= 25 ? Uyari : Tehlike;
        Ciz(yuzde.ToString(), c);
        string metin = ad + " pil: %" + yuzde + (hal != "" ? " (" + hal + ")" : "");
        tepsi.Text = metin.Length > 63 ? metin.Substring(0, 63) : metin;
        durumMenu.Text = metin;
        panel.PilAyarla(yuzde, hal, c);
        if (!sarj && yuzde <= 25 && !dusukUyarildi)
        {
            dusukUyarildi = true;
            tepsi.ShowBalloonTip(5000, ad + " pili azaldı", "Kalan: %" + yuzde + ".", ToolTipIcon.Warning);
        }
        if (yuzde > 25 || sarj) dusukUyarildi = false;
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

    const string Depo = "https://github.com/Teknesyum/" + Ad;

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
            if (k.GetValue(EskiRunAd) != null) k.DeleteValue(EskiRunAd);
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
        using (Mutex mx = new Mutex(true, Uygulama.Ad + "-tek", out yeni))
        {
            if (!yeni) return;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Uygulama());
        }
    }
}

class MenuTemasi : ToolStripProfessionalRenderer
{
    static readonly Color Zemin = ColorTranslator.FromHtml("#08090a");
    static readonly Color Mavi = ColorTranslator.FromHtml("#00f3ff");
    static readonly Color Yazi = ColorTranslator.FromHtml("#ffffff");

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e) { using (SolidBrush b = new SolidBrush(Zemin)) e.Graphics.FillRectangle(b, e.AffectedBounds); }
    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e) { using (SolidBrush b = new SolidBrush(Zemin)) e.Graphics.FillRectangle(b, e.AffectedBounds); }
    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { using (Pen p = new Pen(Color.FromArgb(51, Mavi))) e.Graphics.DrawRectangle(p, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1); }
    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        Rectangle r = new Rectangle(2, 0, e.Item.Width - 4, e.Item.Height);
        using (SolidBrush b = new SolidBrush(e.Item.Selected && e.Item.Enabled ? Color.FromArgb(51, Mavi) : Zemin)) e.Graphics.FillRectangle(b, r);
    }
    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e) { using (Pen p = new Pen(Color.FromArgb(51, Mavi))) e.Graphics.DrawLine(p, 8, e.Item.Height / 2, e.Item.Width - 8, e.Item.Height / 2); }
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? e.Item.ForeColor : Color.FromArgb(128, Yazi);
        base.OnRenderItemText(e);
    }
    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        Rectangle r = e.ImageRectangle;
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using (Pen p = new Pen(Mavi, 2)) e.Graphics.DrawLines(p, new[] { new Point(r.Left + 3, r.Top + r.Height / 2), new Point(r.Left + r.Width / 2 - 1, r.Bottom - 4), new Point(r.Right - 3, r.Top + 4) });
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
    bool kisitli;

    public bool Kisitli { get { return kisitli; } set { if (kisitli == value) return; kisitli = value; Yerlestir(); } }
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
        kul.Gizli = anc.Gizli = mik.Gizli = kisitli;
        seffaf.Gizli = kisitli || anc.Secili != 1;
        ayrac = -10;
        int y = 18;
        foreach (Oge o in ogeler)
        {
            if (o == anc && !o.Gizli) { ayrac = y - 6; y += 12; }
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
