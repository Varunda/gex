using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace gex.Code.Converters {

    public class ActionLogDoubleConverter : JsonConverter<double> {

        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            // god bless whoever is using unit tweaks that generate values with inf/nan
            if (reader.TokenType == JsonTokenType.String) {
                string? text = reader.GetString();
                if ("NaN".Equals(text, StringComparison.OrdinalIgnoreCase)) {
                    return double.NaN;
                } else if ("-Infinity".Equals(text, StringComparison.OrdinalIgnoreCase)) {
                    return double.NegativeInfinity;
                } else if ("Infinity".Equals(text, StringComparison.OrdinalIgnoreCase)) {
                    return double.PositiveInfinity;
                } else {
                    return double.NaN;
                }
            }

            return reader.GetDouble();
        }

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options) {
            writer.WriteNumberValue(value);
        }

    }
}
