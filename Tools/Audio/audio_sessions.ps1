# Lists every Windows playback device, marks the default, and for each device lists the apps
# sending audio to it with their volume, mute and current peak level. Read-only.
# Run: powershell -ExecutionPolicy Bypass -File Tools\Audio\audio_sessions.ps1
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;

[ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] class MMDeviceEnumeratorCom {}
[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMDeviceEnumerator {
  int EnumAudioEndpoints(int dataFlow, int stateMask, out IMMDeviceCollection devices);
  int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice device);
}
[Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMDeviceCollection { int GetCount(out int count); int Item(int index, out IMMDevice device); }
[Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMDevice {
  int Activate(ref Guid iid, int clsCtx, IntPtr p, [MarshalAs(UnmanagedType.IUnknown)] out object o);
  int OpenPropertyStore(int access, out IPropertyStore store);
  int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
}
[Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IPropertyStore { int GetCount(out int c); int GetAt(int i, out PropKey k); int GetValue(ref PropKey k, out PropVariant v); }
[StructLayout(LayoutKind.Sequential)] struct PropKey { public Guid fmtid; public int pid; }
[StructLayout(LayoutKind.Explicit)] struct PropVariant { [FieldOffset(0)] public short vt; [FieldOffset(8)] public IntPtr p; }
[Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IAudioSessionManager2 {
  int GetAudioSessionControl(IntPtr a, int b, out IntPtr c); int GetSimpleAudioVolume(IntPtr a, int b, out IntPtr c);
  int GetSessionEnumerator(out IAudioSessionEnumerator e);
}
[Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IAudioSessionEnumerator { int GetCount(out int c); int GetSession(int i, out IAudioSessionControl2 s); }
[Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c6d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IAudioSessionControl2 {
  int GetState(out int s); int GetDisplayName(out IntPtr n); int SetDisplayName(IntPtr a, IntPtr b);
  int GetIconPath(out IntPtr a); int SetIconPath(IntPtr a, IntPtr b); int GetGroupingParam(out Guid g);
  int SetGroupingParam(IntPtr a, IntPtr b); int RegisterAudioSessionNotification(IntPtr a); int UnregisterAudioSessionNotification(IntPtr a);
  int GetSessionIdentifier(out IntPtr a); int GetSessionInstanceIdentifier(out IntPtr a); int GetProcessId(out uint pid);
}
[Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface ISimpleAudioVolume { int SetMasterVolume(float v, IntPtr c); int GetMasterVolume(out float v); int SetMute(bool m, IntPtr c); int GetMute(out bool m); }
[Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IAudioMeterInformation { int GetPeakValue(out float p); }
[Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IAudioEndpointVolume {
  int a(); int b(); int c(); int SetMasterVolumeLevel(); int SetMasterVolumeLevelScalar(); int GetMasterVolumeLevel(out float f);
  int GetMasterVolumeLevelScalar(out float v); int e(); int f(); int g(); int h(); int SetMute(); int GetMute(out bool m);
}

public static class AudioReport {
  static string Name(IMMDevice d) {
    IPropertyStore s; d.OpenPropertyStore(0, out s);
    var k = new PropKey { fmtid = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), pid = 14 };
    PropVariant v; s.GetValue(ref k, out v);
    return Marshal.PtrToStringUni(v.p);
  }
  public static string Run() {
    var sb = new StringBuilder();
    var en = (IMMDeviceEnumerator)new MMDeviceEnumeratorCom();
    IMMDevice def; en.GetDefaultAudioEndpoint(0, 1, out def); string defId; def.GetId(out defId);
    IMMDeviceCollection col; en.EnumAudioEndpoints(0, 1, out col); int n; col.GetCount(out n);
    for (int i = 0; i < n; i++) {
      IMMDevice d; col.Item(i, out d); string id; d.GetId(out id);
      object o; Guid g = typeof(IAudioEndpointVolume).GUID; d.Activate(ref g, 23, IntPtr.Zero, out o);
      var ev = (IAudioEndpointVolume)o; float vol; bool mute; ev.GetMasterVolumeLevelScalar(out vol); ev.GetMute(out mute);
      sb.AppendLine((id == defId ? "* DEFAULT " : "  ") + Name(d) + "  volume " + vol.ToString("F2") + (mute ? " MUTED" : ""));
      g = typeof(IAudioSessionManager2).GUID; d.Activate(ref g, 23, IntPtr.Zero, out o);
      IAudioSessionEnumerator se; ((IAudioSessionManager2)o).GetSessionEnumerator(out se); int sc; se.GetCount(out sc);
      for (int j = 0; j < sc; j++) {
        IAudioSessionControl2 c; se.GetSession(j, out c); uint pid; c.GetProcessId(out pid); int st; c.GetState(out st);
        string pn = "?"; try { pn = pid == 0 ? "system" : Process.GetProcessById((int)pid).ProcessName; } catch {}
        var sv = (ISimpleAudioVolume)c; float v2; bool m2; sv.GetMasterVolume(out v2); sv.GetMute(out m2);
        float peak; ((IAudioMeterInformation)c).GetPeakValue(out peak);
        sb.AppendLine("      " + pn + " (pid " + pid + ") state " + (st == 1 ? "active" : st == 0 ? "inactive" : "expired") + " volume " + v2.ToString("F2") + (m2 ? " MUTED" : "") + " peak " + peak.ToString("F3"));
      }
    }
    return sb.ToString();
  }
}
"@
[AudioReport]::Run()
