using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

public class HidDinleyici : IDisposable
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr sec, uint disp, uint flags, IntPtr tmpl);

    FileStream rd;
    Thread th;
    volatile bool running;
    readonly int inLen;
    readonly Stopwatch clock;
    readonly string ad;
    readonly List<string> log;
    readonly object sync;
    public int Sayac;
    public string Hata;

    public HidDinleyici(string path, string name, int inputLen, Stopwatch sw, List<string> sharedLog, object sharedSync)
    {
        ad = name; inLen = inputLen; clock = sw; log = sharedLog; sync = sharedSync;
        SafeFileHandle h = CreateFile(path, 0x80000000, 3, IntPtr.Zero, 3, 0x40000000, IntPtr.Zero);
        if (h.IsInvalid) throw new IOException("CreateFile hata kodu " + Marshal.GetLastWin32Error());
        rd = new FileStream(h, FileAccess.Read, 1, true);
        running = true;
        th = new Thread(Dongu);
        th.IsBackground = true;
        th.Start();
    }

    void Dongu()
    {
        byte[] buf = new byte[inLen];
        while (running)
        {
            int n;
            try { n = rd.Read(buf, 0, buf.Length); }
            catch (Exception e) { if (running) Hata = e.Message; break; }
            if (n <= 0) continue;
            double t = clock.Elapsed.TotalMilliseconds;
            string hex = BitConverter.ToString(buf, 0, Math.Min(n, 32));
            lock (sync) { Sayac++; log.Add(string.Format("{0:F0}\tHID\t{1}\t{2}", t, ad, hex)); }
        }
    }

    public void Dispose() { running = false; try { rd.Dispose(); } catch { } }
}

public static class UcNokta
{
    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    class MMDeviceEnumeratorCo { }

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDeviceEnumerator
    {
        int EnumAudioEndpoints(int dataFlow, int stateMask, out IMMDeviceCollection devices);
        int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice device);
    }

    [ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDeviceCollection
    {
        int GetCount(out int count);
        int Item(int index, out IMMDevice device);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDevice
    {
        int Activate(ref Guid iid, int clsCtx, IntPtr p, [MarshalAs(UnmanagedType.IUnknown)] out object o);
        int OpenPropertyStore(int access, out IPropertyStore store);
        int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
    }

    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IPropertyStore
    {
        int GetCount(out int c);
        int GetAt(int i, out PropertyKey k);
        int GetValue(ref PropertyKey k, out PropVariant v);
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PropertyKey { public Guid fmtid; public int pid; }

    [StructLayout(LayoutKind.Sequential)]
    struct PropVariant { public ushort vt; public ushort r1, r2, r3; public IntPtr p; public IntPtr p2; }

    [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAudioEndpointVolume
    {
        int RegisterControlChangeNotify(IntPtr n);
        int UnregisterControlChangeNotify(IntPtr n);
        int GetChannelCount(out uint c);
        int SetMasterVolumeLevel(float db, ref Guid ctx);
        int SetMasterVolumeLevelScalar(float lvl, ref Guid ctx);
        int GetMasterVolumeLevel(out float db);
        int GetMasterVolumeLevelScalar(out float lvl);
        int SetChannelVolumeLevel(uint ch, float db, ref Guid ctx);
        int SetChannelVolumeLevelScalar(uint ch, float lvl, ref Guid ctx);
        int GetChannelVolumeLevel(uint ch, out float db);
        int GetChannelVolumeLevelScalar(uint ch, out float lvl);
        int SetMute(bool m, ref Guid ctx);
        int GetMute(out bool m);
    }

    public class Uc
    {
        public string Ad;
        public string Id;
        public IAudioEndpointVolume Ses;
        public float Son = -1;
        public bool SonSessiz;
        public float Seviye() { float v; Ses.GetMasterVolumeLevelScalar(out v); return v; }
        public bool Sessiz() { bool m; Ses.GetMute(out m); return m; }
        public void Ayarla(float v) { Guid g = Guid.Empty; Ses.SetMasterVolumeLevelScalar(v, ref g); }
    }

    public static List<Uc> Hepsi()
    {
        List<Uc> l = new List<Uc>();
        IMMDeviceEnumerator en = (IMMDeviceEnumerator)new MMDeviceEnumeratorCo();
        IMMDeviceCollection col;
        en.EnumAudioEndpoints(0, 1, out col);
        int n; col.GetCount(out n);
        Guid iid = typeof(IAudioEndpointVolume).GUID;
        PropertyKey pk = new PropertyKey { fmtid = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), pid = 14 };
        for (int i = 0; i < n; i++)
        {
            IMMDevice d; col.Item(i, out d);
            Uc u = new Uc();
            d.GetId(out u.Id);
            IPropertyStore ps; d.OpenPropertyStore(0, out ps);
            PropVariant v; ps.GetValue(ref pk, out v);
            u.Ad = v.vt == 31 ? Marshal.PtrToStringUni(v.p) : u.Id;
            object o; d.Activate(ref iid, 23, IntPtr.Zero, out o);
            u.Ses = (IAudioEndpointVolume)o;
            l.Add(u);
        }
        return l;
    }

    public static string Varsayilan()
    {
        IMMDeviceEnumerator en = (IMMDeviceEnumerator)new MMDeviceEnumeratorCo();
        IMMDevice d; en.GetDefaultAudioEndpoint(0, 1, out d);
        string id; d.GetId(out id);
        return id;
    }
}
