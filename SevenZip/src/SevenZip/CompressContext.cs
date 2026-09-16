using System;
using System.Collections.Generic;
using SevenZip.Detail;
using SevenZip.Interop;

namespace SevenZip;

/// <summary>
/// Default implementation of the <see cref="IArchiveUpdateCallback"/> interface, which is required when
/// creating or modifying an archive.
/// </summary>
#if NET8_0_OR_GREATER
[System.Runtime.InteropServices.Marshalling.GeneratedComClass]
#endif
internal sealed partial class CompressContext : MarshalByRefObject, ICompressContext, IArchiveUpdateCallback, IPasswordProvider, IPasswordProvider2, IDisposable
{
    private readonly CurrentEntry _state = new();
    private readonly IArchiveWriterDelegate _delegate;
    private readonly ArchiveWriter _writer;
    private readonly List<ArchiveStream> _deferredStreams;
    private readonly ArchiveIndexBuffer _indices;
    private ulong _total;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompressContext"/> class.
    /// </summary>
    /// <param name="writer">The archive writer.</param>
    /// <param name="delegate">The delegate to forward status and error notifications to.</param>
    public CompressContext(ArchiveWriter writer, IArchiveWriterDelegate @delegate)
    {
        _writer = writer;
        _indices = writer.UpdateContext.CreateArchiveIndexBuffer();
        _deferredStreams = CanDisposeStreamImmediately(writer.ArchiveFormat) ? null : [];
        _delegate = @delegate;
        _delegate?.OnProgressBegin(this);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _delegate?.OnProgressEnd(this);
        _disposed = true;
        _state.Reset(OperationResult.Ok, null, _deferredStreams);

        if (_deferredStreams != null)
        {
            _deferredStreams.ForEach(stream => stream.Dispose());
            _deferredStreams.Clear();
        }
    }

    private bool IgnoreOperationErrors => (_writer?.Config?.IgnoreOperationErrors).GetValueOrDefault(false);

    private void EnsureNotDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException("The CompressContext object has already been disposed.");
        }
    }

    private static bool CanDisposeStreamImmediately(ArchiveFormat format) => format != ArchiveFormat.Zip;

    #region ICompressContext
    ArchiveWriter ICompressContext.ArchiveWriter => _writer;
    #endregion

    #region IArchiveUpdateCallback
    void IArchiveUpdateCallback.SetTotal(ulong total)
    {
        _total = total;
    }

    void IArchiveUpdateCallback.SetCompleted(ref ulong completeValue)
    {
        _delegate?.OnProgress(this, completeValue, _total);
    }

    int IArchiveUpdateCallback.GetUpdateItemInfo(uint index, ref int newData, ref int newProperties, ref uint indexInArchive)
    {
        EnsureNotDisposed();

        if (_indices.TryGetArchiveUpdateEntry(index, out var entry))
        {
            switch (entry.UpdateOperation)
            {
                case ArchiveUpdateOperation.Update:
                    newData = 1;
                    newProperties = 1;
                    indexInArchive = _indices.GetIndexInExistingArchive(index);
                    break;

                case ArchiveUpdateOperation.Add:
                    newData = entry.IsDirectory ? 0 : 1;
                    newProperties = 1;
                    indexInArchive = uint.MaxValue;
                    break;
            }
        }
        else
        {
            newData = 0;
            newProperties = 0;
            indexInArchive = _indices.GetIndexInExistingArchive(index);
        }

        return 0;
    }

    int IArchiveUpdateCallback.GetProperty(uint index, ArchiveEntryProperty property, ref Union value)
    {
        EnsureNotDisposed();

        if (_indices.TryGetArchiveUpdateEntry(index, out var updateEntry))
        {
            return EntryPropertyToUnion.Convert(updateEntry, property, ref value);
        }
        else
        {
            // 7z only requests the properties of modified or new entries.

            return 0;
        }
    }

    int IArchiveUpdateCallback.GetStream(uint index, out ISequentialInputStream reader)
    {
        EnsureNotDisposed();

        var stream = default(ArchiveStream);

        reader = null;

        if (_indices.TryGetArchiveUpdateEntry(index, out var entry))
        {
            try
            {
                stream = entry.CreateStream();
                reader = stream;
            }
            catch (Exception ex)
            {
                stream = null;
                reader = null;

                _delegate?.OnGetStreamFailed(this, (int)index, entry.ArchivePath, ex);
            }
        }

        _state.Set(index, stream, entry.ArchivePath);
        return reader != null ? 0 : -1;
    }

    void IArchiveUpdateCallback.SetOperationResult(OperationResult operationResult)
    {
        EnsureNotDisposed();

        _state.Reset(operationResult, this, _deferredStreams);
    }
    #endregion

    #region IPasswordProvider
    int IPasswordProvider.CryptoGetPassword(out nint password)
    {
        EnsureNotDisposed();

        password = StringMarshal.ManagedStringToBinaryString(_writer?.Config?.Password);
        return 0;
    }
    #endregion

    #region IPasswordProvider2
    int IPasswordProvider2.CryptoGetTextPassword2(ref int passwordIsDefined, out nint password)
    {
        EnsureNotDisposed();

        password = StringMarshal.ManagedStringToBinaryString(_writer?.Config?.Password);
        passwordIsDefined = password != IntPtr.Zero ? 1 : 0;
        return 0;
    }
    #endregion

    #region CurrentEntry
    private sealed class CurrentEntry
    {
        private ArchiveStream _stream;
        private string _path;
        private uint? _index;

        public void Set(uint index, ArchiveStream stream, string path)
        {
            _stream = stream;
            _index = index;
            _path = path;
        }

        public void Reset(OperationResult result, CompressContext context, ICollection<ArchiveStream> deferredStreams)
        {
            if (deferredStreams != null && _stream != null)
            {
                deferredStreams.Add(_stream);
            }
            else
            {
                _stream?.Dispose();
            }

            try
            {
                if (context != null && _index != null)
                {
                    context?._delegate?.OnCompressOperation(context, (int)_index, _path ?? string.Empty, result);
                }

                if (context?.IgnoreOperationErrors == false && result != OperationResult.Ok)
                {
                    var info = _path != null ? $" ('{_path}')" : string.Empty;
                    var message = _index switch
                    {
                        { } index => $"The compress operation failed for archive entry {index}{info}: {result}" ,
                        _ => $"The compress operation failed{info}: {result}",
                    };

                    throw new ArchiveOperationException(message, result, (int?)_index, _path);
                }
            }
            finally
            {
                _path = null;
                _index = null;
                _stream = null;
            }
        }
    }
    #endregion
}