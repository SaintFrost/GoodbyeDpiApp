using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace GoodbyeDpiApp;

enum ServiceState { NotInstalled, Stopped, Starting, Stopping, Running, Unknown }

/// <summary>
/// The "GoodbyeDPI" Windows service. Changes go through sc.exe exactly like the release's
/// service_install_*.cmd and service_remove.cmd scripts, so the log shows the same commands.
/// </summary>
static class DpiService
{
    public const string Name = "GoodbyeDPI";

    static readonly Encoding OemEncoding = Encoding.GetEncoding((int)GetOEMCP());

    public static (ServiceState State, int Pid) Query(string name = Name)
    {
        IntPtr scm = OpenSCManager(null, null, SC_MANAGER_CONNECT);
        if (scm == IntPtr.Zero)
            return (ServiceState.Unknown, 0);
        try
        {
            IntPtr service = OpenService(scm, name, SERVICE_QUERY_STATUS);
            if (service == IntPtr.Zero)
                return (Marshal.GetLastWin32Error() == ERROR_SERVICE_DOES_NOT_EXIST ? ServiceState.NotInstalled : ServiceState.Unknown, 0);
            try
            {
                if (!QueryServiceStatusEx(service, SC_STATUS_PROCESS_INFO, out var status, Marshal.SizeOf<SERVICE_STATUS_PROCESS>(), out _))
                    return (ServiceState.Unknown, 0);
                var state = status.CurrentState switch
                {
                    1 => ServiceState.Stopped,
                    2 => ServiceState.Starting,
                    3 => ServiceState.Stopping,
                    4 => ServiceState.Running,
                    _ => ServiceState.Unknown,
                };
                return (state, status.ProcessId);
            }
            finally { CloseServiceHandle(service); }
        }
        finally { CloseServiceHandle(scm); }
    }

    /// <summary>The installed service's goodbyedpi.exe path and arguments, if any.</summary>
    public static (string Exe, string Args)? ReadCommandLine()
    {
        if (Query().State == ServiceState.NotInstalled
            || Registry.GetValue($@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\{Name}", "ImagePath", null) is not string cmd)
            return null;

        cmd = cmd.Trim();
        if (cmd.StartsWith('"'))
        {
            int end = cmd.IndexOf('"', 1);
            if (end > 0)
                return (cmd[1..end], cmd[(end + 1)..].Trim());
        }
        int exe = cmd.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        return exe < 0 ? (cmd, "") : (cmd[..(exe + 4)], cmd[(exe + 4)..].Trim());
    }

    public static async Task InstallAsync(string exe, string args, Action<string> log)
    {
        int code = await Sc(log, "create", Name, "binPath=", $"\"{exe}\" {args}", "start=", "auto");
        if (code == ERROR_SERVICE_MARKED_FOR_DELETE)
            throw new InvalidOperationException(
                "Eski hizmet hâlâ silinmeyi bekliyor. Hizmetler penceresi açıksa kapatıp tekrar deneyin ya da bilgisayarı yeniden başlatın.");
        if (code != 0)
            throw new InvalidOperationException($"Hizmet oluşturulamadı (sc hata kodu {code}).");

        await Sc(log, "description", Name, "Türkiye için DNS zorlamasını kaldırır.");
        code = await Sc(log, "start", Name);
        if (code != 0)
            throw new InvalidOperationException(
                $"Hizmet kuruldu ama başlatılamadı (sc hata kodu {code}). Antivirüs programınız WinDivert'i engelliyor olabilir.");
    }

    /// <param name="removeDriver">Also unload WinDivert, as service_remove.cmd does.</param>
    public static async Task RemoveAsync(Action<string> log, bool removeDriver)
    {
        var (state, _) = Query();
        if (state is ServiceState.Running or ServiceState.Starting)
            await Sc(log, "stop", Name);
        if (state != ServiceState.NotInstalled)
            await Sc(log, "delete", Name);

        if (removeDriver)
            foreach (string driver in new[] { "WinDivert", "WinDivert14" })
            {
                if (Query(driver).State == ServiceState.NotInstalled)
                    continue;
                await Sc(log, "stop", driver);
                await Sc(log, "delete", driver);
            }

        // Deletion completes only after the service process has exited.
        for (int i = 0; i < 50 && Query().State != ServiceState.NotInstalled; i++)
            await Task.Delay(200);
    }

    static async Task<int> Sc(Action<string> log, params string[] args)
    {
        var psi = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "sc.exe"))
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = OemEncoding,
            StandardErrorEncoding = OemEncoding,
        };
        foreach (string arg in args)
            psi.ArgumentList.Add(arg);
        log("> sc " + string.Join(' ', args));

        using var process = Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
            foreach (string line in (await stdout + await stderr).Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                log("  " + line);
        return process.ExitCode;
    }

    const uint SC_MANAGER_CONNECT = 0x0001;
    const uint SERVICE_QUERY_STATUS = 0x0004;
    const int SC_STATUS_PROCESS_INFO = 0;
    const int ERROR_SERVICE_DOES_NOT_EXIST = 1060;
    const int ERROR_SERVICE_MARKED_FOR_DELETE = 1072;

    [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr OpenSCManager(string? machine, string? database, uint access);

    [DllImport("advapi32.dll", EntryPoint = "OpenServiceW", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr OpenService(IntPtr scm, string name, uint access);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool QueryServiceStatusEx(IntPtr service, int level, out SERVICE_STATUS_PROCESS status, int size, out int needed);

    [DllImport("advapi32.dll")]
    static extern bool CloseServiceHandle(IntPtr handle);

    [DllImport("kernel32.dll")]
    static extern uint GetOEMCP();

    [StructLayout(LayoutKind.Sequential)]
    struct SERVICE_STATUS_PROCESS
    {
        public int ServiceType, CurrentState, ControlsAccepted, Win32ExitCode, ServiceSpecificExitCode;
        public int CheckPoint, WaitHint, ProcessId, ServiceFlags;
    }
}
