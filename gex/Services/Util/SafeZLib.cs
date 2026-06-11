using gex.Common.Models;
using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Util {

    public class SafeZLib {

        public static async Task<Result<byte[], string>> Decompress(byte[] input, long maxSize, CancellationToken cancel) {
            using MemoryStream stream = new(input);
            using ZLibStream zlib = new(stream, CompressionMode.Decompress);
            using MemoryStream output = new();

            // this is copied from CopyToAsync, but includes a failsafe where if the demofile 
            // gets too large when unzipping, processing exits (anti-zip-bomb)
            //await zlib.CopyToAsync(output, cancel);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1024 * 1024);
            try {
                int totalRead = 0;
                int bytesRead;
                while ((bytesRead = await zlib.ReadAsync(new Memory<byte>(buffer), cancel).ConfigureAwait(false)) != 0) {
                    totalRead += bytesRead;
                    await output.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead), cancel).ConfigureAwait(false);

                    if (totalRead > maxSize) {
                        return $"unsafe size reached";
                    }
                }
            } catch (InvalidDataException ide) {
                return $"failed to decompress data: {ide.Message}";
            } finally {
                ArrayPool<byte>.Shared.Return(buffer);
            }
            byte[] data = output.ToArray();

            return data;
        }

    }
}
