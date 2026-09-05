using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;

namespace gex.Coven.Controls {

    [TemplatePart("PART_Image", typeof(Image), IsRequired = true)]
    public partial class FactionImage : TemplatedControl {

        private static Uri _ArmadaUri = new("avares://gex.Coven/Assets/Factions/Armada.png");
        private static Uri _CortexUri = new("avares://gex.Coven/Assets/Factions/Cortex.png");
        private static Uri _LegionUri = new("avares://gex.Coven/Assets/Factions/Legion.png");
        private static Uri _ScavUri = new("avares://gex.Coven/Assets/Factions/Scav.png");
        private static Uri _RaptorsUri = new("avares://gex.Coven/Assets/Factions/Raptor.png");
        private static Uri _RandomUri = new("avares://gex.Coven/Assets/Factions/Random.png");

        public static readonly StyledProperty<string> FactionProperty = AvaloniaProperty.Register<FactionImage, string>(
            name: nameof(Faction),
            defaultValue: "Armada"
        );

        public string Faction {
            get => GetValue(FactionProperty);
            set => SetValue(FactionProperty, value);
        }

        private Image? _Image = null;

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e) {
            base.OnApplyTemplate(e);

            _Image = e.NameScope.Get<Image>("PART_Image");
            _UpdateValue();
        }

        private void _UpdateValue() {
            if (_Image == null) {
                return;
            }

            Uri? uri = null;

            string f = Faction.ToLower();
            if (f == "armada") {
                uri = _ArmadaUri;
            } else if (f == "cortex") {
                uri = _CortexUri;
            } else if (f == "legion") {
                uri = _LegionUri;
            } else if (f == "scav") {
                uri = _ScavUri;
            } else if (f == "raptor") {
                uri = _RaptorsUri;
            } else if (f == "random") {
                uri = _RandomUri;
            }

            if (uri == null) {
                return;
            }

            _Image.Source = new Bitmap(AssetLoader.Open(uri));
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
            base.OnPropertyChanged(change);

            if (change.Property == FactionProperty) {
                _UpdateValue();
            }
        }

    }
}