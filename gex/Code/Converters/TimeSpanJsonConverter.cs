using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace gex.Code.Converters {

    public class TimeSpanJsonConverter : JsonConverter<TimeSpan> {

        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            return TimeSpan.FromMilliseconds(reader.GetInt64());
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options) {
            writer.WriteStringValue(((long)value.TotalMilliseconds).ToString());
        }

    }
}
