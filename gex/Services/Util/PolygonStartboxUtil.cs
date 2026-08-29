using DSharpPlus.EventArgs;
using gex.Code.ExtensionMethods;
using gex.Common.Models;
using gex.Models.Bar;
using gex.Models.Db;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace gex.Services.Util {

    public class PolygonStartboxUtil {

        /**
         * much of this code is based on BAR's Lua implementation of this:
         * https://github.com/beyond-all-reason/Beyond-All-Reason/pull/7513
         */

        private readonly ILogger<PolygonStartboxUtil> _Logger;

        public PolygonStartboxUtil(ILogger<PolygonStartboxUtil> logger) {
            _Logger = logger;
        }

        /// <summary>
        ///     convert a list of spline-anchor points into a discrete polygon that can be easily used to
        ///     check if an (x, z) point is within it. <paramref name="anchors"/> is assumed to be a closed
        ///     set of points, where the last one implicitly joins to the last one
        /// </summary>
        /// <param name="anchors">list of anchor points used to tessellate into a polygon</param>
        /// <param name="segmentCount">number of segments between anchor points</param>
        /// <returns>
        ///     a list of (X, Z) points that represents the tessellated spline-anchor points,
        ///     with <paramref name="segmentCount"/> points between points with an anchor, 
        ///     or 0 points if the <see cref="PolygonStartbox.Anchor"/> has a <see cref="PolygonStartbox.Anchor.Strength"/> of 0
        /// </returns>
        public List<Pair> TessellateRing(List<PolygonStartbox.Anchor> anchors, int segmentCount = 12) {

            int n = anchors.Count;
            List<Pair> pairs = [];

            if (n < 2) {
                for (int i = 0; i < n; ++i) {
                    pairs.Add(new Pair() {
                        X = anchors[i].X,
                        Z = anchors[i].Z
                    });
                }

                return pairs;
            }

            segmentCount = Math.Max(1, segmentCount);

            for (int i = 1; i <= n; ++i) {
                int iPrev = mod(i - 2, n) + 1;
                int iNext = mod(i, n) + 1;
                int iNext2 = mod(iNext, n) + 1;

                PolygonStartbox.Anchor p0 = anchors[iPrev - 1];
                PolygonStartbox.Anchor p1 = anchors[i - 1];
                PolygonStartbox.Anchor p2 = anchors[iNext - 1];
                PolygonStartbox.Anchor p3 = anchors[iNext2 - 1];

                double s1 = p1.Strength;
                double s2 = p2.Strength;
                double edgeTension = Clamp((Clamp(s1) + Clamp(s2)) * 0.5d);

                pairs.Add(new Pair() {
                    X = p1.X,
                    Z = p1.Z
                });

                if (edgeTension > 0 && n >= 3) {
                    for (int k = 0; k < segmentCount - 1; ++k) {
                        (double x, double z) = SampleSegment(p0, p1, p2, p3, (k + 1) / (double)segmentCount, edgeTension);
                        pairs.Add(new Pair() {
                            X = x,
                            Z = z
                        });
                    }
                }
            }

            return pairs;
        }

        /// <summary>
        ///     parse a JSON string that contains the startbox polygon info
        /// </summary>
        /// <param name="input">input json string</param>
        /// <returns></returns>
        public Result<PolygonStartbox, string> ParseJson(string input) {
            JsonElement json = JsonSerializer.Deserialize<JsonElement>(input);
            if (json.ValueKind != JsonValueKind.Object) {
                return $"expected input json to be an object, got a {json.ValueKind} instead";
            }

            JsonElement? startBoxes = json.GetChild("startboxes");
            if (startBoxes == null) {
                foreach (JsonProperty iter in json.EnumerateObject()) {
                    startBoxes = iter.Value.GetChild("startboxes");
                    break;
                }

                if (startBoxes == null) {
                    Debug.Fail("where startboxes");
                    return $"missing starboxes element";
                }
            }

            if (startBoxes.Value.ValueKind != JsonValueKind.Array) {
                return $"expected element startboxes to be an array, got a {startBoxes.Value.ValueKind} instead";
            }

            PolygonStartbox startbox = new();

            int index = 0;
            foreach (JsonElement iter in startBoxes.Value.EnumerateArray()) {
                JsonElement poly = iter.GetRequiredChild("poly");

                if (poly.ValueKind != JsonValueKind.Array) {
                    return $"expected element poly to be an array, got a {poly.ValueKind} instead";
                }

                PolygonStartbox.Side side = new();
                side.Index = index;

                foreach (JsonElement anchor in poly.EnumerateArray()) {

                    double x = anchor.GetDouble("x", 0d);
                    double z = 0d;
                    if (anchor.GetChild("y") == null) { // idk maybe they swap to Z at some point instead of Y
                        z = anchor.GetDouble("z", 0d);
                    } else {
                        z = anchor.GetDouble("y", 0d);
                    }

                    JsonElement? strNode = anchor.GetChild("strength");
                    double strength = 0d;
                    if (strNode != null) {
                        strength = strNode.Value.GetDouble();
                    }

                    side.Anchors.Add(new PolygonStartbox.Anchor() {
                        X = x,
                        Z = z,
                        Strength = strength
                    });
                }

                ++index;
                startbox.Sides.Add(side);
            }

            return startbox;
        }

        /// <summary>
        ///     parse the base64 string that contains a compressed zlib json string
        /// </summary>
        /// <param name="input">base64 input string</param>
        /// <returns></returns>
        public Result<PolygonStartbox, string> Parse(string input) {
            byte[] compressed = Base64Url.DecodeFromChars(input);

            using MemoryStream stream = new(compressed);
            using ZLibStream zlib = new(stream, CompressionMode.Decompress);
            using MemoryStream output = new();

            zlib.CopyTo(output);

            string json = Encoding.UTF8.GetString(output.ToArray());

            return ParseJson(json);
        }

        /// <summary>
        ///     get a polygon startbox from a <see cref="BarMatch"/>. first the game setting <code>mapmetadata_startbox_override</code>
        ///     is used, then <code>mapmetadata_startbox_set</code> if not found
        /// </summary>
        /// <param name="match">match that contains the game settings used to parse the polygon startbox from</param>
        /// <returns></returns>
        public Result<Maybe<PolygonStartbox>, string> GetFromMatch(BarMatch match) {
            JsonElement? value = match.GameSettings.GetChild("mapmetadata_startboxes_override");
            if (value == null || IsValidMapDataElement(value) == false) {
                value = match.GameSettings.GetChild("mapmetadata_startboxes_set");

                if (value == null || IsValidMapDataElement(value) == false) {
                    return Result<Maybe<PolygonStartbox>, string>.Ok(Maybe<PolygonStartbox>.None());
                }
            }

            if (value.Value.ValueKind != JsonValueKind.String) {
                _Logger.LogWarning($"unexpected valuekind for mapmetadata_startboxes_set [gameID={match.ID}] [valuekind={value.Value.ValueKind}]");
                return Result<Maybe<PolygonStartbox>, string>.Ok(Maybe<PolygonStartbox>.None());
            }

            Result<PolygonStartbox, string> parsed = Parse(value.Value.GetString()!);
            if (parsed.IsOk) {
                return Maybe<PolygonStartbox>.Some(parsed.Value);
            }
            return Result<Maybe<PolygonStartbox>, string>.Err(parsed.Error);
        }

        private static bool IsValidMapDataElement(JsonElement? elem) {
            return elem != null
                && elem.Value.ValueKind == JsonValueKind.String
                && elem.Value.GetString() != null
                && elem.Value.GetString() != "";
        }

        /// <summary>
        ///     check if a point is within a polygon defines by a list of vertices
        /// </summary>
        /// <param name="verts">list of verticies. implied that the list vertex joins to the first one</param>
        /// <param name="x">x point</param>
        /// <param name="z">z point</param>
        /// <returns>if the (x, z) pair is within the list of verts</returns>
        public bool PointWithinPolygon(List<Pair> verts, double x, double z) {
            int n = verts.Count;
            if (n < 3) {
                return false;
            }

            bool inside = false;
            int j = n - 1;
            for (int i = 0; i < n; ++i) {
                (double xi, double zi) = (verts[i].X, verts[i].Z);
                (double xj, double zj) = (verts[j].X, verts[j].Z);

                if ((zi > z) != (zj > z)) {
                    double intersectX = xj + (z - zj) * (xi - xj) / (zi - zj);
                    if (x < intersectX) {
                        inside = !inside;
                    }
                }

                j = i;
            }

            return inside;
        }

        public struct Pair {
            public double X { get; set; }
            public double Z { get; set; }
        }

        private static double Clamp(double v) {
            if (v < 0d) {
                return 0d;
            }
            if (v > 1d) {
                return 1d;
            }
            return v;
        }

        private static double KnotDelta(PolygonStartbox.Anchor p0, PolygonStartbox.Anchor p1) {
            double dx = p1.X - p0.X;
            double dz = p1.Z - p0.Z;
            return Math.Pow((dx * dx + dz * dz), 0.25d);
        }

        private static (double, double) BgLerp(double tt, double ax, double az, double bx, double bz, double ta, double tb) {
            double w = (tb - tt) / (tb - ta);
            return (
                w * ax + (1 - w) * bx,
                w * az + (1 - w) * bz
            );
        }

        private static (double, double) SampleSegment(PolygonStartbox.Anchor p0, PolygonStartbox.Anchor p1,
            PolygonStartbox.Anchor p2, PolygonStartbox.Anchor p3, double t, double tension) {

            double lx = p1.X + (p2.X - p1.X) * t;
            double lz = p1.Z + (p2.Z - p1.Z) * t;
            if (tension <= 0) {
                return (lx, lz);
            }

            double t0 = 0;
            double t1 = t0 + KnotDelta(p0, p1);
            double t2 = t1 + KnotDelta(p1, p2);
            double t3 = t2 + KnotDelta(p2, p3);

            double crX = 0d;
            double crZ = 0d;

            if (t2 - t1 <= 1e-9) {
                crX = p1.X;
                crZ = p1.Z;
            } else {
                double tt = t1 + (t2 - t1) * t;
                (double A1x, double A1z) = (p1.X, p1.Z);
                if (t1 - t0 > 1e-9) {
                    (A1x, A1z) = BgLerp(tt, p0.X, p0.Z, p1.X, p1.Z, t0, t1);
                }

                (double A2x, double A2z) = BgLerp(tt, p1.X, p1.Z, p2.X, p2.Z, t1, t2);

                (double A3x, double A3z) = (p2.X, p2.Z);
                if (t3 - t2 > 1e-9) {
                    (A3x, A3z) = BgLerp(tt, p2.X, p2.Z, p3.X, p3.Z, t2, t3);
                }

                (double B1x, double B1z) = BgLerp(tt, A1x, A1z, A2x, A2z, t0, t2);
                (double B2x, double B2z) = BgLerp(tt, A2x, A2z, A3x, A3z, t1, t3);
                (crX, crZ) = BgLerp(tt, B1x, B1z, B2x, B2z, t1, t2);
            }

            if (tension >= 1) {
                return (crX, crZ);
            }

            return (
                lx + (crX - lx) * tension,
                lz + (crZ - lz) * tension
            );
        }

        /// <summary>
        ///     the % operator in c# is a remainder, not a mod operator like in lua,
        ///     which causes different results for negative numbers
        /// </summary>
        /// <param name="x">input number to be mod</param>
        /// <param name="m">mod value</param>
        /// <returns></returns>
        private static int mod(int x, int m) {
            int r = x % m;
            return r < 0 ? r + m : r;
        }

    }
}
