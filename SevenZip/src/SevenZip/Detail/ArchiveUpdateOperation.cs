namespace SevenZip.Detail;

/// <summary>
/// Enumeration listing the supported operations to perform when updating an archive.
/// </summary>
internal enum ArchiveUpdateOperation
{
    /// <summary>
    /// Add a new entry to the archive.
    /// </summary>
    Add,

    /// <summary>
    /// Update an existing entry within the archive.
    /// </summary>
    Update,

    /// <summary>
    /// Delete an existing entry from the archive.
    /// </summary>
    Delete,
}
