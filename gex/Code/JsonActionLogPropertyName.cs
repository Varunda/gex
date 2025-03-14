using System;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace gex.Code {

    /// <summary>
    ///     creates an asymmetric json serializer, where when parsing the action log, the value in this attribute is used,
    ///     but while serializing (for like sending over API), it uses the default serializer
    ///     https://stackoverflow.com/questions/76250152/asymmetric-field-names-for-serialisation-and-deserialisation-using-system-text-j
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
    public abstract class JsonActionLogPropertyNameAttributeBase : Attribute {

        public JsonActionLogPropertyNameAttributeBase(string? name) => this.Name = name;
        public string? Name { get; private set; }

    }

    public sealed class JsonActionLogPropertyNameAttribute : JsonActionLogPropertyNameAttributeBase {

        public JsonActionLogPropertyNameAttribute(string name) : base(name) { }

    }

    public static partial class JsonExtensions {

        public static Action<JsonTypeInfo> UseActionLogNames<TAttribute>() where TAttribute : JsonActionLogPropertyNameAttributeBase => static typeInfo => {
            if (typeInfo.Kind != JsonTypeInfoKind.Object) {
                return;
            }

            foreach (JsonPropertyInfo prop in typeInfo.Properties) {
                if (prop.AttributeProvider?.GetCustomAttributes(typeof(TAttribute), true) is { } list && list.Length > 0) {
                    prop.Name = list.OfType<TAttribute>().FirstOrDefault()?.Name ?? prop.GetMemberName() ?? prop.Name;
                }
            }
        };

        public static string? GetMemberName(this JsonPropertyInfo property) => (property.AttributeProvider as MemberInfo)?.Name;

    }

}
