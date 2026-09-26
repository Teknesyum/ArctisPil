using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

public class ArctisHid : IDisposable
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr sec, uint disp, uint flags, IntPtr tmpl);
    [DllImport("hid.dll", SetLastError = true)]
    static extern bool HidD_GetPreparsedData(SafeFileHandle h, out IntPtr p);
    [DllImport("hid.dll")]
    static extern bool HidD_FreePreparsedData(IntPtr p);
    [DllImport("hid.dll")]
    static extern int HidP_GetCaps(IntPtr p, ref HIDP_CAPS caps);

    [StructLayout(LayoutKind.Sequential)]
    public struct HIDP_CAPS
    {
        public ushort Usage;
        public ushort UsagePage;
        public ushort InputReportByteLength;
        public ushort OutputReportByteLength;
        public ushort FeatureReportByteLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        public ushort[] Reserved;
        public ushort NumberLinkCollectionNodes;
        public ushort NumberInputButtonCaps;
        public ushort NumberInputValueCaps;
        public ushort NumberInputDataIndices;
        public ushort NumberOutputButtonCaps;
        public ushort NumberOutputValueCaps;
        public ushort NumberOutputDataIndices;
        public ushort NumberFeatureButtonCaps;
        public ushort NumberFeatureValueCaps;
        public ushort NumberFeatureDataIndices;
    }

    public static bool TryGetCaps(string path, out HIDP_CAPS caps)
    {
        caps = new HIDP_CAPS();
        using (SafeFileHandle h = CreateFile(path, 0, 3, IntPtr.Zero, 3, 0, IntPtr.Zero))
        {
            if (h.IsInvalid) return false;
            IntPtr p;
            if (!HidD_GetPreparsedData(h, out p)) return false;
            HidP_GetCaps(p, ref caps);
            HidD_FreePreparsedData(p);
            return true;
        }
    }

    static FileStream Open(string path)
    {
        SafeFileHandle h = CreateFile(path, 0xC0000000, 3, IntPtr.Zero, 3, 0x40000000, IntPtr.Zero);
        if (h.IsInvalid) throw new IOException("CreateFile hata kodu " + Marshal.GetLastWin32Error());
        return new FileStream(h, FileAccess.ReadWrite, 1, true);
    }

    FileStream rd;
    FileStream wr;
    Thread reader;
    volatile bool running;
    readonly int inLen;
    readonly int outLen;
    readonly Stopwatch clock;

    public readonly object Sync = new object();
    public readonly List<string> Log = new List<string>();
    public readonly List<double> QueryTimes = new List<double>();
    public readonly List<double> PushTimes = new List<double>();
    public int Responses;
    public int LastLevel = -1;
    public int LastStatus = -1;
    public string LastError;

    public ArctisHid(string path, int inputLen, int outputLen, Stopwatch sw)
    {
        inLen = inputLen;
        outLen = outputLen;
        clock = sw;
        rd = Open(path);
        wr = Open(path);
        running = true;
        reader = new Thread(ReadLoop);
        reader.IsBackground = true;
        reader.Start();
    }

    void ReadLoop()
    {
        byte[] buf = new byte[inLen];
        while (running)
        {
            int n;
            try { n = rd.Read(buf, 0, buf.Length); }
            catch (Exception e) { if (running) LastError = e.Message; break; }
            if (n <= 0) continue;
            double t = clock.Elapsed.TotalMilliseconds;
            bool resp = n > 15 && buf[0] == 0x06 && buf[1] == 0xb0;
            string hex = BitConverter.ToString(buf, 0, Math.Min(n, 24));
            lock (Sync)
            {
                if (resp) { Responses++; LastLevel = buf[6]; LastStatus = buf[15]; }
                else PushTimes.Add(t);
                Log.Add(string.Format("{0:F0}\t{1}\t{2}", t, resp ? "YANIT" : "OLAY", hex));
            }
        }
    }

    public void Query()
    {
        byte[] o = new byte[outLen];
        o[0] = 0x06;
        o[1] = 0xb0;
        double t = clock.Elapsed.TotalMilliseconds;
        try { wr.Write(o, 0, o.Length); }
        catch (Exception e) { LastError = e.Message; }
        lock (Sync) { QueryTimes.Add(t); Log.Add(string.Format("{0:F0}\tSORGU", t)); }
    }

    public static int Percent(int level)
    {
        if (level < 0) return -1;
        if (level > 8) level = 8;
        return level * 100 / 8;
    }

    public void Dispose()
    {
        running = false;
        try { rd.Dispose(); } catch { }
        try { wr.Dispose(); } catch { }
    }
}
