using System;
using System.Runtime.InteropServices;

namespace Nimbus;

internal class Native
{
    #region Kernel-Module

    internal const string KernelModule = "kernel32.dll";

    [DllImport(KernelModule, SetLastError = true, CharSet = CharSet.Ansi)]
    internal static extern IntPtr LoadLibrary(
        string lpFileName);

    [DllImport(KernelModule, SetLastError = true)]
    internal static extern IntPtr GetProcAddress(
        IntPtr hModule,
        string lpProcName);

    [DllImport(KernelModule, SetLastError = true)]
    public static extern IntPtr GetModuleHandle(
        string lpModuleName);

    [DllImport(KernelModule, SetLastError = true)]
    internal static extern bool FreeLibrary(
        IntPtr hModule);

    #endregion

    internal static InitializeDelegate Initialize;
    internal delegate void InitializeDelegate();

    internal static GetClientsDelegate GetClients;
    internal delegate IntPtr GetClientsDelegate();

    internal static ExecuteAsyncDelegate ExecuteAsync;
    internal delegate void ExecuteAsyncDelegate(
        byte[] Source,
        string[] Clients,
        int Length);

    internal static string ModuleName = "Nimbus.dll";

    internal static T Resolve<T>(string Module, string Function)
    {
        IntPtr hModule = LoadLibrary(Module);

        if (hModule == IntPtr.Zero)
            throw new NimbusException($"Failed To Load Module: {Module}");

        IntPtr hFunction = GetProcAddress(hModule, Function);

        return hFunction == IntPtr.Zero
            ? throw new NimbusException($"Failed To Get Function: {Function}")
            : Marshal.GetDelegateForFunctionPointer<T>(hFunction);
    }

    static Native()
    {
        Initialize = Resolve<InitializeDelegate>(ModuleName, "Initialize");
        GetClients = Resolve<GetClientsDelegate>(ModuleName, "GetClients");
        ExecuteAsync = Resolve<ExecuteAsyncDelegate>(ModuleName, "ExecuteAsync");
    }
}
