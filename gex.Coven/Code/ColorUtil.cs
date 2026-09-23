using LiveChartsCore.Drawing;
using LiveChartsCore.Painting;
using LiveChartsCore.SkiaSharpView.Painting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Code {

    public static class ColorUtil {

        public static Paint ToPaint(string hex) {
            if (LvcColor.TryParse(hex, out LvcColor color) == true) {
                return new SolidColorPaint(new SkiaSharp.SKColor(
                    red: color.R, green: color.G, blue: color.B, alpha: color.A
                ));
            }

            throw new FormatException($"unhandled hex '{hex}'");
        }

        public static Paint ToPaint(int color) {
            return ToPaint(color.ToString("X2").PadLeft(6, '0'));
        }

    }
}
