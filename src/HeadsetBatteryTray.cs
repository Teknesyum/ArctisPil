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

class PilTahmin
{
    public const int Seviye = 8;
    const double Ogrenme = 0.3, VarsayilanM = 12;
    static readonly System.Globalization.CultureInfo Kultur = System.Globalization.CultureInfo.InvariantCulture;

    readonly string yol;
    public double M = VarsayilanM, W = 1, Oran = 0.5;
    public readonly double[] D = new double[100];
    public readonly bool[] Ogrenildi = new bool[100];
    public int Bantlar;
    public int Kademe = -1;
    public string Hal = "";
    public bool Kesik = true;
    public double Aktif, Bos;
    DateTime son = DateTime.MinValue;
    readonly List<double[]> gecmis = new List<double[]>();

    public PilTahmin(string yol)
    {
        this.yol = yol;
        for (int i = 0; i < 100; i++) D[i] = M;
        Yukle();
    }

    public static int Ust(int k) { return Math.Max(0, k) * 100 / Seviye; }

    public int Yuzde
    {
        get
        {
            if (Kademe < 0) return -1;
            if (Hal == "sarj" || Kademe == 0) return Ust(Kademe);
            int e = Ust(Kademe), alt = Ust(Kademe - 1) + 1;
            double u = Aktif + W * Bos;
            while (e > alt && u >= D[e - 1]) { u -= D[e - 1]; e--; }
            return e;
        }
    }

    public string Aralik { get { return Kademe <= 0 ? "%0" : "%" + Ust(Kademe - 1) + "-" + Ust(Kademe); } }

    public double KalanDakika
    {
        get
        {
            if (Bantlar == 0 || Kademe <= 0 || Hal != "acik") return -1;
            double kalan = -(Aktif + W * Bos), taban = 0;
            for (int i = 0; i < Ust(Kademe); i++) kalan += D[i];
            for (int i = 0; i < Ust(Kademe - 1); i++) taban += D[i];
            double ta = gecmis.Sum(g => g[0]), tb = gecmis.Sum(g => g[1]);
            double o = ta + tb > 60 ? ta / (ta + tb) : Oran;
            return Math.Max(kalan, taban) / (o + W * (1 - o));
        }
    }

    public void Olay(DateTime t, int k, string h)
    {
        if (k < 0 || h == "yok") { if (Hal == "acik") Hal = "yok"; return; }
        if (h == "kapali") { if (Hal != "sarj") Hal = h; return; }
        if (h == "sarj") { Hal = h; Kademe = k; Kesik = true; Aktif = Bos = 0; return; }
        bool sarjdan = Hal == "sarj";
        Hal = h;
        if (k == Kademe && !sarjdan) return;
        if (Kademe < 0 || sarjdan || k > Kademe) { Capa(k, k != Seviye); return; }
        if (k == Kademe - 1 && !Kesik) Ogren();
        Capa(k, k != Kademe - 1);
    }

    void Capa(int k, bool kesik)
    {
        Kademe = k; Aktif = Bos = 0; Kesik = kesik;
    }

    public void Dakika(DateTime t, double sesli)
    {
        if (son == DateTime.MinValue || t <= son) { son = t; return; }
        double dt = (t - son).TotalMinutes;
        son = t;
        if (Hal != "acik" || Kademe <= 0) return;
        if (dt > 180) { Kesik = true; return; }
        if (dt > 2) { Bos += dt - 1; dt = 1; }
        double s = Math.Max(0, Math.Min(1, sesli));
        Aktif += dt * s;
        Bos += dt * (1 - s);
        Oran += 0.05 * (s - Oran);
    }

    void Ogren()
    {
        int ust = Ust(Kademe), alt = Ust(Kademe - 1), n = ust - alt;
        gecmis.Add(new[] { Aktif, Bos, n });
        if (gecmis.Count > 12) gecmis.RemoveAt(0);
        WOgren();
        double u = Aktif + W * Bos;
        if (u < 1) return;
        double bek = 0;
        for (int i = alt; i < ust; i++) bek += D[i];
        double f = 1 + Ogrenme * (u / bek - 1);
        for (int i = alt; i < ust; i++) { D[i] *= f; Ogrenildi[i] = true; }
        M += Ogrenme * (u / n - M);
        for (int i = 0; i < 100; i++) if (!Ogrenildi[i]) D[i] = M;
        Bantlar++;
    }

    void WOgren()
    {
        if (gecmis.Count < 3) return;
        double aa = 0, ai = 0, ii = 0, an = 0, iN = 0;
        foreach (double[] g in gecmis) { aa += g[0] * g[0]; ai += g[0] * g[1]; ii += g[1] * g[1]; an += g[0] * g[2]; iN += g[1] * g[2]; }
        double det = aa * ii - ai * ai;
        if (det <= 0.01 * aa * ii) return;
        double a = (an * ii - iN * ai) / det, b = (aa * iN - ai * an) / det;
        if (a > 0 && b > 0) W = Math.Max(0.1, Math.Min(2, b / a));
    }

    public void Oynat(string csv)
    {
        foreach (string satir in File.ReadAllLines(csv).Skip(1))
        {
            string[] a = satir.Split(',');
            if (a.Length < 5) continue;
            DateTime t;
            if (!DateTime.TryParseExact(a[0], "yyyy-MM-ddTHH:mm:ss", Kultur, System.Globalization.DateTimeStyles.None, out t)) continue;
            int ham, sesli;
            int.TryParse(a[2], out ham); int.TryParse(a[4], out sesli);
            Olay(t, ham, a[3]);
            if (a[1] == "d") Dakika(t, sesli / 60.0);
        }
    }

    string Sayi(double v) { return v.ToString("0.####", Kultur); }

    public void Kaydet()
    {
        try
        {
            List<string> l = new List<string> {
                "m=" + Sayi(M), "w=" + Sayi(W), "oran=" + Sayi(Oran), "bantlar=" + Bantlar,
                "kademe=" + Kademe, "hal=" + Hal, "kesik=" + (Kesik ? 1 : 0), "aktif=" + Sayi(Aktif), "bos=" + Sayi(Bos),
                "son=" + (son == DateTime.MinValue ? "" : son.ToString("yyyy-MM-ddTHH:mm:ss", Kultur)),
                "d=" + string.Join(";", D.Select(Sayi)),
                "ogrenildi=" + string.Concat(Ogrenildi.Select(b => b ? "1" : "0")),
                "gecmis=" + string.Join(";", gecmis.Select(g => string.Join(":", g.Select(Sayi)))) };
            Directory.CreateDirectory(Path.GetDirectoryName(yol));
            File.WriteAllLines(yol, l);
        }
        catch { }
    }

    void Yukle()
    {
        if (!File.Exists(yol)) return;
        try
        {
            Dictionary<string, string> v = new Dictionary<string, string>();
            foreach (string s in File.ReadAllLines(yol)) { int i = s.IndexOf('='); if (i > 0) v[s.Substring(0, i)] = s.Substring(i + 1); }
            Func<string, double> sayi = k => double.Parse(v[k], Kultur);
            M = sayi("m"); W = sayi("w"); Oran = sayi("oran"); Bantlar = int.Parse(v["bantlar"]);
            Kademe = int.Parse(v["kademe"]); Hal = v["hal"]; Kesik = v["kesik"] == "1"; Aktif = sayi("aktif"); Bos = sayi("bos");
            if (v["son"] != "") son = DateTime.ParseExact(v["son"], "yyyy-MM-ddTHH:mm:ss", Kultur);
            string[] d = v["d"].Split(';');
            for (int i = 0; i < 100 && i < d.Length; i++) { D[i] = double.Parse(d[i], Kultur); Ogrenildi[i] = v["ogrenildi"][i] == '1'; }
            foreach (string g in v["gecmis"].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                gecmis.Add(g.Split(':').Select(x => double.Parse(x, Kultur)).ToArray());
        }
        catch { }
    }

    public static PilTahmin Ac(string anahtar)
    {
        string csv = PilKaydi.Yol(anahtar), txt = Path.ChangeExtension(csv, ".txt");
        bool yeni = !File.Exists(txt);
        PilTahmin p = new PilTahmin(txt);
        if (yeni && File.Exists(csv)) { try { p.Oynat(csv); } catch { } p.Kaydet(); }
        return p;
    }
}

class Uygulama : ApplicationContext
{
    public const string Ad = "HeadsetBatteryTray", Surum = "0.4.0";
    const string RunAnahtar = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunAd = Ad, EskiRunAd = "ArctisPil";

    readonly NotifyIcon tepsi = new NotifyIcon();
    readonly System.Windows.Forms.Timer pilSaat = new System.Windows.Forms.Timer();
    readonly SynchronizationContext ui;
    readonly ToolStripMenuItem aktarmaMenu;
    readonly ToolStripMenuItem baslangicMenu;
    readonly ToolStripMenuItem durumMenu;
    readonly ToolStripMenuItem tahminMenu;
    readonly PilTahmin tahmin = PilTahmin.Ac(NovaAnahtar);
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

        ContextMenuStrip m = new ContextMenuStrip();
        MenuTemasi.Uygula(m);
        durumMenu = new ToolStripMenuItem("Bağlanıyor…") { Enabled = false };
        aktarmaMenu = new ToolStripMenuItem("Ses aktarma (20-80)", null, (s, e) => aktarmaMenu.Checked = !aktarmaMenu.Checked) { Checked = true };
        tahminMenu = new ToolStripMenuItem("Pil tahmini (%1 adım)", null, (s, e) => { tahminMenu.Checked = !tahminMenu.Checked; TahminKaydet(tahminMenu.Checked); if (seviye >= 0 && durum != 0x01) PilGoster2(); }) { Checked = TahminAcik() };
        baslangicMenu = new ToolStripMenuItem("Windows ile başlat", null, (s, e) => Baslangic(!baslangicMenu.Checked));
        m.Items.Add(new ToolStripMenuItem(Ad + " v" + Surum) { Enabled = false });
        m.Items.Add(durumMenu);
        m.Items.Add(new ToolStripMenuItem("Pili şimdi yenile", null, (s, e) => Sorgula()));
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add(aktarmaMenu);
        m.Items.Add(tahminMenu);
        m.Items.Add(baslangicMenu);
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add(new ToolStripMenuItem("Bize ulaşın", null, (s, e) => Git(Depo + "/issues/new/choose")));
        m.Items.Add(new ToolStripMenuItem("Teknesyum", null, (s, e) => Git(Depo)) { ForeColor = Tema.Renk1 });
        m.Items.Add(new ToolStripMenuItem("Destekle", null, (s, e) => Git("https://github.com/sponsors/Teknesyum")));
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add(new ToolStripMenuItem("Çıkış", null, (s, e) => Kapat()));
        m.Opening += (s, e) => baslangicMenu.Checked = BaslangicVar();

        tepsi.ContextMenuStrip = m;
        tepsi.Text = Ad + ": bağlanıyor";
        Ciz("?", Tema.TextBody);
        tepsi.Visible = true;
        panel = new SesPaneli();
        panel.Acildi = Sorgula;
        panel.KulaklikAyar = v => { dugme = v; Gonder(0x06, 0x25, (byte)v); };
        panel.AncAyar = i => Gonder(0x06, 0xBD, (byte)i);
        panel.SeffafAyar = l => Gonder(0x06, 0xB9, (byte)l);
        panel.MikAyar = l => Gonder(0x06, 0x37, (byte)l);
        panel.Kaydet = () => { Gonder(0x06, 0x09); Log("ayarlar cihaza kaydedildi"); };
        panel.YenidenAra = () => { Sorgula(); if (panel.Kisitli) ThreadPool.QueueUserWorkItem(_ => YedekBak()); };
        EventWaitHandle acSinyal = new EventWaitHandle(false, EventResetMode.AutoReset, Ad + "-ac");
        new Thread(() => { while (calisiyor) if (acSinyal.WaitOne(1000)) ui.Post(_ => panel.Ac(true), null); }) { IsBackground = true }.Start();
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
        if (anahtar == NovaAnahtar) tahmin.Olay(DateTime.Now, ham, hal);
        bool degisti = anahtar != kayitAnahtar || ham != kayitHam || hal != kayitHal;
        kayitAnahtar = anahtar; kayitHam = ham; kayitHal = hal;
        if (degisti) Kayit('s');
    }

    void DakikaKaydi()
    {
        Ses.OlcerYenile();
        if (kayitAnahtar != null) Kayit('d');
        if (kayitAnahtar == NovaAnahtar)
        {
            tahmin.Dakika(DateTime.Now, Math.Min(sesliSaniye, 60) / 60.0);
            tahmin.Kaydet();
            if (seviye >= 0 && durum != 0x01 && !yedekte) PilGoster2();
        }
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
            else { durumMenu.Text = "Kulaklık bulunamadı"; tepsi.Text = Ad + ": kulaklık yok"; Ciz("–", Tema.TextBody); panel.PilAyarla(-1, "yok"); }
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
        if (du == 0x01)
        {
            Ciz("–", Tema.TextBody);
            tepsi.Text = "Arctis: kulaklık kapalı";
            panel.PilAyarla(-1, "kapalı");
            durumMenu.Text = "Kulaklık kapalı";
            return;
        }
        PilGoster2();
    }

    void PilGoster2()
    {
        bool sarj = durum == 0x02;
        if (!tahminMenu.Checked || sarj || tahmin.Yuzde < 0) { PilGoster("Arctis", seviye * 100 / 8, sarj, null); return; }
        double k = tahmin.KalanDakika;
        string ek = " (cihaz " + tahmin.Aralik + ")" + (k >= 0 ? ", ~" + Sure(k) + " kaldı" : tahmin.Bantlar == 0 ? ", öğreniyor" : "");
        PilGoster("Arctis", tahmin.Yuzde, false, ek);
    }

    static string Sure(double dk)
    {
        int d = (int)Math.Round(dk / 10) * 10;
        return d >= 60 ? (d / 60 + " sa " + (d % 60 > 0 ? d % 60 + " dk" : "")).TrimEnd() : Math.Max(d, 10) + " dk";
    }

    const string AyarAnahtar = @"Software\" + Ad;

    static bool TahminAcik()
    {
        try { using (RegistryKey k = Registry.CurrentUser.OpenSubKey(AyarAnahtar)) return k == null || !(k.GetValue("Tahmin") is int) || (int)k.GetValue("Tahmin") != 0; }
        catch { return true; }
    }

    static void TahminKaydet(bool acik)
    {
        try { using (RegistryKey k = Registry.CurrentUser.CreateSubKey(AyarAnahtar)) k.SetValue("Tahmin", acik ? 1 : 0, RegistryValueKind.DWord); }
        catch { }
    }

    void PilGoster(string ad, int yuzde, bool sarj, string ek = null)
    {
        string hal = sarj ? "şarjda" : "";
        Color c = sarj ? Tema.TextBody : Tema.PilYazi(yuzde, sarj);
        Ciz(yuzde.ToString(), c);
        string metin = ad + " pil: " + (ek != null ? "~" : "") + "%" + yuzde + (hal != "" ? " (" + hal + ")" : "") + (ek ?? "");
        tepsi.Text = metin.Length > 63 ? metin.Substring(0, 63) : metin;
        durumMenu.Text = metin;
        panel.PilAyarla(yuzde, hal);
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

    [DllImport("user32.dll")]
    static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")]
    static extern bool AllowSetForegroundWindow(int p);

    [STAThread]
    static void Main()
    {
        bool yeni;
        using (Mutex mx = new Mutex(true, Uygulama.Ad + "-tek", out yeni))
        {
            if (!yeni)
            {
                try { AllowSetForegroundWindow(-1); using (EventWaitHandle h = EventWaitHandle.OpenExisting(Uygulama.Ad + "-ac")) h.Set(); } catch { }
                return;
            }
            try { SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Uygulama());
        }
    }
}

static class Tema
{
    public static readonly Color Renk1 = ColorTranslator.FromHtml("#6FB7FF");
    public static readonly Color Renk2 = ColorTranslator.FromHtml("#CBA7D2");
    public static readonly Color Renk3 = ColorTranslator.FromHtml("#C3A3FF");
    public static readonly Color Success = ColorTranslator.FromHtml("#66F09A");
    public static readonly Color Renk2Text = ColorTranslator.FromHtml("#FA8CFF");
    public static readonly Color Danger = Renk2;
    public static readonly Color DangerText = Renk2Text;
    public static readonly Color Warning = ColorTranslator.FromHtml("#FFD24D");
    public static readonly Color Surface = ColorTranslator.FromHtml("#000000");
    public static readonly Color TextBody = ColorTranslator.FromHtml("#FFFFFF");
    public static readonly Color TextLabel = Renk1;
    public static readonly Color FocusRing = Renk1;
    public static readonly Color BorderDefault = Color.FromArgb(0x8C, Renk1);
    public static readonly Color BorderDecorative = Color.FromArgb(0x33, Renk1);
    public static readonly Color Renk1Yuzde20 = Color.FromArgb(0x33, Renk1);
    public static readonly Color Renk1Yuzde30 = Color.FromArgb(0x4D, Renk1);

    public const int Radius = 3, WindowRadius = 3, BorderWidth = 1, FocusWidth = 2, FocusOffset = 2;
    public const int FontSize1 = 14, FontSize2 = 16, FontSize3 = 20;
    public const double LineHeightHeading = 1.25;
    public const int Space1 = 4, Space2 = 8, Space3 = 12;
    public const int PanelPadding = 24, SectionGap = 24, RowGap = 12, FieldGap = 8;
    public const int TargetMin = 24, InputHeight = 40, EntryOffset = 8, FrameBudgetMs = 16;
    public const float ScaleHover = 1.02f, ScalePress = 0.98f;
    public const int Fast = 80, Base = 150, Slow = 240;
    public static readonly double[] Cik = { 0.2, 0, 0, 1 };
    public static readonly double[] Gir = { 0.4, 0, 1, 1 };
    public static readonly double[] GirCik = { 0.4, 0, 0.2, 1 };

    [DllImport("user32.dll")]
    static extern bool SystemParametersInfo(uint a, uint b, out bool c, uint d);
    [DllImport("gdi32.dll")]
    static extern IntPtr AddFontMemResourceEx(IntPtr pb, uint cb, IntPtr pdv, out uint n);

    public static bool Hareket = HareketAcik();

    static bool HareketAcik()
    {
        bool v;
        try { if (SystemParametersInfo(0x1042, 0, out v, 0)) return v; } catch { }
        return true;
    }

    static float olcek;
    public static float Olcek
    {
        get
        {
            if (olcek > 0) return olcek;
            try { using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) olcek = g.DpiX / 96f; } catch { olcek = 1f; }
            return olcek;
        }
    }

    static PrivateFontCollection pfc;
    static readonly List<IntPtr> bellek = new List<IntPtr>();
    static FontFamily sans, sansYari;
    static string mono;

    static void Yukle()
    {
        if (pfc != null) return;
        pfc = new PrivateFontCollection();
        System.Reflection.Assembly a = typeof(Tema).Assembly;
        foreach (string ad in a.GetManifestResourceNames())
        {
            if (!ad.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)) continue;
            try
            {
                byte[] v;
                using (Stream s = a.GetManifestResourceStream(ad))
                using (MemoryStream m = new MemoryStream()) { s.CopyTo(m); v = m.ToArray(); }
                IntPtr p = Marshal.AllocCoTaskMem(v.Length);
                Marshal.Copy(v, 0, p, v.Length);
                pfc.AddMemoryFont(p, v.Length);
                uint n;
                AddFontMemResourceEx(p, (uint)v.Length, IntPtr.Zero, out n);
                bellek.Add(p);
            }
            catch { }
        }
        foreach (FontFamily f in pfc.Families)
        {
            if (f.Name == "Atkinson Hyperlegible Next") sans = f;
            else if (f.Name.StartsWith("Atkinson Hyperlegible Next ")) sansYari = f;
        }
        if (sans == null) sans = new FontFamily("Segoe UI");
        using (InstalledFontCollection y = new InstalledFontCollection())
            mono = y.Families.Any(f => f.Name == "Cascadia Mono") ? "Cascadia Mono" : "Consolas";
    }

    public static Font Sans(float px, bool yari)
    {
        Yukle();
        if (yari && sansYari != null) return new Font(sansYari, px, FontStyle.Regular, GraphicsUnit.Pixel);
        return new Font(sans, px, yari ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
    }

    public static Font Mono(float px) { Yukle(); return new Font(mono, px, FontStyle.Bold, GraphicsUnit.Pixel); }

    public static string Aileler() { Yukle(); return (sans != null ? sans.Name : "-") + " | " + (sansYari != null ? sansYari.Name : "-") + " | " + mono; }

    public static Color PilYazi(int yuzde, bool sarj) { return sarj ? Renk1 : yuzde >= 50 ? Success : yuzde >= 25 ? Warning : DangerText; }
    public static Color PilDolgu(int yuzde, bool sarj) { return sarj ? Renk1 : yuzde >= 50 ? Success : yuzde >= 25 ? Renk1 : Danger; }

    public static double Egri(double[] e, double t)
    {
        if (t <= 0) return 0;
        if (t >= 1) return 1;
        double a = 0, b = 1, s = t;
        for (int i = 0; i < 24; i++) { s = (a + b) / 2; if (Bz(e[0], e[2], s) < t) a = s; else b = s; }
        return Bz(e[1], e[3], s);
    }

    static double Bz(double p1, double p2, double s) { double u = 1 - s; return 3 * u * u * s * p1 + 3 * u * s * s * p2 + s * s * s; }

    public static Color Karistir(Color a, Color b, float t)
    {
        t = Math.Max(0f, Math.Min(1f, t));
        return Color.FromArgb((int)Math.Round(a.A + (b.A - a.A) * t), (int)Math.Round(a.R + (b.R - a.R) * t), (int)Math.Round(a.G + (b.G - a.G) * t), (int)Math.Round(a.B + (b.B - a.B) * t));
    }

    public static System.Drawing.Drawing2D.GraphicsPath Yuvarlak(RectangleF r, float y)
    {
        System.Drawing.Drawing2D.GraphicsPath p = new System.Drawing.Drawing2D.GraphicsPath();
        float d = Math.Min(y * 2, Math.Min(r.Width, r.Height));
        if (d <= 0) { p.AddRectangle(r); return p; }
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}

class Gecis
{
    float bas, hedef;
    int t0, sure;
    double[] egri = Tema.Cik;

    public Gecis(float v) { bas = hedef = v; }
    public float Hedef { get { return hedef; } }
    public void Ata(float v) { bas = hedef = v; sure = 0; }

    public void Git(float v, int ms, double[] e)
    {
        if (v == hedef) return;
        bas = Deger; hedef = v; egri = e; t0 = Environment.TickCount;
        sure = Tema.Hareket ? ms : 0;
    }

    public bool Suruyor { get { return sure > 0 && Environment.TickCount - t0 < sure; } }

    public float Deger
    {
        get
        {
            if (!Suruyor) return hedef;
            double t = (Environment.TickCount - t0) / (double)sure;
            return bas + (hedef - bas) * (float)Tema.Egri(egri, t);
        }
    }
}

class MenuTemasi : ToolStripProfessionalRenderer
{
    public static void Uygula(ContextMenuStrip m)
    {
        m.Renderer = new MenuTemasi();
        m.ForeColor = Tema.TextBody;
        m.BackColor = Tema.Surface;
        m.Font = Tema.Sans(Tema.FontSize2 * Tema.Olcek, false);
        m.ShowImageMargin = false;
        m.ShowCheckMargin = true;
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e) { using (SolidBrush b = new SolidBrush(Tema.Surface)) e.Graphics.FillRectangle(b, e.AffectedBounds); }
    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e) { using (SolidBrush b = new SolidBrush(Tema.Surface)) e.Graphics.FillRectangle(b, e.AffectedBounds); }
    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { using (Pen p = new Pen(Tema.BorderDefault)) e.Graphics.DrawRectangle(p, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1); }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        Rectangle r = new Rectangle(2, 0, e.Item.Width - 4, e.Item.Height);
        using (SolidBrush b = new SolidBrush(Tema.Surface)) e.Graphics.FillRectangle(b, r);
        if (!e.Item.Selected || !e.Item.Enabled) return;
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using (System.Drawing.Drawing2D.GraphicsPath p = Tema.Yuvarlak(r, Tema.Radius * Tema.Olcek))
        using (SolidBrush b = new SolidBrush(Tema.Renk1Yuzde20)) e.Graphics.FillPath(b, p);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e) { using (Pen p = new Pen(Tema.BorderDecorative)) e.Graphics.DrawLine(p, 8, e.Item.Height / 2, e.Item.Width - 8, e.Item.Height / 2); }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont, e.TextRectangle, e.Item.Enabled ? e.Item.ForeColor : Tema.TextBody, e.TextFormat);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        Rectangle r = e.ImageRectangle;
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using (Pen p = new Pen(Tema.Renk1, Tema.FocusWidth * Tema.Olcek)) e.Graphics.DrawLines(p, new[] { new Point(r.Left + 3, r.Top + r.Height / 2), new Point(r.Left + r.Width / 2 - 1, r.Bottom - 4), new Point(r.Right - 3, r.Top + 4) });
    }
}

class SesPaneli : Form
{
    const int Genislik = 300;

    class Oge
    {
        public string Ad;
        public string[] Secenek;
        public float Deger = -1;
        public int Secili = -1;
        public Color Renk = Tema.Renk1;
        public Color YaziRenk = Tema.TextBody;
        public int Y;
        public Rectangle Alan;
        public Func<float, string> Bicim;
        public Action<float> Ayar;
        public Action<int> Sec;
        public Action Eylem;
        public string Ipucu;
        public float Adim = 0.02f;
        public bool Kalici, Kilitli, Gizli;
        public int UzerindeSec = -1;
        public readonly Gecis Dolu = new Gecis(-1), Kay = new Gecis(-1), Uzerinde = new Gecis(0), Boy = new Gecis(1);
        public bool Odaklanir { get { return !Gizli && !Kilitli && Ipucu == null; } }
    }

    readonly List<Oge> ogeler = new List<Oge>();
    readonly Oge pil, ipucu, dugmeOge, kul, win, anc, seffaf, mik;
    int ayrac = -1, baslik, kulV = -1, hedefX, hedefY, sayac;
    bool kisitli, klavye, kapaniyor;
    string pilHal;
    float olcek = Tema.Olcek;
    Font fEtiket, fDeger, fGovde, fDugme;
    Oge surukle, odak, uzerinde, basili;
    DateTime kapanis;
    readonly Gecis acilis = new Gecis(1);
    readonly System.Windows.Forms.Timer kare = new System.Windows.Forms.Timer();
    readonly System.Windows.Forms.Timer kaydetSaat = new System.Windows.Forms.Timer();

    public Action<int> KulaklikAyar, AncAyar, SeffafAyar, MikAyar;
    public Action Kaydet, Acildi, YenidenAra;

    public bool Kisitli { get { return kisitli; } set { if (kisitli == value) return; kisitli = value; Yerlestir(); } }
    public float Olcek { get { return olcek; } set { olcek = value; FontlariKur(); Yerlestir(); } }

    public SesPaneli()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Tema.Surface;
        DoubleBuffered = true;
        KeyPreview = true;

        pil = Cubuk("Pil", f => "%" + Math.Round(f * 100));
        pil.Kilitli = true;
        ipucu = new Oge { Ipucu = "", Kilitli = true };
        ogeler.Add(ipucu);
        dugmeOge = new Oge { Ad = "Yeniden ara", Eylem = () => { if (YenidenAra != null) YenidenAra(); } };
        ogeler.Add(dugmeOge);
        kul = Cubuk("Kulaklık sesi", KulMetin);
        kul.Adim = 0.05f;
        kul.Ayar = f => { int v = (int)Math.Round((1 - Bes(f)) * 56); kul.Deger = (56 - v) / 56f; if (v == kulV) return; kulV = v; if (KulaklikAyar != null) KulaklikAyar(v); };
        win = Cubuk("Windows sesi", f => "%" + Math.Round(f * 100));
        win.Renk = Tema.Renk3;
        win.Adim = 0.05f;
        win.Ayar = f => { float p = Bes(f); try { Ses.Ayarla(p); win.Deger = p; } catch { } };
        anc = Secim("Gürültü engelleme", new[] { "Kapalı", "Şeffaf", "ANC" });
        anc.Sec = i => { Yerlestir(); if (AncAyar != null) AncAyar(i); };
        anc.Kalici = true;
        seffaf = Cubuk("Şeffaflık", f => Math.Round(f * 10) + "/10");
        seffaf.Adim = 0.1f; seffaf.Kalici = true;
        seffaf.Ayar = f => { int l = Math.Max(1, Math.Min(10, (int)Math.Round(f * 10))); seffaf.Deger = l / 10f; if (SeffafAyar != null) SeffafAyar(l); };
        mik = Cubuk("Mikrofon", f => Math.Round(f * 10) + "/10");
        mik.Adim = 0.1f; mik.Kalici = true;
        mik.Ayar = f => { int l = Math.Max(1, Math.Min(10, (int)Math.Round(f * 10))); mik.Deger = l / 10f; if (MikAyar != null) MikAyar(l); };
        FontlariKur();
        Yerlestir();

        kare.Interval = Tema.FrameBudgetMs;
        kare.Tick += (s, e) => Kare();
        kaydetSaat.Interval = 800;
        kaydetSaat.Tick += (s, e) => { kaydetSaat.Stop(); if (Kaydet != null) Kaydet(); };
    }

    int D(double v) { return (int)Math.Round(v * olcek); }

    void FontlariKur()
    {
        foreach (Font f in new[] { fEtiket, fDeger, fGovde, fDugme }) if (f != null) f.Dispose();
        fEtiket = Tema.Sans(Tema.FontSize1 * olcek, true);
        fDeger = Tema.Mono(Tema.FontSize3 * olcek);
        fGovde = Tema.Sans(Tema.FontSize2 * olcek, false);
        fDugme = Tema.Sans(Tema.FontSize2 * olcek, true);
    }

    static float Bes(float f) { return Math.Max(0f, Math.Min(1f, (float)Math.Round(f * 20) / 20f)); }

    static string KulMetin(float f)
    {
        int v = (int)Math.Round((1 - f) * 56);
        for (int k = 0; k <= 20; k++) if ((int)Math.Round((1 - k / 20f) * 56) == v) return "%" + k * 5;
        return "%" + Math.Round(f * 100);
    }

    Oge Cubuk(string ad, Func<float, string> bicim)
    {
        Oge o = new Oge { Ad = ad, Bicim = bicim };
        ogeler.Add(o);
        return o;
    }

    Oge Secim(string ad, string[] secenek)
    {
        Oge o = new Oge { Ad = ad, Secenek = secenek };
        ogeler.Add(o);
        return o;
    }

    static readonly Bitmap olcu = new Bitmap(1, 1);

    static StringFormat Bicim(StringAlignment yatay, StringAlignment dikey, bool sar)
    {
        StringFormat f = new StringFormat(StringFormat.GenericTypographic);
        f.Alignment = yatay;
        f.LineAlignment = dikey;
        f.FormatFlags |= StringFormatFlags.NoClip;
        if (!sar) f.FormatFlags |= StringFormatFlags.NoWrap;
        return f;
    }

    void Yerlestir()
    {
        string ip = null, dg = null;
        if (pilHal == "yok") { ip = "Kulaklık bulunamadı. Bağlayıp açın; desteklenmiyorsa bize yazın."; dg = "Yeniden ara"; }
        else if (pilHal == "kapalı") { ip = "Kulaklık kapalı. Açınca pil burada görünür."; dg = "Yeniden dene"; }
        else if (pil.Deger < 0) ip = "Pil okunuyor…";
        else if (kisitli) ip = "Bu kulaklıkta yalnız pil okunur. Ses ve gürültü ayarları Arctis Nova Pro Wireless içindir.";
        ipucu.Ipucu = ip ?? "";
        ipucu.Gizli = ip == null;
        dugmeOge.Gizli = dg == null;
        if (dg != null) dugmeOge.Ad = dg;
        kul.Gizli = anc.Gizli = mik.Gizli = kisitli;
        seffaf.Gizli = kisitli || anc.Secili != 1;
        if (odak != null && !odak.Odaklanir) odak = null;

        int k = D(Tema.PanelPadding), gen = D(Genislik), ic = gen - 2 * k, satir = D(Tema.RowGap);
        baslik = D(Tema.FontSize3 * Tema.LineHeightHeading);
        int ek = D((Tema.TargetMin - Tema.Space3) / 2);
        ayrac = -1;
        int y = k;
        foreach (Oge o in ogeler)
        {
            if (o.Gizli) continue;
            if (o == anc) { ayrac = y + (D(Tema.SectionGap) - satir) / 2 - satir / 2; y += D(Tema.SectionGap) - satir; }
            o.Y = y;
            if (o.Ipucu != null)
            {
                int h;
                using (Graphics g = Graphics.FromImage(olcu))
                using (StringFormat sf = Bicim(StringAlignment.Near, StringAlignment.Near, true))
                    h = (int)Math.Ceiling(g.MeasureString(o.Ipucu, fGovde, ic, sf).Height);
                o.Alan = new Rectangle(k, y, ic, h);
                y += h + satir;
            }
            else if (o.Eylem != null) { o.Alan = new Rectangle(k, y, ic, D(Tema.InputHeight)); y = o.Alan.Bottom + satir + ek; }
            else if (o.Secenek != null) { o.Alan = new Rectangle(k, y + baslik + D(Tema.FieldGap), ic, D(Tema.TargetMin + Tema.Space1)); y = o.Alan.Bottom + satir + ek; }
            else { o.Alan = new Rectangle(k, y + baslik + D(Tema.FieldGap), ic, D(Tema.TargetMin)); y = o.Alan.Bottom + satir; }
        }
        int yuk = y - satir + k;
        if (ClientSize.Width != gen || ClientSize.Height != yuk)
        {
            int fark = yuk - ClientSize.Height;
            ClientSize = new Size(gen, yuk);
            using (System.Drawing.Drawing2D.GraphicsPath p = Tema.Yuvarlak(new RectangleF(0, 0, gen, yuk), D(Tema.WindowRadius))) Region = new Region(p);
            if (Visible) { hedefY -= fark; Konumla(); }
        }
        Invalidate();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams c = base.CreateParams;
            c.ExStyle |= 0x80;
            c.ClassStyle |= 0x20000;
            return c;
        }
    }

    public void Ac() { Ac(false); }

    public void Ac(bool zorla)
    {
        if (!zorla && (DateTime.Now - kapanis).TotalMilliseconds < 300) return;
        if (Visible && !kapaniyor) { Activate(); return; }
        Rectangle alan = Screen.FromPoint(Cursor.Position).WorkingArea;
        int bosluk = D(Tema.Space2);
        hedefX = Math.Min(Math.Max(alan.Left + bosluk, Cursor.Position.X - Width / 2), alan.Right - Width - bosluk);
        hedefY = alan.Bottom - Height - bosluk;
        Oku();
        if (Acildi != null) Acildi();
        odak = null; klavye = false; kapaniyor = false; surukle = null; basili = null;
        acilis.Ata(0);
        acilis.Git(1, Tema.Slow, Tema.Cik);
        Konumla();
        Show();
        Activate();
        kare.Start();
    }

    void Konumla()
    {
        float v = acilis.Deger;
        double op = Math.Max(0, Math.Min(1, v));
        if (Opacity != op) Opacity = op;
        Location = new Point(hedefX, hedefY + (int)Math.Round((1 - v) * D(Tema.EntryOffset)));
    }

    public void Kapan()
    {
        if (!Visible || kapaniyor) return;
        kapaniyor = true;
        kapanis = DateTime.Now;
        surukle = null; basili = null;
        acilis.Git(0, Tema.Base, Tema.Gir);
        if (!acilis.Suruyor) Bitir();
    }

    void Bitir()
    {
        kare.Stop();
        kapaniyor = false;
        Hide();
        acilis.Ata(1);
    }

    void Kare()
    {
        sayac++;
        if (kapaniyor && !acilis.Suruyor) { Bitir(); return; }
        if (acilis.Suruyor || kapaniyor || Opacity < 1) Konumla();
        bool oku = sayac % 12 == 0;
        if (oku && surukle != win) Oku();
        if (Esitle() || oku) Invalidate();
    }

    bool Esitle()
    {
        bool s = false;
        foreach (Oge o in ogeler)
        {
            if (o.Secenek != null)
            {
                if (o.Secili != o.Kay.Hedef) { if (o.Kay.Hedef < 0 || o.Secili < 0 || !Visible) o.Kay.Ata(o.Secili); else o.Kay.Git(o.Secili, Tema.Base, Tema.GirCik); }
            }
            else if (o.Ipucu == null && o.Eylem == null && o.Deger != o.Dolu.Hedef)
            {
                if (o.Dolu.Hedef < 0 || o.Deger < 0 || o == surukle || !Visible) o.Dolu.Ata(o.Deger); else o.Dolu.Git(o.Deger, Tema.Base, Tema.GirCik);
            }
            s |= o.Dolu.Suruyor || o.Kay.Suruyor || o.Uzerinde.Suruyor || o.Boy.Suruyor;
        }
        return s;
    }

    protected override void OnDeactivate(EventArgs e) { base.OnDeactivate(e); Kapan(); }

    void Oku() { try { win.Deger = Ses.Seviye(); } catch { win.Deger = -1; } }

    public void PilAyarla(int yuzde, string hal)
    {
        bool sarj = hal == "şarjda";
        pil.Deger = yuzde < 0 ? -1 : yuzde / 100f;
        pil.Renk = yuzde < 0 ? Tema.Renk1 : Tema.PilDolgu(yuzde, sarj);
        pil.YaziRenk = yuzde < 0 ? Tema.TextBody : Tema.PilYazi(yuzde, sarj);
        pilHal = hal;
        Yerlestir();
    }

    public int Dugme { set { kulV = value; if (surukle != kul) kul.Deger = value < 0 ? -1 : (56 - Math.Min(value, 56)) / 56f; if (Visible) Invalidate(); } }

    public void Ayarlar(int ancMod, int seffafSeviye, int mikSeviye)
    {
        if (ancMod >= 0 && ancMod <= 2 && ancMod != anc.Secili) { anc.Secili = ancMod; Yerlestir(); }
        if (surukle != seffaf && seffafSeviye >= 1 && seffafSeviye <= 10) seffaf.Deger = seffafSeviye / 10f;
        if (surukle != mik && mikSeviye >= 1 && mikSeviye <= 10) mik.Deger = mikSeviye / 10f;
        if (Visible) Invalidate();
    }

    void Doldur(Graphics g, RectangleF r, Color renk)
    {
        using (System.Drawing.Drawing2D.GraphicsPath p = Tema.Yuvarlak(r, D(Tema.Radius)))
        using (SolidBrush b = new SolidBrush(renk))
            g.FillPath(b, p);
    }

    void Cerceve(Graphics g, RectangleF r, Color renk, float kalinlik, float yaricap)
    {
        r.Inflate(-kalinlik / 2, -kalinlik / 2);
        using (System.Drawing.Drawing2D.GraphicsPath p = Tema.Yuvarlak(r, yaricap))
        using (Pen pen = new Pen(renk, kalinlik))
            g.DrawPath(pen, p);
    }

    void Yaz(Graphics g, string s, Font f, Color c, RectangleF r, StringAlignment yatay, StringAlignment dikey, bool sar)
    {
        using (StringFormat sf = Bicim(yatay, dikey, sar))
        using (SolidBrush b = new SolidBrush(c))
            g.DrawString(s, f, b, r, sf);
    }

    RectangleF CubukAlan(Oge o)
    {
        int h = D(Tema.Space3);
        return new RectangleF(o.Alan.X, o.Alan.Y + (o.Alan.Height - h) / 2, o.Alan.Width, h);
    }

    void CubukCiz(Graphics g, Oge o)
    {
        RectangleF r = CubukAlan(o);
        Doldur(g, r, Tema.Karistir(Tema.Renk1Yuzde20, Tema.Renk1Yuzde30, o == uzerinde ? o.Uzerinde.Deger : 0));
        Cerceve(g, r, Tema.BorderDefault, D(Tema.BorderWidth), D(Tema.Radius));
        float v = o.Dolu.Deger;
        if (v <= 0) return;
        float w = Math.Max(r.Height, r.Width * Math.Min(1f, v));
        Doldur(g, new RectangleF(r.X, r.Y, w, r.Height), o.Renk);
    }

    RectangleF Hucre(Oge o, float i)
    {
        int n = o.Secenek.Length;
        float ara = D(Tema.Space1), w = (o.Alan.Width - ara * (n - 1)) / n;
        return new RectangleF(o.Alan.X + i * (w + ara), o.Alan.Y, w, o.Alan.Height);
    }

    void SecimCiz(Graphics g, Oge o)
    {
        int n = o.Secenek.Length;
        for (int i = 0; i < n; i++)
        {
            RectangleF r = Hucre(o, i);
            float u = o == uzerinde && o.UzerindeSec == i ? o.Uzerinde.Deger : 0;
            Doldur(g, r, Tema.Karistir(Tema.Renk1Yuzde20, Tema.Renk1Yuzde30, u));
            Cerceve(g, r, Tema.BorderDefault, D(Tema.BorderWidth), D(Tema.Radius));
        }
        float s = o.Kay.Deger;
        if (s >= 0) Doldur(g, Hucre(o, s), Tema.Renk1);
        for (int i = 0; i < n; i++)
            Yaz(g, o.Secenek[i], fEtiket, s >= 0 && Math.Abs(s - i) < 0.5f ? Tema.Surface : Tema.TextBody, Hucre(o, i), StringAlignment.Center, StringAlignment.Center, false);
    }

    void DugmeCiz(Graphics g, Oge o)
    {
        RectangleF r = o.Alan;
        float b = o.Boy.Deger;
        r.Inflate(r.Width * (b - 1) / 2, r.Height * (b - 1) / 2);
        if (pilHal == "kapalı")
        {
            Doldur(g, r, Tema.Renk1Yuzde20);
            Cerceve(g, r, Tema.BorderDefault, D(Tema.BorderWidth), D(Tema.Radius));
            Yaz(g, o.Ad, fDugme, Tema.TextBody, r, StringAlignment.Center, StringAlignment.Center, false);
            return;
        }
        Doldur(g, r, Tema.Renk1);
        Yaz(g, o.Ad, fDugme, Tema.Surface, r, StringAlignment.Center, StringAlignment.Center, false);
    }

    void OdakCiz(Graphics g, Oge o)
    {
        RectangleF r = o.Eylem != null || o.Secenek != null ? (RectangleF)o.Alan : CubukAlan(o);
        float b = Math.Max(1f, o.Boy.Deger);
        r.Inflate(r.Width * (b - 1) / 2, r.Height * (b - 1) / 2);
        float w = D(Tema.FocusWidth), a = D(Tema.FocusOffset);
        r.Inflate(a + w, a + w);
        Cerceve(g, r, Tema.FocusRing, w, D(Tema.Radius) + a + w);
    }

    string Etiket(Oge o)
    {
        if (o != pil) return o.Ad;
        if (pilHal == "şarjda") return "Pil · şarjda";
        if (pil.Deger >= 0 && pil.Deger < 0.25f) return "Pil · düşük";
        return "Pil";
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Esitle();
        Graphics g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.Clear(Tema.Surface);
        int k = D(Tema.PanelPadding), gen = ClientSize.Width;
        Cerceve(g, new RectangleF(0, 0, gen, ClientSize.Height), Tema.BorderDefault, D(Tema.BorderWidth), D(Tema.WindowRadius));
        if (ayrac >= 0) using (Pen p = new Pen(Tema.BorderDecorative, D(Tema.BorderWidth))) g.DrawLine(p, k, ayrac, gen - k, ayrac);
        foreach (Oge o in ogeler)
        {
            if (o.Gizli) continue;
            if (o.Ipucu != null) { Yaz(g, o.Ipucu, fGovde, Tema.TextBody, o.Alan, StringAlignment.Near, StringAlignment.Near, true); continue; }
            if (o.Eylem != null) DugmeCiz(g, o);
            else
            {
                RectangleF bas = new RectangleF(k, o.Y, gen - 2 * k, baslik);
                Yaz(g, Etiket(o), fEtiket, Tema.TextLabel, bas, StringAlignment.Near, StringAlignment.Center, false);
                if (o.Secenek != null) SecimCiz(g, o);
                else
                {
                    Yaz(g, o.Deger < 0 ? "—" : o.Bicim(o.Deger), fDeger, o.YaziRenk, bas, StringAlignment.Far, StringAlignment.Center, false);
                    CubukCiz(g, o);
                }
            }
            if (klavye && o == odak) OdakCiz(g, o);
        }
    }

    Oge Bul(Point p)
    {
        foreach (Oge o in ogeler) if (o.Odaklanir && o.Alan.Contains(p)) return o;
        return null;
    }

    int HucreBul(Oge o, int x)
    {
        int n = o.Secenek.Length;
        return Math.Max(0, Math.Min(n - 1, (x - o.Alan.X) * n / o.Alan.Width));
    }

    void UzerindeAyarla(Oge o, int x)
    {
        int h = o != null && o.Secenek != null ? HucreBul(o, x) : -1;
        if (o == uzerinde && (o == null || h == o.UzerindeSec)) return;
        if (uzerinde != null) { uzerinde.Uzerinde.Ata(0); uzerinde.UzerindeSec = -1; }
        uzerinde = o;
        if (o != null) { o.UzerindeSec = h; o.Uzerinde.Ata(0); o.Uzerinde.Git(1, Tema.Fast, Tema.Cik); }
        foreach (Oge d in ogeler) if (d.Eylem != null) BoyAyarla(d);
        Invalidate();
    }

    void BoyAyarla(Oge o) { o.Boy.Git(o == basili ? Tema.ScalePress : o == uzerinde ? Tema.ScaleHover : 1f, Tema.Fast, Tema.Cik); }

    void Degisti(Oge o) { if (o.Kalici) { kaydetSaat.Stop(); kaydetSaat.Start(); } Invalidate(); }

    void Surukle(Oge o, int x)
    {
        o.Ayar(Math.Max(0f, Math.Min(1f, (x - o.Alan.X) / (float)o.Alan.Width)));
        Degisti(o);
    }

    void SecimYap(Oge o, int i)
    {
        i = Math.Max(0, Math.Min(o.Secenek.Length - 1, i));
        if (i == o.Secili) return;
        o.Secili = i;
        o.Sec(i);
        Degisti(o);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        klavye = false;
        Oge o = Bul(e.Location);
        if (o == null) { Invalidate(); return; }
        odak = o;
        if (o.Eylem != null) { basili = o; BoyAyarla(o); Invalidate(); return; }
        if (o.Secenek != null) { SecimYap(o, HucreBul(o, e.X)); return; }
        surukle = o;
        Surukle(o, e.X);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        Oge o = Bul(e.Location);
        UzerindeAyarla(o, e.X);
        Cursor = o != null || surukle != null ? Cursors.Hand : Cursors.Default;
        if (surukle != null) Surukle(surukle, e.X);
    }

    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); UzerindeAyarla(null, 0); }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        surukle = null;
        Oge b = basili;
        if (b == null) return;
        basili = null;
        BoyAyarla(b);
        Invalidate();
        if (Bul(e.Location) == b) b.Eylem();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        Oge o = Bul(e.Location);
        if (o == null || o.Eylem != null) return;
        int yon = e.Delta > 0 ? 1 : -1;
        if (o.Secenek != null) { SecimYap(o, (o.Secili < 0 ? 0 : o.Secili) + yon); return; }
        if (o.Deger < 0) return;
        o.Ayar(Math.Max(0f, Math.Min(1f, o.Deger + yon * o.Adim)));
        Degisti(o);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys tuslar)
    {
        Keys t = tuslar & Keys.KeyCode;
        bool shift = (tuslar & Keys.Shift) != 0;
        if (t == Keys.Escape) { Kapan(); return true; }
        List<Oge> l = ogeler.Where(o => o.Odaklanir).ToList();
        if (l.Count == 0) return base.ProcessCmdKey(ref msg, tuslar);
        if (t == Keys.Tab || t == Keys.Up || t == Keys.Down)
        {
            bool geri = t == Keys.Up || (t == Keys.Tab && shift);
            int i = l.IndexOf(odak);
            i = i < 0 || !klavye ? (i < 0 ? (geri ? l.Count - 1 : 0) : i) : (i + (geri ? -1 : 1) + l.Count) % l.Count;
            odak = l[i];
            klavye = true;
            Invalidate();
            return true;
        }
        if (odak == null || !odak.Odaklanir) return base.ProcessCmdKey(ref msg, tuslar);
        Oge o2 = odak;
        if (o2.Eylem != null)
        {
            if (t != Keys.Enter && t != Keys.Space) return base.ProcessCmdKey(ref msg, tuslar);
            klavye = true;
            o2.Eylem();
            return true;
        }
        if (t != Keys.Left && t != Keys.Right && t != Keys.Home && t != Keys.End) return base.ProcessCmdKey(ref msg, tuslar);
        klavye = true;
        if (o2.Secenek != null)
        {
            int c = o2.Secili < 0 ? 0 : o2.Secili;
            SecimYap(o2, t == Keys.Left ? c - 1 : t == Keys.Right ? c + 1 : t == Keys.Home ? 0 : o2.Secenek.Length - 1);
            Invalidate();
            return true;
        }
        if (o2.Deger >= 0)
        {
            float v = t == Keys.Left ? o2.Deger - o2.Adim : t == Keys.Right ? o2.Deger + o2.Adim : t == Keys.Home ? 0f : 1f;
            o2.Ayar(Math.Max(0f, Math.Min(1f, v)));
            Degisti(o2);
        }
        Invalidate();
        return true;
    }
}
