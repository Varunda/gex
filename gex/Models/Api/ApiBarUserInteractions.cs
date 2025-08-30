using gex.Models.UserStats;

namespace gex.Models.Api {

    public class ApiBarUserInteractions {

        public BarUserInteractions Interactions { get; set; } = new();

        public BarUser? User { get; set; } = null;

    }
}
