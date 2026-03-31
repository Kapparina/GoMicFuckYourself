using System.Runtime.InteropServices;

namespace GoMicFuckYourself.Agent.Audio;

public sealed class PolicyConfigInterop : IPolicyConfigInterop
{
    public void SetDefaultEndpoint(string deviceId, AudioPolicyRole role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);

        var policyConfig = (IPolicyConfig)Activator.CreateInstance(typeof(PolicyConfigClient))!;
        try
        {
            var result = policyConfig.SetDefaultEndpoint(deviceId, (ERole)role);
            Marshal.ThrowExceptionForHR(result);
        }
        finally
        {
            Marshal.ReleaseComObject(policyConfig);
        }
    }

    [ComImport]
    [Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
    private sealed class PolicyConfigClient;

    [ComImport]
    [Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPolicyConfig
    {
        [PreserveSig]
        int GetMixFormat([MarshalAs(UnmanagedType.LPWStr)] string deviceId, out IntPtr format);

        [PreserveSig]
        int GetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string deviceId,
            [MarshalAs(UnmanagedType.Bool)] bool defaultFormat, out IntPtr format);

        [PreserveSig]
        int ResetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string deviceId);

        [PreserveSig]
        int SetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string deviceId, IntPtr endpointFormat, IntPtr mixFormat);

        [PreserveSig]
        int GetProcessingPeriod([MarshalAs(UnmanagedType.LPWStr)] string deviceId,
            [MarshalAs(UnmanagedType.Bool)] bool defaultPeriod, out long defaultPeriodInHns,
            out long minimumPeriodInHns);

        [PreserveSig]
        int SetProcessingPeriod([MarshalAs(UnmanagedType.LPWStr)] string deviceId, ref long periodInHns);

        [PreserveSig]
        int GetShareMode([MarshalAs(UnmanagedType.LPWStr)] string deviceId, out IntPtr mode);

        [PreserveSig]
        int SetShareMode([MarshalAs(UnmanagedType.LPWStr)] string deviceId, IntPtr mode);

        [PreserveSig]
        int GetPropertyValue([MarshalAs(UnmanagedType.LPWStr)] string deviceId, ref PropertyKey propertyKey,
            out PropVariant value);

        [PreserveSig]
        int SetPropertyValue([MarshalAs(UnmanagedType.LPWStr)] string deviceId, ref PropertyKey propertyKey,
            ref PropVariant value);

        [PreserveSig]
        int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string deviceId, ERole role);

        [PreserveSig]
        int SetEndpointVisibility([MarshalAs(UnmanagedType.LPWStr)] string deviceId,
            [MarshalAs(UnmanagedType.Bool)] bool isVisible);
    }

    private enum ERole
    {
        Console = 0,
        Multimedia = 1,
        Communications = 2
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PropertyKey
    {
        public Guid FormatId;
        public int PropertyId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PropVariant
    {
        public ushort VariantType;
        public ushort Reserved1;
        public ushort Reserved2;
        public ushort Reserved3;
        public IntPtr Data;
        public int DataHigh;
    }
}