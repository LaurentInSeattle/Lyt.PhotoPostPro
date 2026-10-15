namespace Lyt.PhotoPostPro.Implementations.Windows.Architecture;

public static class ArchitectureChecker
{
    public const ushort IMAGE_FILE_MACHINE_UNKNOWN = 0 ;
    public const ushort IMAGE_FILE_MACHINE_ARM64 = 0xAA64;

#pragma warning disable CA1401 // P/Invokes should not be visible
#pragma warning disable SYSLIB1054 // Use Library Import 

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWow64Process2(
            IntPtr hProcess,
            out ushort pProcessMachine,
            out ushort pNativeMachine
        );

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

#pragma warning restore CA1401 // P/Invokes should not be visible
#pragma warning restore SYSLIB1054 // Use Library Import 

    public static void Notify (string text, string caption)
        => _ = MessageBox(IntPtr.Zero, text, caption, 0x10);

    public static bool Check()
    {
        if (!Is64BitProcess())
        {
            Notify("This application requires a 64-bit CPU processor.", "Unsupported Architecture");
            return false;
        }

        if (!Is64BitOperatingSystem())
        {
            Notify("This application requires a 64-bit operating system.", "Unsupported Operating System");
            return false;
        }

        if (IsX64EmulatedOnArm64())
        {
            Notify("This application does not support running on ARM64 emulation.", "Unsupported Architecture");
            return false;
        }

        return true;
    }

    public static bool Is64BitProcess() => Environment.Is64BitProcess;

    public static bool Is64BitOperatingSystem() => Environment.Is64BitOperatingSystem;

    private static bool IsWow64Process() => !Environment.Is64BitProcess && Environment.Is64BitOperatingSystem;

    public static bool IsX64EmulatedOnArm64()
    {
        try
        {
            // Get the current process handle
            IntPtr processHandle = Process.GetCurrentProcess().Handle;
            if (IsWow64Process2(processHandle, out ushort processMachine, out ushort nativeMachine))
            {
                // When an x64 app runs under ARM64 emulation:
                // - processMachine is UNKNOWN (0) because it's not a standard WOW64 guest
                // - nativeMachine is ARM64 (0xAA64) representing the host hardware
                return (processMachine == IMAGE_FILE_MACHINE_UNKNOWN &&
                        nativeMachine == IMAGE_FILE_MACHINE_ARM64);
            }
        }
        catch
        {
            // Fallback for environment context if API fails or isn't supported on old platforms
        }

        return false;
    }
}
