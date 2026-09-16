#if NET6_0_OR_GREATER

using System.IO;
using static System.Math;

namespace SevenZip.Detail;

/// <summary>
/// Provides utility methods for creating and managing temporary files used during archive operations.
/// </summary>
internal static class TempFile
{
    /// <summary>
    /// Creates a temporary file which will optionally be deleted after it has been closed.
    /// </summary>
    /// <param name="config">
    /// The <see cref="ArchiveConfig"/>, used to obtain the temporary file path by
    /// calling <see cref="ArchiveConfig.GetTempFileName"/>.
    /// </param>
    /// <param name="deleteOnClose">
    /// Specifies whether the file shall be deleted when being closed.
    /// </param>
    /// <param name="preallocationSize">
    /// The expected final size of the temporary file.
    /// </param>
    /// <returns>
    /// A <see cref="FileStream"/> providing read and write access to the temporary file.
    /// </returns>
    public static FileStream Create(ArchiveConfig config, bool deleteOnClose, long preallocationSize)
    {
        return File.Open(config.GetTempFileName(), new FileStreamOptions()
        {
            Access = FileAccess.ReadWrite,
            Mode = FileMode.Create,
            BufferSize = Min(81920, Max(4096, (int)(preallocationSize & 0x7FFFFFFF))),
            Options = FileOptions.Asynchronous | (deleteOnClose ? FileOptions.DeleteOnClose : FileOptions.None),
            PreallocationSize = preallocationSize,
        });
    }
}

#endif // NET6_0_OR_GREATER
