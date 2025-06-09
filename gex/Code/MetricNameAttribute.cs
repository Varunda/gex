using System;

namespace gex.Code {

    /// <summary>
    ///		attribute to automatically register metrics to OTel
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class MetricNameAttribute : Attribute {

        public string Name;

        public MetricNameAttribute(string name) {
            Name = name;
        }

    }

}
