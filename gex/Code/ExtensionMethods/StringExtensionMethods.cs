namespace gex.Code.ExtensionMethods {

    public static class StringExtensionMethods {

        public static string Truncate(this string str, int length) {
            if (str.Length > length) {
                return str.Substring(0, length - 3) + "...";
            }

            return str;
        }

        public static string EscapeDiscordCharacters(this string str) {
            return str.Replace("_", "\\_")
                .Replace("*", "\\*")
                .Replace("~~", "\\~~");
        }

    }
}
