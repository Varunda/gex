using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using System;
using System.Globalization;

namespace gex.Coven.Controls {


    [TemplatePart("PART_Label", typeof(TextBlock))]
    public class DateTimeView : TemplatedControl {

        public static readonly DirectProperty<DateTimeView, DateTime?> WhenProperty = AvaloniaProperty.RegisterDirect<DateTimeView, DateTime?>(nameof(When),
            getter: o => o.When,
            setter: (o, v) => o.When = v
        );

        public static readonly StyledProperty<string> FormatProperty = AvaloniaProperty.Register<DateTimeView, string>(
            name: nameof(Format),
            defaultValue: $"yyyy-MM-dd {CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern}"
        );

        public static readonly DirectProperty<DateTimeView, string?> ValueProperty = AvaloniaProperty.RegisterDirect<DateTimeView, string?>(nameof(Value), o => o.Value);

        private DateTime? _when = DateTime.UtcNow;

        public DateTime? When {
            get => _when;
            set => SetAndRaise(WhenProperty, ref _when, value);
        }

        public string Format {
            get => GetValue(FormatProperty);
            set => SetValue(FormatProperty, value);
        }

        private string? _value = null;
        public string? Value {
            get => _value;
            private set { SetAndRaise(ValueProperty, ref _value, value); }
        }

        private void _UpdateValue() {
            if (When == null) {
                Value = null;
                return;
            }

            DateTimeOffset local = TimeZoneInfo.ConvertTimeFromUtc(When.Value, TimeZoneInfo.Local);
            Value = local.ToString(Format);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
            base.OnPropertyChanged(change);

            if (change.Property == WhenProperty) {
                _UpdateValue();
            } else if (change.Property == FormatProperty) {
                _UpdateValue();
            }
        }

    }
}