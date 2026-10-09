using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Text;

namespace gex.Coven.ViewModels.Match {

    public class BarMatchTag {

        public BarMatchTag() {

        }

        public BarMatchTag(string name, IBrush textBrush, IBrush? fillBrush) {
            Name = name;
            TextBrush = textBrush;

            if (fillBrush is not null) {
                FillBrush = fillBrush;
            } else {
                {
                    if (TextBrush is SolidColorBrush textColor) {
                        HslColor hsl = textColor.Color.ToHsl();
                        HslColor darker = new(hsl.A, hsl.H, hsl.S, hsl.L * 0.2d);

                        FillBrush = new SolidColorBrush(darker.ToRgb());
                    }
                }

                { 
                    if (TextBrush is IImmutableSolidColorBrush textColor) {
                        HslColor hsl = textColor.Color.ToHsl();
                        HslColor darker = new(hsl.A, hsl.H, hsl.S, hsl.L * 0.2d);

                        FillBrush = new SolidColorBrush(darker.ToRgb());
                    }
                }
            }
        }

        public string Name { get; set; } = "";

        public IBrush TextBrush { get; set; } = Brushes.Black;

        public IBrush FillBrush { get; set; } = Brushes.White;

    }
}
