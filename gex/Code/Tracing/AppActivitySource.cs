using System.Diagnostics;

namespace gex.Code.Tracking {

    public static class AppActivitySource {

        public static readonly string ActivitySourceName = "Gex";

        /// <summary>
        ///     Root activity source timing is done from
        /// </summary>
        public static readonly ActivitySource Root = new ActivitySource(ActivitySourceName);

    }
}
