using System;
using System.IO;

namespace SevenZip.Specialized;

/// <summary>
/// The <see cref="IScopedStreamPool"/> is an interface that serves streams
/// that reference a single preallocated region of managed or native memory
/// to reduce allocations while extracting or compressing data.
/// 
/// Once the capacity limit is reached, a call to <see cref="Discard"/>
/// resets the memory cursor which invalidates all yet allocated streams,
/// so they must not be used anymore.
/// </summary>
internal interface IScopedStreamPool : IDisposable
{
    /// <summary>
    /// Gets the maximum capacity of the stream pool.
    /// </summary>
    int Capacity { get; }

    /// <summary>
    /// Resets the internal memory pool to allow reusing the pool for new
    /// streams. All streams that were allocated previously must no longer
    /// be used once this method has been called.
    /// </summary>
    void Discard();

    /// <summary>
    /// Tests if the pool provides enough remaining capacity to allocate
    /// another stream with the given <paramref name="capacity"/>.
    /// </summary>
    /// <param name="capacity">The desired capacity.</param>
    /// <returns>
    /// <c>true</c>, if another stream can be created with
    /// the given capacity.
    /// </returns>
    bool CanCreateStreamWithCapacity(int capacity);

    /// <summary>
    /// Creates a new <see cref="Stream"/> with the given <paramref name="capacity"/>.
    /// </summary>
    /// <param name="capacity">The capacity of the stream to create.</param>
    /// <returns>
    /// A new stream referencing the memory pool. The stream cannot exceed the given
    /// <paramref name="capacity"/> and must no longer be used once <see cref="Discard"/>
    /// has been called.
    /// </returns>
    /// <exception cref="OutOfMemoryException">
    /// Thrown if the pool does not have enough free space to create a stream with
    /// the given <paramref name="capacity"/>.
    /// </exception>
    Stream CreateStream(int capacity);
}