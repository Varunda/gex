using System;

namespace gex.Common.Code.ExtensionMethods {

    public static class DateTimeExtensionMethods {

        public static string GetDiscordTimestamp(this DateTime when, string format) {
            return $"<t:{new DateTimeOffset(when).ToUnixTimeSeconds()}:{format}>";
        }

        public static string GetDiscordFullTimestamp(this DateTime when) {
            return when.GetDiscordTimestamp("f");
        }

        public static string GetDiscordRelativeTimestamp(this DateTime when) {
            return when.GetDiscordTimestamp("R");
        }

    }
}
