using Avalonia;
using Avalonia.Controls.Primitives;

namespace gex.Coven.Controls {

    public partial class InfoHover : TemplatedControl {
        #region properties

        public static readonly StyledProperty<TablerIcons.Icons> IconProperty = AvaloniaProperty.Register<InfoHover, TablerIcons.Icons>(
            name: nameof(Icon),
            defaultValue: TablerIcons.Icons.IconHelp
        );

        public TablerIcons.Icons Icon {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<InfoHover, string>(
            name: nameof(Text),
            defaultValue: "help text goes here!"
        );

        public string Text {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        #endregion

    }
}