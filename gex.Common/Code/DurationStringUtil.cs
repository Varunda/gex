using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Code {

    public class DurationStringUtil {

        /// <summary>
        ///     parse a string into a <see cref="TimeSpan"/>.
        ///     for example, "1h40m" would be translated into a <see cref="TimeSpan"/> of 1 hour and 40 minutes.
        ///     d => days, h => hours, m => minutes, s => seconds (case-insensitive)
        /// </summary>
        /// <param name="input">input string</param>
        /// <returns>
        ///     a <see cref="TimeSpan"/> if <paramref name="input"/> was not empty,
        ///     or <c>null</c> if <paramref name="input"/> was empty
        /// </returns>
        /// <exception cref="Exception">if the input contained invalid characters</exception>
        public static TimeSpan Parse(string input) {
            if (string.IsNullOrWhiteSpace(input)) {
                throw new Exception($"empty input");
            }

            TimeSpan span = TimeSpan.Zero;

            bool daysGiven = false;
            bool hoursGiven = false;
            bool minutesGiven = false;
            bool secondsGiven = false;

            string word = "";
            foreach (char iter in input) {
                char i = char.ToLower(iter);
                if (char.IsDigit(i)) {
                    word += i;
                } else if (i == 'd' || i == 'h' || i == 'm' || i == 's') {
                    if (int.TryParse(word, out int value) == false) {
                        throw new FormatException($"failed to parse '{word}' to a valid int");
                    }

                    word = "";

                    if (i == 'd') {
                        if (daysGiven == true) {
                            throw new ArgumentException($"a days component was already given");
                        }

                        span += TimeSpan.FromDays(value);
                        daysGiven = true;
                    } else if (i == 'h') {
                        if (hoursGiven == true) {
                            throw new ArgumentException($"an hours component was already given");
                        }

                        span += TimeSpan.FromHours(value);
                        hoursGiven = true;
                    } else if (i == 'm') {
                        if (minutesGiven == true) {
                            throw new ArgumentException($"a minutes component was already given");
                        }

                        span += TimeSpan.FromMinutes(value);
                        minutesGiven = true;
                    } else if (i == 's') {
                        if (secondsGiven == true) {
                            throw new ArgumentException($"a seconds component was already given");
                        }

                        span += TimeSpan.FromSeconds(value);
                        secondsGiven = true;
                    } else {
                        throw new FormatException($"unchecked letter character '{i}'");
                    }
                } else {
                    throw new FormatException($"invalid character {i}");
                }
            }

            return span;
        }

        public static bool TryParse(string input, out TimeSpan span) {
            try {
                span = Parse(input);
                return true;
            } catch {
                span = TimeSpan.Zero;
                return false;
            }
        }


    }
}
