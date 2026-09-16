using System;
using System.Runtime.InteropServices;
using SevenZip.Interop;

namespace SevenZip.Detail;

/// <summary>
/// Factory class to create COM-like objects from the native 7z library. Supported
/// interfaces are <see cref="IArchiveWriter"/> and <see cref="IArchiveReader"/>,
/// which can both be created for different <see cref="ArchiveFormat"/>s.
/// </summary>
static class ComObjectFactory
{
    /// <summary>
    /// Creates an object supporting the given <typeparamref name="TInterface"/> type and
    /// <paramref name="format"/>.
    /// 
    /// If the native library does not support the given <paramref name="format"/>, an
    /// exception is thrown where the <see cref="Exception.HResult"/> provides details
    /// about the error that occurred.
    /// </summary>
    /// <typeparam name="TInterface">
    /// The interface the object to create has to support. The <see cref="Type.GUID"/>
    /// property is used to obtain the UUID to forward to the <c>CreateObject</c> call.
    /// </typeparam>
    /// <param name="library">
    /// The pointer to the native library.
    /// </param>
    /// <param name="format">
    /// The format the object to create has to support.
    /// </param>
    /// <returns>
    /// The new COM-like object supporting the given interface.
    /// </returns>
    /// <exception cref="Exception">
    /// An exception is thrown when the native call to <c>CreateObject</c> returns an error.
    /// The <see cref="Exception.HResult"/> property can be used to obtain the error code.
    /// </exception>
    public static TInterface CreateObject<TInterface>(IntPtr library, ArchiveFormat format)
        where TInterface : class
    {
        var createObject = Native.GetExportAs<Native.CreateObjectDelegate>(library, "CreateObject");
        var classId = ComClassList.GetClassId(format);
        var interfaceId = typeof(TInterface).GUID;

        var result = createObject(ref classId, ref interfaceId, out var @interface);

        Marshal.ThrowExceptionForHR(result);

#if NET8_0_OR_GREATER
        unsafe
        {
            return System.Runtime.InteropServices.Marshalling.ComInterfaceMarshaller<TInterface>.ConvertToManaged(@interface.ToPointer());
        }
#else
        return @interface as TInterface;
#endif
    }
}