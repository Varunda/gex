using System;

namespace gex.Common.Code.ExtensionMethods {

    public static class StringExtensionMethods {

        public static string Truncate(this string str, int length) {
            if (str.Length > length) {
                if (str.Length - length < 3) {
                    return str.Substring(0, length);
                }
                return str.Substring(0, length - Math.Min(3, Math.Max(0, length - 3))) + "...";
            }

            return str;
        }

        public static string EscapeDiscordCharacters(this string str) {
            return str.Replace("_", "\\_")
                .Replace("*", "\\*")
                .Replace("~~", "\\~~");
        }

        public static string EscapeRecoilFilesytemCharacters(this string str) {
            // https://github.com/beyond-all-reason/pr-downloader/blob/8263d019b8b048f3939ddfbc2595d7f10ef45bf4/src/FileSystem/FileSystem.cpp#L698
            // const static std::string illegalChars = "\\/:?\"<>|";

            char[] IllegalCharactes = [
                '\\', '/', ':',
                '?', '"', '|',
                '<', '>',
                // these 2 are added for Gex and aren't from pr-downloader
                ' ', '\''
            ];

            string mut = str;
            foreach (char illegal in IllegalCharactes) {
                mut = mut.Replace(illegal, '_');
            }

            return mut;
        }

    }
}
