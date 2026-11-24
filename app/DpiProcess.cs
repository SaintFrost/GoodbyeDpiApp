using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GoodbyeDpiApp;

/// <summary>
/// Runs goodbyedpi.exe as a hidden child process (what turkey_dnsredir.cmd does in a console window).
/// The child is placed in a kill-on-close job, so it cannot outlive this app even after a crash.
/// </summary>
sealed class DpiProcess
{
    readonly IntPtr _job = CreateKillOnCloseJob();
    Process? _process;

    public event Action<string>? Output;
    public event Action<int>? Exited;

    public bool IsRunning => _process is { HasExited: false };

    public bool Owns(int pid) => _process is { HasExited: false } p && p.Id == pid;

    public void Start(string exe, string args)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo(exe, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(exe),
            },
            EnableRaisingEvents = true,
        };
        process.OutputDataReceived += (_, e) => { if (e.Data is { Length: > 0 } line) Output?.Invoke(line); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is { Length: > 0 } line) Output?.Invoke(line); };
        process.Exited += (_, _) =>
        {
            process.WaitForExit(); // drains the redirected output first
            Exited?.Invoke(process.ExitCode);
        };

        process.Start();
        AssignProcessToJobObject(_job, process.Handle);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        _process = process;
    }

    public void Stop()
    {
        if (_process is not { HasExited: false } process)
            return;
        process.Kill();
        process.WaitForExit(3000);
    }

    static IntPtr CreateKillOnCloseJob()
    {
        IntPtr job = CreateJobObject(IntPtr.Zero, null);
        var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
        info.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;
        SetInformationJobObject(job, JobObjectExtendedLimitInformation, ref info,
            Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>());
        return job;
    }

    const int JobObjectExtendedLimitInformation = 9;
    const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr CreateJobObject(IntPtr attributes, string? name);

    [DllImport("kernel32.dll")]
    static extern bool SetInformationJobObject(IntPtr job, int infoClass, ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION info, int length);

    [DllImport("kernel32.dll")]
    static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

    [StructLayout(LayoutKind.Sequential)]
    struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct IO_COUNTERS
    {
        public ulong ReadOperationCount, WriteOperationCount, OtherOperationCount;
        public ulong ReadTransferCount, WriteTransferCount, OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }
}
