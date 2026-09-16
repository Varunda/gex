using System.Runtime.InteropServices;

namespace SevenZip;

/// <summary>
/// The <see cref="ArchiveConfig"/> class allows providing additional parameters for
/// an <see cref="ArchiveReader"/>.
/// </summary>
public sealed class ArchiveConfig
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveConfig"/> class. This constructor
    /// set the <see cref="Password"/> to <c>null</c> and the <see cref="NativeLibraryPath"/>
    /// to <c>7z.dll</c>.
    /// </summary>
    public ArchiveConfig() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveConfig"/> class.
    /// </summary>
    /// <param name="password">The password to set.</param>
    /// <param name="ignoreOperationErrors">
    /// Set to <c>false</c> to throw an exception whenever an operation of any entry 
    /// reports a result different than <see cref="OperationResult.Ok"/>.
    /// 
    /// See <see cref="IgnoreOperationErrors"/> for details.
    /// </param>
    /// <param name="nativeLibraryPath">
    /// The native library path. If empty or <c>null</c>, <see cref="NativeLibraryPath"/>
    /// will be set to <c>7z.dll</c>.
    /// </param>
    public ArchiveConfig(string password, bool ignoreOperationErrors, string nativeLibraryPath = null)
        : this(password, nativeLibraryPath)
    {
        IgnoreOperationErrors = ignoreOperationErrors;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveConfig"/> class.
    /// </summary>
    /// <param name="password">The password to set.</param>
    /// <param name="nativeLibraryPath">The native library path. If empty or <c>null</c>,
    /// <see cref="NativeLibraryPath"/> will be set to <c>7z.dll</c>.</param>
    public ArchiveConfig(string password, string nativeLibraryPath = null)
    {
        Password = password;
        NativeLibraryPath = string.IsNullOrEmpty(nativeLibraryPath) ? Default.NativeLibraryPath : nativeLibraryPath;
    }

    /// <summary>
    /// Gets a value indicating whether an exception of type <see cref="ArchiveOperationException"/> shall be thrown
    /// whenever the library reports an <see cref="OperationResult"/> other than <see cref="OperationResult.Ok"/>
    /// for the current archive entry.
    /// 
    /// The default value is <c>false</c>.
    /// </summary>
    /// <remarks>
    /// To throw only on specific results, a custom <see cref="IArchiveReaderDelegate"/> or <see cref="IArchiveWriterDelegate"/>
    /// can be used.
    /// </remarks>
    public bool IgnoreOperationErrors { get; init; } = false;

    /// <summary>
    /// Gets or sets the archive password.
    /// </summary>
    public string Password { get; init; }

    /// <summary>
    /// Gets or sets the path to the native 7z library to load.
    /// </summary>
    public string NativeLibraryPath { get; init; } = GetDefaultLibraryName();

    /// <summary>
    /// Gets the delegate used to generate a uniquely named, zero-byte temporary file on disk.
    /// </summary>
    /// <remarks>
    /// By default, this property uses <see cref="System.IO.Path.GetTempFileName"/> to create
    /// a temporary file.
    /// </remarks>
    public GetTempFileName GetTempFileNameDelegate { get; } = System.IO.Path.GetTempFileName;

    /// <summary>
    /// Gets the default configuration to use when no user configuration is provided.
    /// </summary>
    internal static ArchiveConfig Default { get; } = new();

    /// <summary>
    /// Gets a full path to a uniquely named, zero-byte temporary file on disk.
    /// </summary>
    /// <remarks>
    /// By default, this method uses <see cref="System.IO.Path.GetTempFileName"/> to create
    /// a temporary file. A custom delegate can be specified by setting the
    /// <see cref="GetTempFileNameDelegate"/> property."/>
    /// </remarks>
    internal string GetTempFileName()
    {
        return GetTempFileNameDelegate switch
        {
            { } @delegate => @delegate(),
            null => System.IO.Path.GetTempFileName(),
        };
    }

    /// <summary>
    /// Creates a copy of the given <paramref name="config"/> to prevent that properties may be
    /// changed afterwards externally and to ensure that a native library name is set.
    /// </summary>
    /// <param name="config">The configuration to clone.</param>
    /// <returns>A copy of the given <paramref name="config"/> with a default native
    /// library path, if no path was set. Or, if <paramref name="config"/> was <c>null</c>,
    /// the <see cref="Default"/> instance.</returns>
    internal static ArchiveConfig CloneOrDefault(ArchiveConfig config)
    {
        if (config == null)
        {
            return Default;
        }

        return new ArchiveConfig()
        {
            NativeLibraryPath = string.IsNullOrEmpty(config.NativeLibraryPath) ? Default.NativeLibraryPath : config.NativeLibraryPath,
            IgnoreOperationErrors = config.IgnoreOperationErrors,
            Password = config.Password,
        };
    }

    private static string GetDefaultLibraryName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "7z.dll";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "7z.so";
        }

        throw new System.PlatformNotSupportedException();
    }
}