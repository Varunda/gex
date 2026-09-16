using System;
using System.Runtime.InteropServices;

// This class is only required for .NET Framework builds.
#if NETFRAMEWORK

namespace SevenZip.Interop;

static class NativeLibrary
{
    [DllImport("kernel32.dll", EntryPoint = "LoadLibrary")]
    public static extern IntPtr Load(string path);
    
    [DllImport("kernel32.dll", EntryPoint = "GetProcAddress")]
    public static extern IntPtr GetExport(IntPtr module, string name);
}

#endif
