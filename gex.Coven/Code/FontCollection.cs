using Avalonia.Media.Fonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Code {
    public sealed class FontCollection : EmbeddedFontCollection {

        public FontCollection() : base(
            key: new Uri("fonts:Coven", UriKind.Absolute),
            source: new Uri("avares://gex.Coven/Assets/Fonts", UriKind.Absolute)
        ) { }

    }
}
