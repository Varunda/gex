﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gex.Services.Db {

    /// <summary>
    /// Creates and updates the database
    /// </summary>
    public interface IDbCreator {

        /// <summary>
        /// Execute the creator
        /// </summary>
        Task Execute();

    }
}
