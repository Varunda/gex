using System;
using System.Text.RegularExpressions;

namespace gex.Models.Bar {

    public class BarEngineVersion {

        public const int FORMAT_BAR105 = 1;

        public const int FORMAT_YM = 2;

        private static readonly Regex RegexBar105 = new(@"105\.1\.1-(\d+)-[a-z0-9]{8} BAR105");

        private static readonly Regex RegexYM = new(@"(\d\d\d\d).(\d\d).(\d+)");

        public BarEngineVersion(string engine) {
            Match bar105 = RegexBar105.Match(engine);
            if (bar105.Success == true) {
                if (bar105.Groups.Count > 1) {
                    Format = FORMAT_BAR105;
                    Version = int.Parse(bar105.Groups[1].Value);
                    return;
                }
            }

            Match ym = RegexYM.Match(engine);
            if (ym.Success == true) {
                if (ym.Groups.Count > 3) {
                    Format = FORMAT_YM;
                    int year = int.Parse(ym.Groups[1].Value);
                    int month = int.Parse(ym.Groups[2].Value);
                    int build = int.Parse(ym.Groups[3].Value);
                    Version = (year * 10000) + (month * 100) + build;
                }
            }
        }

        public int Format { get; private set; }

        public int Version { get; private set; }

        public override bool Equals(object? obj) {
            return obj is BarEngineVersion version &&
                   Format == version.Format &&
                   Version == version.Version;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Format, Version);
        }

        public static bool operator ==(BarEngineVersion left, BarEngineVersion right) {
            return left.Format == right.Format && left.Version == right.Version;
        }

        public static bool operator !=(BarEngineVersion left, BarEngineVersion right) {
            return !(left == right);
        }

        public static bool operator >(BarEngineVersion left, BarEngineVersion right) {
            if (left.Format == right.Format) {
                return left.Version > right.Version;
            }
            return left.Format > right.Format;
        }

        public static bool operator <(BarEngineVersion left, BarEngineVersion right) {
            return !(left > right);
        }

        public static bool operator >=(BarEngineVersion left, BarEngineVersion right) {
            return (left == right) || (left > right);
        }

        public static bool operator <=(BarEngineVersion left, BarEngineVersion right) {
            return (left == right) || (left < right);
        }
    }
}
