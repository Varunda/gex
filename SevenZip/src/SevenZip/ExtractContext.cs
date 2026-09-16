using System;
using System.IO;
using SevenZip.Detail;
using SevenZip.Interop;

namespace SevenZip;

/// <summary>
/// Default implementation of the <see cref="IArchiveExtractCallback"/> interface, which is required to obtain
/// output streams to write extracted content to and to receive status notifications while a decompression
/// is in progress.
/// </summary>
#if NET8_0_OR_GREATER
[System.Runtime.InteropServices.Marshalling.GeneratedComClass]
#endif
internal sealed partial class ExtractContext : MarshalByRefObject, IExtractContext, IArchiveExtractCallback, IPasswordProvider, IDisposable
{
    private readonly CurrentEntry _state = new();
    private readonly IArchiveReaderDelegate _delegate;
    private readonly Func<ArchiveEntry, Stream> _onGetStream;
    private readonly ArchiveReader _reader;
    private readonly ArchiveFlags _flags;
    private ulong _total;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractContext"/> class.
    /// </summary>
    /// <param name="reader">The archive to work with.</param>
    /// <param name="delegate">The delegate to forward status and error notifications to.</param>
    /// <param name="flags">Additional flags to configure the extract operation behavior. See <see cref="ArchiveFlags"/>
    /// for details.</param>
    /// <param name="onGetStream">The callback to invoke when an output stream for an entry is needed by the
    /// decoder.</param>
    public ExtractContext(ArchiveReader reader, IArchiveReaderDelegate @delegate, ArchiveFlags flags, Func<ArchiveEntry, Stream> onGetStream)
    {
        _flags = flags;
        _reader = reader;
        _onGetStream = onGetStream;
        _delegate = @delegate;
        _delegate?.OnProgressBegin(this);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractContext"/> class. This constructor must only
    /// be used for the extraction test mode.
    /// </summary>
    /// <param name="reader">The archive to work with.</param>
    public ExtractContext(ArchiveReader reader)
        : this(reader, null, ArchiveFlags.None, null)
    {
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
        _state.Reset(OperationResult.Ok, null);
    }

    private bool IgnoreOperationErrors => (_reader?.Config?.IgnoreOperationErrors).GetValueOrDefault(false);

    private void EnsureNotDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException("The ExtractContext object has already been disposed.");
        }
    }

    private ArchiveEntry? GetArchiveEntry(uint index)
    {
        if (_reader == null || index >= _reader.Count)
        {
            return null;
        }
            
        return _reader.Entries[(int)index];
    }

    private Stream OnGetStream(int index, in ArchiveEntry? entry, out OperationResult result)
    {
        result = OperationResult.Ok;
        try
        {
            return entry != null ? _onGetStream(entry.Value) : default;
        }
        catch (Exception ex)
        {
            _delegate?.OnGetStreamFailed(this, index, entry, ex);
            result = OperationResult.Unavailable;
            return null;
        }
    }

    private Detail.FileUpdateInfo GetFileUpdateInfo(in ArchiveEntry? entry)
    {
        if (entry == null || (_flags & ArchiveFlags.ApplyArchiveEntryTimestampsToFileStreams) == 0)
        {
            return null;
        }

        return new Detail.FileUpdateInfo()
        {
            CreationTime = entry.Value.CreationTime,
            LastWriteTime = entry.Value.LastWriteTime,
            LastAccessTime = entry.Value.LastAccessTime,
        };
    }

    #region IExtractContext
    ArchiveReader IExtractContext.ArchiveReader => _reader;
    #endregion

    #region IArchiveExtractCallback
    void IArchiveExtractCallback.SetTotal(ulong total)
    {
        _total = total;
    }

    void IArchiveExtractCallback.SetCompleted(ref ulong completeValue)
    {
        _delegate?.OnProgress(this, completeValue, _total);
    }

    int IArchiveExtractCallback.GetStream(uint index, out ISequentialOutputStream output, ExtractOperation operation)
    {
        EnsureNotDisposed();

        var result = OperationResult.Ok;
        var stream = default(ArchiveStream);

        switch (operation)
        {
            case ExtractOperation.Test:
                output = null;
                break;
                
            case ExtractOperation.Skip:
                output = null;
                break;

            case ExtractOperation.Extract:
            default:
                var leaveOpen = (_flags & ArchiveFlags.CloseArchiveEntryStreamAfterExtraction) == 0;
                var entry = GetArchiveEntry(index);

                output = stream = OnGetStream((int)index, entry, out result) switch
                {
                    { } baseStream => new ArchiveStream(baseStream, leaveOpen, GetFileUpdateInfo(entry)),
                    _ => null,
                };
                break;
        }

        _state.Set(index, operation, stream);
        return (int)result;
    }

    void IArchiveExtractCallback.PrepareOperation(ExtractOperation operation)
    {
    }

    void IArchiveExtractCallback.SetOperationResult(OperationResult result)
    {
        _state.Reset(result, this);
    }
    #endregion

    #region IPasswordProvider
    int IPasswordProvider.CryptoGetPassword(out nint password)
    {
        EnsureNotDisposed();

        password = StringMarshal.ManagedStringToBinaryString(_reader?.Config?.Password);
        return 0;
    }
    #endregion

    #region CurrentEntry
    private sealed class CurrentEntry
    {
        private ArchiveStream _stream;
        private uint? _index;
        private ExtractOperation _operation;

        public void Set(uint index, ExtractOperation operation, ArchiveStream stream) =>
            (_index, _operation, _stream) = (index, operation, stream);

        public void Reset(OperationResult result, ExtractContext context)
        {
            ArchiveEntry? currentArchiveEntry = null;

            try
            {
                if (_index != null)
                {
                    currentArchiveEntry = context?.GetArchiveEntry(_index.Value);
                    context?._delegate?.OnExtractOperation(context, (int)_index.Value, currentArchiveEntry, _operation, result);
                }

                if (context?.IgnoreOperationErrors == false && result != OperationResult.Ok)
                {
                    var message = currentArchiveEntry switch
                    {
                        { } instance => $"The '{_operation}' operation failed for archive entry {instance.Index} ('{instance.Path}'): {result}" ,
                        _ => $"The '{_operation}' operation failed: {result}",
                    };

                    throw new ArchiveOperationException(message, result, currentArchiveEntry);
                }
            }
            finally
            {
                _operation = ExtractOperation.Skip;
                _stream?.Dispose();
                _stream = null;
                _index = null;
            }
        }
    }
    #endregion
}