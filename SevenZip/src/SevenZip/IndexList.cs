using System.Collections.Generic;
using System.Linq;

namespace SevenZip;

/// <summary>
/// The <see cref="IndexList"/> contains the indices of the <see cref="ArchiveEntry"/>
/// objects to include in an archive extract operation.
/// </summary>
public sealed class IndexList
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IndexList"/> class.
    /// </summary>
    /// <param name="indices">The indices to initialize this instance with.</param>
    public IndexList(params int[] indices) => Values = ToArray(indices);

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexList"/> class.
    /// </summary>
    /// <param name="indices">The indices to initialize this instance with.</param>
    public IndexList(IEnumerable<int> indices) => Values = ToArray(indices);

    /// <summary>
    /// Gets an instance of <see cref="IndexList"/> which can be used to include
    /// all archive entries.
    /// </summary>
    public static IndexList All { get; } = new IndexList(null);

    /// <summary>
    /// Gets the indices of the archive entries to include, or <c>null</c>,
    /// to indicate that all indices shall be included.
    /// </summary>
    internal uint[] Values { get; }

    /// <summary>
    /// Gets the numbers of elements stored in <see cref="Values"/> or <see cref="uint.MaxValue"/>,
    /// if <see cref="Values"/> is <c>null</c>.
    /// </summary>
    internal uint Length => Values != null ? (uint)Values.Length : uint.MaxValue;

    private static uint[] ToArray(IEnumerable<int> indices)
    {
        if (indices == null)
        {
            return default;
        }

        return indices.Select(i => (uint)i).ToArray();
    }
}
