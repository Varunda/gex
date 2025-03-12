using System.Collections.Generic;

namespace gex.Services.Queues {

    public interface IProcessQueue {

        List<long> GetProcessTime();

        int Count();

        long Processed();

    }
}
