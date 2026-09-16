namespace SevenZip;

/// <summary>
/// Enumeration listing the supported extraction operation types.
/// </summary>
public enum ExtractOperation
{
    /// <summary>
    /// The content of an archive entry is being extracted.
    /// </summary>
    Extract,
        
    /// <summary>
    /// The content of an archive entry is being tested only.
    /// </summary>
    Test,
        
    /// <summary>
    /// The content of an archive entry is being skipped.
    /// </summary>
    Skip
}