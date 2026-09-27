using System;
using System.Runtime.InteropServices;
using Plunderspell.Core;
using UnityEngine;

namespace Plunderspell.Voice
{
    /// <summary>
    /// Chooses which microphone to listen on. Unity's "default" (a null device name) is simply the
    /// first device it lists, which on a machine with virtual audio software is a silent virtual
    /// input — so voice casting heard nothing. In order of preference:
    ///   1. the device the player picked in Settings, if it is still plugged in;
    ///   2. Windows' own default recording device, unless it is a virtual one;
    ///   3. the first device that does not look virtual;
    ///   4. whatever is first.
    /// </summary>
    public static class MicrophonePicker
    {
        private static readonly string[] s_virtualHints = { "virtual", "stereo mix", "loopback", "cable output", "voicemeeter" };

        /// <summary>The device to listen on, or null if there is no microphone at all.</summary>
        public static string Resolve()
        {
            string[] devices = Microphone.devices;
            if (devices == null || devices.Length == 0)
                return null;

            string chosen = PlayerPrefs.GetString(AudioInputSettings.MicrophoneKey, string.Empty);
            if (!string.IsNullOrEmpty(chosen) && Array.IndexOf(devices, chosen) >= 0)
                return chosen;

            return Automatic(devices);
        }

        /// <summary>What "Automatic" resolves to on this machine right now.</summary>
        public static string Automatic(string[] devices)
        {
            if (devices == null || devices.Length == 0)
                return null;

            // A VR streaming app (Virtual Desktop) makes itself the Windows default and is silent
            // when the headset is off, so a virtual default is skipped like any other virtual input.
            string windowsDefault = WindowsDefaultCaptureDeviceName();
            if (!string.IsNullOrEmpty(windowsDefault) && !LooksVirtual(windowsDefault))
            {
                // Unity lists the Windows friendly name, e.g. "Headset (UGREEN Studio Pro)".
                foreach (string device in devices)
                {
                    if (string.Equals(device, windowsDefault, StringComparison.OrdinalIgnoreCase))
                        return device;
                }
            }

            foreach (string device in devices)
            {
                if (!LooksVirtual(device))
                    return device;
            }

            return devices[0];
        }

        public static bool LooksVirtual(string device)
        {
            string lower = device.ToLowerInvariant();
            foreach (string hint in s_virtualHints)
            {
                if (lower.Contains(hint))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Windows' default recording ("capture", console role) device's friendly name, via the Core
        /// Audio COM API. Null anywhere else, or if the query fails for any reason — the caller falls
        /// back to a name heuristic, so a failure here costs a guess, never the mic.
        /// </summary>
        public static string WindowsDefaultCaptureDeviceName()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
                const int eCapture = 1;
                const int eConsole = 0;
                if (enumerator.GetDefaultAudioEndpoint(eCapture, eConsole, out IMMDevice device) != 0 || device == null)
                    return null;

                const int STGM_READ = 0;
                if (device.OpenPropertyStore(STGM_READ, out IPropertyStore store) != 0 || store == null)
                    return null;

                var friendlyName = new PropertyKey
                {
                    fmtid = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"),
                    pid = 14,
                };
                if (store.GetValue(ref friendlyName, out PropVariant value) != 0)
                    return null;

                const short VT_LPWSTR = 31;
                string name = value.vt == VT_LPWSTR ? Marshal.PtrToStringUni(value.pointerValue) : null;
                PropVariantClear(ref value);
                return name;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Mic] Could not ask Windows for its default microphone: {e.Message}");
                return null;
            }
#else
            return null;
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumeratorComObject
        {
        }

        [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            [PreserveSig] int EnumAudioEndpoints(int dataFlow, int stateMask, out IntPtr devices);
            [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice endpoint);
        }

        [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            [PreserveSig] int Activate(ref Guid iid, int clsCtx, IntPtr activationParams, out IntPtr instance);
            [PreserveSig] int OpenPropertyStore(int access, out IPropertyStore properties);
        }

        [ComImport, Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyStore
        {
            [PreserveSig] int GetCount(out int count);
            [PreserveSig] int GetAt(int index, out PropertyKey key);
            [PreserveSig] int GetValue(ref PropertyKey key, out PropVariant value);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PropertyKey
        {
            public Guid fmtid;
            public int pid;
        }

        // PROPVARIANT is 24 bytes on 64-bit Windows; the native side writes all of it.
        [StructLayout(LayoutKind.Explicit, Size = 24)]
        private struct PropVariant
        {
            [FieldOffset(0)] public short vt;
            [FieldOffset(8)] public IntPtr pointerValue;
            [FieldOffset(16)] public IntPtr reserved;
        }

        [DllImport("ole32.dll")]
        private static extern int PropVariantClear(ref PropVariant value);
#endif
    }
}
