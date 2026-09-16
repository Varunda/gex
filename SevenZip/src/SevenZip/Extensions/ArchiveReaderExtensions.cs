using System;
using System.IO;

namespace SevenZip.Extensions;

/// <summary>
/// Extension methods for the <see cref="ArchiveReader"/> class.
/// </summary>
public static class ArchiveReaderExtensions
{
    /// <summary>
    /// Extracts a single entry to the given <paramref name="stream"/>.
    /// </summary>
    /// <param name="self">
    /// The <see cref="ArchiveReader"/> instance calling this extension.
    /// </param>
    /// <param name="index">
    /// The index of the <see cref="ArchiveEntry"/> to extract.
    /// </param>
    /// <param name="stream">
    /// The stream to extract the entry to.
    /// </param>
    /// <param name="flags">
    /// Additional flags to configure the extraction behavior. See <see cref="ArchiveFlags"/> for details.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="stream"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="index"/> is
    /// out of range or if <paramref name="stream"/> is not writeable.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if this object has already been disposed.
    /// </exception>
    /// <exception cref="ArchiveOperationException">
    /// Thrown if <see cref="ArchiveConfig.IgnoreOperationErrors"/> is set to <c>false</c> and an
    /// <see cref="OperationResult"/> other than <see cref="OperationResult.Ok"/> is reported by
    /// the native library.
    /// </exception>
    /// <exception cref="Exception">
    /// A more generic exception may be thrown when the native library reports an error.
    /// The <see cref="Exception.HResult"/> property may be used to retrieve the error code.
    /// </exception>
    public static void Extract(this ArchiveReader self, int index, Stream stream, ArchiveFlags flags = ArchiveFlags.None)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (stream.CanWrite is false)
        {
            throw new ArgumentException("The given output stream must be writeable.");
        }

        self.Extract(new IndexList(index), entry => entry.Index == index ? stream : null, flags);
    }

    /// <summary>
    /// Extracts a single entry to the given <paramref name="targetDir"/>
    /// </summary>
    /// <param name="self">
    /// The <see cref="ArchiveReader"/> instance calling this extension.
    /// </param>
    /// <param name="index">
    /// The index of the <see cref="ArchiveEntry"/> to extract.
    /// </param>
    /// <param name="targetDir">
    /// The target directory.
    /// </param>
    /// <param name="flags">
    /// Additional flags to configure the extraction behavior. See <see cref="ArchiveFlags"/> for details.
    /// </param>
    /// 
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="targetDir"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="index"/> is out of range.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if this object has already been disposed.
    /// </exception>
    /// <exception cref="ArchiveOperationException">
    /// Thrown if <see cref="ArchiveConfig.IgnoreOperationErrors"/> is set to <c>false</c> and an
    /// <see cref="OperationResult"/> other than <see cref="OperationResult.Ok"/> is reported by
    /// the native library.
    /// </exception>
    /// <exception cref="Exception">
    /// A more generic exception may be thrown when the native library reports an error.
    /// The <see cref="Exception.HResult"/> property may be used to retrieve the error code.
    /// </exception>
    public static void Extract(this ArchiveReader self, int index, string targetDir, ArchiveFlags flags = ArchiveFlags.None)
    {
        if (string.IsNullOrEmpty(targetDir))
        {
            throw new ArgumentException("The given target directory must not be null or empty.");
        }

        if (Directory.Exists(targetDir) is false)
        {
            Directory.CreateDirectory(targetDir);
        }

        self.Extract(new IndexList(index), StreamCallbackFactory.FileStreams(targetDir, flags), flags | ArchiveFlags.CloseArchiveEntryStreamAfterExtraction);
    }

    /// <summary>
    /// Extracts the entries specified by <paramref name="indices"/> to the given <paramref name="targetDir"/>.
    /// </summary>
    /// <param name="self">
    /// The <see cref="ArchiveReader"/> instance calling this extension.
    /// </param>
    /// <param name="indices">
    /// The indices of the <see cref="ArchiveEntry"/> to extract.
    /// </param>
    /// <param name="targetDir">
    /// The target directory.
    /// </param>
    /// <param name="flags">
    /// Additional flags to configure the extraction behavior. See <see cref="ArchiveFlags"/> for details.
    /// </param>
    /// 
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="targetDir"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="index"/> is out of range.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if this object has already been disposed.
    /// </exception>
    /// <exception cref="ArchiveOperationException">
    /// Thrown if <see cref="ArchiveConfig.IgnoreOperationErrors"/> is set to <c>false</c> and an
    /// <see cref="OperationResult"/> other than <see cref="OperationResult.Ok"/> is reported by
    /// the native library.
    /// </exception>
    /// <exception cref="Exception">
    /// A more generic exception may be thrown when the native library reports an error.
    /// The <see cref="Exception.HResult"/> property may be used to retrieve the error code.
    /// </exception>
    public static void Extract(this ArchiveReader self, IndexList indices, string targetDir, ArchiveFlags flags = ArchiveFlags.None)
    {
        if (string.IsNullOrEmpty(targetDir))
        {
            throw new ArgumentException("The given target directory must not be null or empty.");
        }

        if (Directory.Exists(targetDir) is false)
        {
            Directory.CreateDirectory(targetDir);
        }

        var canonicalTargetDir = Path.GetFullPath(targetDir);
        var onGetStream = StreamCallbackFactory.FileStreams(canonicalTargetDir, true, flags);

        self.Extract(indices, onGetStream, flags | ArchiveFlags.CloseArchiveEntryStreamAfterExtraction);
    }

    /// <summary>
    /// Extracts all entries to the given <paramref name="targetDir"/>.
    /// </summary>
    /// <param name="targetDir">The directory to create the files in.</param>
    /// <param name="flags">Additional flags to configure the extraction behavior. See
    /// <see cref="ArchiveFlags"/> for details.</param>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="targetDir"/> is <c>null</c> or empty.</exception>
    /// <exception cref="IOException">
    /// Thrown if the given <paramref name="targetDir"/> cannot be created in case it does not yet exist.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown anything else failed during extraction
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the object has already been disposed.
    /// </exception>
    /// <exception cref="ArchiveOperationException">
    /// Thrown if <see cref="ArchiveConfig.IgnoreOperationErrors"/> is set to <c>false</c> and an
    /// <see cref="OperationResult"/> other than <see cref="OperationResult.Ok"/> is reported by
    /// the native library.
    /// </exception>
    /// <exception cref="Exception">
    /// A more generic exception may be thrown when the native library reports an error.
    /// The <see cref="Exception.HResult"/> property may be used to retrieve the error code.
    /// </exception>
    public static void ExtractAll(this ArchiveReader self, string targetDir, ArchiveFlags flags = ArchiveFlags.None)
    {
        Extract(self, IndexList.All, targetDir, flags);
    }
}
