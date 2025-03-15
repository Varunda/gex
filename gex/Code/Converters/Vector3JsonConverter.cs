using System;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace gex.Code.Converters {

    public class Vector3JsonConverter : JsonConverter<Vector3> {

        // will need to do this if a vector3 is from json, so far it's only output so safe?
        public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options) {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.X);
            writer.WriteNumber("y", value.Y);
            writer.WriteNumber("z", value.Z);
            writer.WriteEndObject();
        }

    }
}
