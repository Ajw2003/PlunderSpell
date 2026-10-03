using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Plunderspell.Audio
{
    /// <summary>One Windows playback device: its Core Audio endpoint id and its friendly name.</summary>
    public readonly struct OutputDevice
    {
        public readonly string Id;
        public readonly string Name;

        public OutputDevice(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    /// <summary>
    /// Chooses which playback device the game plays through. Unity has no output-device API and always
    /// plays to the Windows default, so this sets Windows' own per-app output (Settings > Sound > Volume
    /// mixer, the route EarTrumpet uses) for this process, then restarts Unity's audio so it reopens on
    /// that device. The per-app interface is undocumented. See docs/4-systems/audio.md, "Output device".
    /// </summary>
    public static class AudioOutputDevices
    {
        /// <summary>PlayerPrefs key holding the chosen device's endpoint id; empty means the Windows default.</summary>
        public const string OutputDeviceKey = "Settings.OutputDevice";

        /// <summary>The saved choice: an endpoint id, or empty for the Windows default.</summary>
        public static string ChosenId => PlayerPrefs.GetString(OutputDeviceKey, string.Empty);

        /// <summary>True where the per-app route can be set at all (Windows player and Editor).</summary>
        public static bool IsSupported
        {
            get
            {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>Every active playback device, in Windows' order. Allocates; call from UI, not per frame.</summary>
        public static List<OutputDevice> List()
        {
            var devices = new List<OutputDevice>();
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
                if (enumerator.EnumAudioEndpoints(RenderFlow, DeviceStateActive, out IMMDeviceCollection collection) != 0 || collection == null)
                    return devices;
                collection.GetCount(out int count);
                for (int i = 0; i < count; i++)
                {
                    if (collection.Item(i, out IMMDevice device) != 0 || device == null)
                        continue;
                    if (device.GetId(out string id) != 0 || string.IsNullOrEmpty(id))
                        continue;
                    devices.Add(new OutputDevice(id, FriendlyName(device) ?? id));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Audio] Could not list playback devices: {e.Message}");
            }
#endif
            return devices;
        }

        /// <summary>
        /// Routes this game to a device (empty id: back to the Windows default), saves the choice, and
        /// restarts Unity's audio so the new route takes effect. Looping sounds that were playing
        /// (music, fires, ambience) carry on from where they were; one-shots in flight are cut.
        /// Returns false, with a warning logged, if Windows refused the route.
        /// </summary>
        public static bool Apply(string deviceId)
        {
            deviceId = deviceId ?? string.Empty;
            if (!SetRoute(deviceId))
                return false;

            PlayerPrefs.SetString(OutputDeviceKey, deviceId);
            PlayerPrefs.Save();
            RestartAudio();
            Debug.Log($"[Audio] Output device: {(deviceId.Length == 0 ? "Windows default" : NameOf(deviceId))}.");
            return true;
        }

        /// <summary>
        /// At startup: if the saved choice and Windows' route for this process disagree (the route was
        /// cleared, or this is a new install path), apply the saved choice. A saved device that is not
        /// plugged in is left alone; Windows plays to the default until it returns.
        /// </summary>
        public static void ApplySaved()
        {
            string chosen = ChosenId;
            if (!IsSupported || chosen.Length == 0 || chosen == CurrentRoute())
                return;
            foreach (OutputDevice device in List())
            {
                if (device.Id == chosen)
                {
                    Apply(chosen);
                    return;
                }
            }
        }

        /// <summary>The friendly name of an endpoint id, or the id itself if it is not plugged in.</summary>
        public static string NameOf(string deviceId)
        {
            foreach (OutputDevice device in List())
            {
                if (device.Id == deviceId)
                    return device.Name;
            }
            return deviceId;
        }

        /// <summary>The endpoint id Windows routes this process to, or empty when it follows the default.</summary>
        public static string CurrentRoute()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            IntPtr factory = ActivatePolicyConfig();
            if (factory == IntPtr.Zero)
                return string.Empty;
            try
            {
                var get = Slot<GetPersistedEndpoint>(factory, GetPersistedSlot);
                if (get(factory, ProcessId, RenderFlow, RoleMultimedia, out IntPtr hstring) != 0)
                    return string.Empty;
                string wrapped = FromHString(hstring);
                WindowsDeleteString(hstring);
                return Unwrap(wrapped);
            }
            finally
            {
                Release(factory);
            }
#else
            return string.Empty;
#endif
        }

        private static void RestartAudio()
        {
            // AudioSettings.Reset stops every source; restart the looping ones where they were.
            AudioSource[] sources = UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var resume = new List<(AudioSource source, int samples)>();
            foreach (AudioSource source in sources)
            {
                if (source.isPlaying && source.loop)
                    resume.Add((source, source.timeSamples));
            }

            AudioSettings.Reset(AudioSettings.GetConfiguration());
            AudioLevels.KeepPushing(); // the restart can drop the mixer's live values

            foreach ((AudioSource source, int samples) in resume)
            {
                if (source == null || source.isPlaying)
                    continue;
                source.Play();
                if (source.clip != null && samples < source.clip.samples)
                    source.timeSamples = samples;
            }
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private const int RenderFlow = 0;
        private const int RoleConsole = 0;
        private const int RoleMultimedia = 1;
        private const int DeviceStateActive = 1;

        // IAudioPolicyConfigFactory: IUnknown (3) + IInspectable (3) + 19 methods before these two.
        private const int SetPersistedSlot = 25;
        private const int GetPersistedSlot = 26;

        private const string PolicyConfigClass = "Windows.Media.Internal.AudioPolicyConfig";
        // Windows 10 21H2 and later, then the older interface id.
        private static readonly Guid[] s_policyConfigIids =
        {
            new Guid("ab3d4648-e242-459f-b02f-541c70306324"),
            new Guid("2a59116d-6c4f-45e0-a74f-707e3fef9258"),
        };

        // The per-app route stores a device interface path, not a bare endpoint id.
        private const string DevicePathPrefix = @"\\?\SWD#MMDEVAPI#";
        private const string DevicePathSuffix = "#{e6327cad-dcec-4949-ae8a-991e976a79d2}";

        private static uint ProcessId => (uint)System.Diagnostics.Process.GetCurrentProcess().Id;

        private static bool SetRoute(string deviceId)
        {
            IntPtr factory = ActivatePolicyConfig();
            if (factory == IntPtr.Zero)
                return false;

            IntPtr hstring = IntPtr.Zero;
            try
            {
                if (deviceId.Length > 0)
                {
                    string path = DevicePathPrefix + deviceId + DevicePathSuffix;
                    if (WindowsCreateString(path, path.Length, out hstring) != 0)
                    {
                        Debug.LogWarning("[Audio] Could not build the device path for the output route.");
                        return false;
                    }
                }

                var set = Slot<SetPersistedEndpoint>(factory, SetPersistedSlot);
                int console = set(factory, ProcessId, RenderFlow, RoleConsole, hstring);
                int multimedia = set(factory, ProcessId, RenderFlow, RoleMultimedia, hstring);
                if (console != 0 || multimedia != 0)
                {
                    Debug.LogWarning($"[Audio] Windows refused the output route (0x{console:X8}, 0x{multimedia:X8}).");
                    return false;
                }
                return true;
            }
            finally
            {
                if (hstring != IntPtr.Zero)
                    WindowsDeleteString(hstring);
                Release(factory);
            }
        }

        private static IntPtr ActivatePolicyConfig()
        {
            if (WindowsCreateString(PolicyConfigClass, PolicyConfigClass.Length, out IntPtr className) != 0)
                return IntPtr.Zero;
            try
            {
                int result = 0;
                foreach (Guid iid in s_policyConfigIids)
                {
                    Guid id = iid;
                    result = RoGetActivationFactory(className, ref id, out IntPtr factory);
                    if (result == 0 && factory != IntPtr.Zero)
                        return factory;
                }
                Debug.LogWarning($"[Audio] Windows' per-app audio route is not available here (0x{result:X8}).");
                return IntPtr.Zero;
            }
            finally
            {
                WindowsDeleteString(className);
            }
        }

        private static T Slot<T>(IntPtr comObject, int slot) where T : Delegate
        {
            IntPtr vtable = Marshal.ReadIntPtr(comObject);
            IntPtr function = Marshal.ReadIntPtr(vtable, slot * IntPtr.Size);
            return Marshal.GetDelegateForFunctionPointer<T>(function);
        }

        private static void Release(IntPtr comObject) => Marshal.Release(comObject);

        private static string FromHString(IntPtr hstring)
        {
            if (hstring == IntPtr.Zero)
                return string.Empty;
            IntPtr buffer = WindowsGetStringRawBuffer(hstring, out uint length);
            return Marshal.PtrToStringUni(buffer, (int)length);
        }

        private static string Unwrap(string path)
        {
            if (!path.StartsWith(DevicePathPrefix, StringComparison.OrdinalIgnoreCase))
                return path;
            string inner = path.Substring(DevicePathPrefix.Length);
            return inner.EndsWith(DevicePathSuffix, StringComparison.OrdinalIgnoreCase)
                ? inner.Substring(0, inner.Length - DevicePathSuffix.Length)
                : inner;
        }

        private static string FriendlyName(IMMDevice device)
        {
            const int StgmRead = 0;
            if (device.OpenPropertyStore(StgmRead, out IPropertyStore store) != 0 || store == null)
                return null;
            var friendlyName = new PropertyKey { fmtid = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), pid = 14 };
            if (store.GetValue(ref friendlyName, out PropVariant value) != 0)
                return null;
            const short VtLpwstr = 31;
            string name = value.vt == VtLpwstr ? Marshal.PtrToStringUni(value.pointerValue) : null;
            PropVariantClear(ref value);
            return name;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int SetPersistedEndpoint(IntPtr self, uint processId, int flow, int role, IntPtr deviceId);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetPersistedEndpoint(IntPtr self, uint processId, int flow, int role, out IntPtr deviceId);

        [DllImport("combase.dll")]
        private static extern int RoGetActivationFactory(IntPtr activatableClassId, ref Guid iid, out IntPtr factory);

        [DllImport("combase.dll", CharSet = CharSet.Unicode)]
        private static extern int WindowsCreateString(string source, int length, out IntPtr hstring);

        [DllImport("combase.dll")]
        private static extern int WindowsDeleteString(IntPtr hstring);

        [DllImport("combase.dll")]
        private static extern IntPtr WindowsGetStringRawBuffer(IntPtr hstring, out uint length);

        [DllImport("ole32.dll")]
        private static extern int PropVariantClear(ref PropVariant value);

        [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumeratorComObject
        {
        }

        [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            [PreserveSig] int EnumAudioEndpoints(int dataFlow, int stateMask, out IMMDeviceCollection devices);
        }

        [ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceCollection
        {
            [PreserveSig] int GetCount(out int count);
            [PreserveSig] int Item(int index, out IMMDevice device);
        }

        [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            [PreserveSig] int Activate(ref Guid iid, int clsCtx, IntPtr activationParams, out IntPtr instance);
            [PreserveSig] int OpenPropertyStore(int access, out IPropertyStore properties);
            [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
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
#else
        private static bool SetRoute(string deviceId)
        {
            Debug.LogWarning("[Audio] Choosing an output device is only possible on Windows.");
            return false;
        }
#endif
    }
}
