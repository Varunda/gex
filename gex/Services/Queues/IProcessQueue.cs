using System;
using System.Collections.Generic;

namespace gex.Services.Queues {

    public interface IProcessQueue {

        List<long> GetProcessTime();

        int Count();

        long Processed();

        /// <summary>
        ///     get the type of object this queue deals with
        /// </summary>
        /// <returns></returns>
        Type GetQueueEntryType();

        void Clear();

    }
}
