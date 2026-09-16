using System;

namespace SevenZip;

/// <summary>
/// Flags used to configure the behavior of <see cref="ArchiveReader"/> operations.
/// </summary>
[Flags]
public enum ArchiveFlags
{
    /// <summary>
    /// No flags at all, use the default behavior.
    /// </summary>
    None = 0,
        
    /// <summary>
    /// Dispose any stream that was created during an extract operation.
    /// </summary>
    CloseArchiveEntryStreamAfterExtraction = 1 << 0,

    /// <summary>
    /// Apply the original timestamps stored in the archive to extracted
    /// files. This flag requires that <see cref="CloseArchiveEntryStreamAfterExtraction"/>
    /// is set and that the associated stream is a <see cref="System.IO.FileStream"/>.
    /// 
    /// In cases where the archive entry does not contain a timestamp (<see cref="DateTime.MinValue"/>),
    /// the file property is not being updated.
    /// </summary>
    ApplyArchiveEntryTimestampsToFileStreams = 1 << 1,

    /// <summary>
    /// Do not overwrite already existing files in the target directory.
    /// </summary>
    /// <remarks>
    /// This flag is only recognized when using an overload of <c>Extract</c> or <c>ExtractAll</c>
    /// which accepts a target directory. Overloads accepting a <see cref="System.IO.Stream"/>
    /// or the <see cref="Func{T, TResult}<"/> callback to provide a custom stream ignore this flag.
    /// </remarks>
    SkipExistingFilesInTargetDir = 1 << 10,

    /// <summary>
    /// Strip the directories from an archive path and use only the filename. All entries will
    /// be written directly to the target directory and no subdirectories will be created.
    /// </summary>
    /// <remarks>
    /// This flag is only recognized when using an overload of <c>Extract</c> or <c>ExtractAll</c>
    /// which accepts a target directory. Overloads accepting a <see cref="System.IO.Stream"/>
    /// or the <see cref="Func{T, TResult}<"/> callback to provide a custom stream ignore this flag.
    /// </remarks>
    FlattenArchiveEntryPaths = 1 << 11,
}
