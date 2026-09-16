using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SevenZip.Interop;

/// <summary>
/// This class offers methods to dynamically load a native library and their exported functions.
/// </summary>
internal static class Native
{
    /// <summary>
    /// A delegate declaration for the native <c>CreateObject</c> function used
    /// to create a COM object.
    /// </summary>
    /// <param name="classId">The UUID of the class to create.</param>
    /// <param name="interfaceId">The UUID of the interface to create.</param>
    /// <param name="interfacePtr">The created interface or <c>null</c>, if the creation failed.</param>
    /// <returns>Zero (0), if the operation succeeded or a non-zero value indicating an error otherwise.</returns>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate int CreateObjectDelegate(
        [In] ref Guid classId,
        [In] ref Guid interfaceId,
#if NET8_0_OR_GREATER
        out IntPtr interfacePtr
#else
        [MarshalAs(UnmanagedType.Interface)] out object interfacePtr
#endif
    );

    /// <summary>
    /// Loads a native library. If the first attempt failed, the library is searched
    /// in the directory of the current executable.
    /// </summary>
    /// <param name="name">The name or path of the library to load.</param>
    /// <returns>A pointer to the library handle.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the given library file could
    /// not be found.</exception>
    /// <exception cref="BadImageFormatException">Thrown if the library could not be
    /// loaded.</exception>
    public static IntPtr LoadLibrary(string name)
    {
        try
        {
            return NativeLibrary.Load(name);
        }
        catch (FileNotFoundException)
        {
            // Continue and try to find the library within the application directory.
        }
        catch (DllNotFoundException)
        {
            // Continue and try to find the library within the application directory.
        }

        if (!Path.IsPathRooted(name))
        {
            var directory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? string.Empty;

            if (TryGetLibraryPath(directory, Path.GetFileName(name), out var path))
            {
                return NativeLibrary.Load(path);
            }
        }

        throw new FileNotFoundException($"The native library {name} could not be found.");
    }

    /// <summary>
    /// Loads a function named <paramref name="name"/> from the given <paramref name="library"/>.
    /// </summary>
    /// <typeparam name="TDelegate">The delegate declaring the function signature.</typeparam>
    /// <param name="library">The library containing the function to load.</param>
    /// <param name="name">The name of the function to load.</param>
    /// <returns>A delegate for the loaded native function.</returns>
    public static TDelegate GetExportAs<TDelegate>(IntPtr library, string name)
        where TDelegate : class
    {
        var handle = NativeLibrary.GetExport(library, name);
        var function = Marshal.GetDelegateForFunctionPointer<TDelegate>(handle);

        return function;
    }

    private static bool TryGetLibraryPath(string directory, string filename, out string path)
    {
        path = Path.Combine(directory, filename);

        if (File.Exists(path))
        {
            return true;
        }

        path = Directory
            .EnumerateFiles(directory, filename, SearchOption.AllDirectories)
            .FirstOrDefault();

        if (path != null && File.Exists(path))
        {
            return true;
        }

        path = null;
        return false;
    }
}