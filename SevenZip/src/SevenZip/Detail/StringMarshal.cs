using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SevenZip.Detail;

/// <summary>
/// String related functions to convert native strings to managed strings.
/// 
/// <para>
///     The pointer passed to any function expecting a binary string usually
///     points to the first character, but the number of characters is stored
///     in the 4 bytes before the given address.
///     
///     So the layout of a binary string is
///     <code>
///         4 BYTE - N (Character Count)
///         N BYTE - Characters
///     </code>
/// </para>
/// 
/// <para>
///     The per character width is 16 bit when using MSVC, while other compilers and linux
///     use 32 bit. Currently, this implementation always assumes 16 bit on Windows and 32
///     bit on Linux.
/// </para>
/// 
/// <para>
///     On Linux, it looks like it is required to explicitly zero-terminate the native
///     string with 4 zero-bytes. If not doing so, 'garbage' may be appended to the
///     string and filenames for example will be incorrect.
/// </para>
/// 
/// <para>
///     https://learn.microsoft.com/en-us/previous-versions/windows/desktop/automat/bstr
/// </para>
/// </summary>
internal static class StringMarshal
{
    /// <summary>
    /// Creates a managed string from the given unmanaged string located in unmanaged memory.
    /// </summary>
    /// <param name="ptr">
    /// The pointer to the first character of the binary string. The length of the string,
    /// in bytes, must be stored as 32-bit integer just before this pointer (e.g. <c>((uint*)ptr)[-1]</c>).
    /// </param>
    /// <returns>
    /// The resulting managed string or <c>null</c>, if <paramref name="ptr"/> is
    /// <see cref="IntPtr.Zero"/>.
    /// </returns>
    public static unsafe string BinaryStringToManagedString(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
        {
            return null;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Marshal.PtrToStringBSTR(ptr);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var length = Unsafe.Subtract(
                ref Unsafe.AsRef<uint>(ptr.ToPointer()), 
                1);
            var result = Encoding.UTF32.GetString((byte *)ptr, (int)length);

            return result;
        }

        return string.Empty;
    }

    /// <summary>
    /// Creates an unmanaged binary string from a managed string. The pointer returned
    /// by this method must be freed by the caller to avoid memory leaks.
    /// </summary>
    /// <param name="value">
    /// The managed string to transform.
    /// </param>
    /// <returns>
    /// A pointer to the first character of the string. The length is stored
    /// in the 4 bytes before the address returned by this method.
    /// 
    /// If <paramref name="value"/> is empty or <c>null</c>, <see cref="IntPtr.Zero"/>
    /// is being returned.
    /// </returns>
    public static unsafe IntPtr ManagedStringToBinaryString(string value)
    {
        if (value == null)
        {
            return IntPtr.Zero;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Marshal.StringToBSTR(value);
        }

#if NET6_0_OR_GREATER
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var length = Encoding.UTF32.GetByteCount(value);
            var nativeMemoryLength = 4 + length + 4;
            var nativeMemory = Marshal.AllocHGlobal(nativeMemoryLength);
            var buffer = new Span<byte>(nativeMemory.ToPointer(), nativeMemoryLength);

            Unsafe.Write(nativeMemory.ToPointer(), length);
            Unsafe.Write(IntPtr.Add(nativeMemory, 4 + length).ToPointer(), 0U);

            Encoding.UTF32.GetBytes(value, buffer[4..]);

            return IntPtr.Add(nativeMemory, 4);
        }
#endif

        return IntPtr.Zero;
    }

    /// <summary>
    /// Frees the native memory.
    /// </summary>
    /// <param name="ptr">The pointer to the unmanaged binary string.</param>
    public static void BinaryStringFree(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
        {
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Marshal.FreeBSTR(ptr);
        }
        else
        {
            Marshal.FreeHGlobal(IntPtr.Subtract(ptr, 4));
        }
    }
}