using gex.Models;
using gex.Models.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class HeadlessInstanceStatusRepository {

        private readonly ILogger<HeadlessInstanceStatusRepository> _Logger;

        private ConcurrentDictionary<int, HeadlessStatus> _Statuses = new();

        private SemaphoreSlim _Lock = new SemaphoreSlim(1, 1);

        public HeadlessInstanceStatusRepository(ILogger<HeadlessInstanceStatusRepository> logger) {
            _Logger = logger;
        }

        /// <summary>
        ///		register a new runner under an ID
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public async Task<Result<bool, string>> RegisterRunner(int ID) {
            try {
                await _Lock.WaitAsync(TimeSpan.FromSeconds(10));
            } catch (Exception) {
                return "failed to aquire semaphore after 10 seconds";
            }

            if (_Statuses.ContainsKey(ID) == true) {
                _Lock.Release();
                return $"runner with ID of {ID} is already registered";
            }

            bool res = _Statuses.TryAdd(ID, new HeadlessStatus(ID));
            if (res == false) {
                _Lock.Release();
                return $"TryAdd returned false when adding {ID} to dictionary";
            }

            _Lock.Release();
            return true;
        }

        /// <summary>
        ///		get the status of all runners registered here
        /// </summary>
        /// <returns></returns>
        public async Task<Result<List<HeadlessStatus>, string>> Get() {
            try {
                await _Lock.WaitAsync(TimeSpan.FromSeconds(10));
            } catch (Exception) {
                return "failed to aquire semaphore after 10 seconds";
            }

            List<HeadlessStatus> ret = new(_Statuses.Values);

            _Lock.Release();
            return ret;
        }

        public async Task<Result<bool, string>> Update(int ID, HeadlessProcessStatus? proc) {
            try {
                await _Lock.WaitAsync(TimeSpan.FromSeconds(10));
            } catch (Exception) {
                return "failed to aquire semaphore after 10 seconds";
            }

            if (_Statuses.TryGetValue(ID, out HeadlessStatus? status) == false || status == null) {
                _Lock.Release();
                return $"no runner with ID {ID} was registered";
            }

            status.Status = proc;
            _Lock.Release();

            return true;
        }

        public Task<Result<bool, string>> Clear(int ID) {
            return Update(ID, null);
        }


    }
}
