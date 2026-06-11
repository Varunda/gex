using gex.Common.Code.Constants;
using gex.Common.Models;
using gex.Common.Models.Options;
using gex.Services.Repositories;
using ImageMagick;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;

namespace gex.Services.Util {

    public class MapSymmetryUtil {

        private const float MAX_DIFF_ALLOWED = 20000f;

        private readonly ILogger<MapSymmetryUtil> _Logger;
        private readonly MapImageRepository _MapImageRepository;
        private readonly IOptions<FileStorageOptions> _FileStorageOptions;

        public MapSymmetryUtil(ILogger<MapSymmetryUtil> logger,
            MapImageRepository mapImageRepository, IOptions<FileStorageOptions> fileStorageOptions) {

            _Logger = logger;
            _MapImageRepository = mapImageRepository;
            _FileStorageOptions = fileStorageOptions;
        }

        public async Task<Result<MapSymmetryAxis, string>> Find(string mapName) {
            string workingDir = Path.Join(_FileStorageOptions.Value.TempWorkLocation, "map_sym");
            string mapDir = Path.Join(workingDir, mapName);

            await GenerateMaps(mapName, mapDir);

            byte[] original = await _ReadImage(Path.Join(mapDir, "original.jpg"));

            float diffOrig = _Diff(original, original);
            float diffFlip = _Diff(original, await _ReadImage(Path.Join(mapDir, "flip.jpg")));
            float diffFlop = _Diff(original, await _ReadImage(Path.Join(mapDir, "flop.jpg")));
            float diffRotate = _Diff(original, await _ReadImage(Path.Join(mapDir, "rotate90.jpg")));
            float diffFlipRot90 = _Diff(original, await _ReadImage(Path.Join(mapDir, "flip_rotate90.jpg")));
            float diffFlopRot90 = _Diff(original, await _ReadImage(Path.Join(mapDir, "flop_rotate90.jpg")));
            float diffFlipFlop = _Diff(original, await _ReadImage(Path.Join(mapDir, "flip_flop.jpg")));
            float diffFlipFlopRot90 = _Diff(original, await _ReadImage(Path.Join(mapDir, "flip_flop_rotate90.jpg")));

            //Directory.Delete(mapDir, true);

            List<float> diffs = [diffFlip, diffFlop, diffRotate, diffFlipRot90, diffFlopRot90, diffFlipFlop, diffFlipFlopRot90];
            float minDiff = diffs.Min();

            _Logger.LogInformation($"symmetry diffs [map={mapName}] [minDiff={minDiff}] [flip={diffFlip}] [flop={diffFlop}] [rot={diffRotate}] "
                + $"[flip rot={diffFlipRot90}] [flop rot={diffFlopRot90}] [flip flop={diffFlipFlop}] [flip flop rot={diffFlipFlopRot90}]");

            if (minDiff > MAX_DIFF_ALLOWED) {
                return MapSymmetryAxis.MISSING;
            }

            if (minDiff == diffFlip) {
                return MapSymmetryAxis.MIRRORED_HORIZONTAL;
            } else if (minDiff == diffFlop) {
                return MapSymmetryAxis.MIRRORED_VERTICAL;
            } else if (minDiff == diffRotate) {
                return MapSymmetryAxis.MIRRORED_VERTICAL;
            } else if (minDiff == diffFlipRot90) {
                return MapSymmetryAxis.MIRRORED_DIAGONAL;
            } else if (minDiff == diffFlopRot90) {
                return MapSymmetryAxis.MIRRORED_DIAGONAL;
            } else if (minDiff == diffFlipFlop) {
                return MapSymmetryAxis.FLIPPED_HORIZONTAL;
            } else if (minDiff == diffFlipFlopRot90) {
                return MapSymmetryAxis.MIRRORED_VERTICAL;
            }

            Debug.Fail("unchecked result of minDiff");
            throw new Exception($"unchecked minDiff");
        }

        /// <summary>
        ///     generate all the maps used for checking symmetry
        /// </summary>
        /// <param name="mapName"></param>
        /// <returns></returns>
        private async Task GenerateMaps(string mapName, string mapDir) {
            Result<string, string> mapPath = await _MapImageRepository.GetMapPath(mapName, "texture-mq");
            if (mapPath.IsOk == false) {
                _Logger.LogError($"failed to get mapPath for map [mapName={mapName}] [error={mapPath.Error}]");
                return;
            }

            Directory.CreateDirectory(mapDir);

            using MagickImage mImage = new(mapPath.Value);
            mImage.Grayscale();
            mImage.Resize(500, 500);

            // nothing
            string original = Path.Join(mapDir, "original.jpg");
            await mImage.WriteAsync(original);

            await _Op(original, (i) => {
                i.Flip();
            }, Path.Join(mapDir, "flip.jpg"));

            await _Op(original, (i) => {
                i.Rotate(90);
            }, Path.Join(mapDir, "rotate90.jpg"));

            await _Op(original, (i) => {
                i.Flop();
            }, Path.Join(mapDir, "flop.jpg"));

            await _Op(original, (i) => {
                i.Flip();
                i.Rotate(90);
            }, Path.Join(mapDir, "flip_rotate90.jpg"));

            await _Op(original, (i) => {
                i.Flop();
                i.Rotate(90);
            }, Path.Join(mapDir, "flop_rotate90.jpg"));

            await _Op(original, (i) => {
                i.Flop();
                i.Flip();
            }, Path.Join(mapDir, "flip_flop.jpg"));

            await _Op(original, (i) => {
                i.Flop();
                i.Flip();
                i.Rotate(90);
            }, Path.Join(mapDir, "flip_flop_rotate90.jpg"));
        }

        private async Task _Op(string original, Action<MagickImage> ops, string output) {
            using MagickImage mImage = new(original);
            ops(mImage);

            await mImage.WriteAsync(output);
        }

        private async Task<byte[]> _ReadImage(string path) {
            byte[] arr = await File.ReadAllBytesAsync(path);
            SKBitmap img = SKBitmap.Decode(arr);

            List<byte> bytes = [];

            for (int col = 0; col < img.Width; ++col) {
                for (int row = 0; row < img.Height; ++row) {
                    bytes.Add(img.GetPixel(col, row).Red);
                }
            }

            return bytes.ToArray();
        }

        private float _Diff(byte[] input, byte[] target) {

            const float MIN_DIFF = 0.01f;

            if (input.Length != target.Length) {
                throw new ArgumentException($"input and target must have the same length");
            }

            float diff = 0f;

            for (int i = 0; i < input.Length; ++i) {
                byte ii = input[i];
                byte ti = target[i];

                float d = Math.Abs(ii - ti) / 255f;
                if (d > MIN_DIFF) {
                    diff += d;
                }
            }

            return diff;
        }

    }
}
