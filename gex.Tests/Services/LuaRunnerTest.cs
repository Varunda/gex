using gex.Code.ExtensionMethods;
using gex.Models;
using gex.Services;
using gex.Tests.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Tests.Services {

    [TestClass]
    public class LuaRunnerTest {

        private LuaRunner _Make => new LuaRunner(new TestLogger<LuaRunner>());

        /// <summary>
        ///     ensures compiled scripts cannot run
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Run_CompiledScript_Fails() {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
            Result<object[], string> ret = await _Make.Run(Encoding.UTF8.GetString([ 27 ]), TimeSpan.FromSeconds(1), cts.Token);

            Assert.AreEqual(false, ret.IsOk);
            Assert.AreEqual("refusing to run compiled lua", ret.Error);
        }

        /// <summary>
        ///     ensure a script is correctly stopped if execution exceeds runDuration
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Run_ForeverLoop_Exits() {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
            Result<object[], string> ret = await _Make.Run("local i = 0\nwhile true do i = i + 1\n end", TimeSpan.FromSeconds(1), cts.Token);

            Assert.AreEqual(false, ret.IsOk);
            Assert.IsTrue(ret.Error.StartsWith("failed to run lua: execution timeout"));
        }

        /// <summary>
        ///     ensure a script is correctly stopped when a cancellation token is cancelled
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Run_CancelThrown() {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
            Result<object[], string> ret = await _Make.Run("local i = 0\nwhile true do i = i + 1\n end", TimeSpan.FromSeconds(10), cts.Token);

            Assert.AreEqual(false, ret.IsOk);
            Assert.IsTrue(ret.Error.StartsWith("failed to run lua: cancelled"), $"got error: {ret.Error}");
        }

        [TestMethod]
        public async Task Run_Return1() {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
            Result<object[], string> ret = await _Make.Run("return 1", TimeSpan.FromSeconds(1), cts.Token);

            Assert.AreEqual(true, ret.IsOk);
            Assert.AreEqual(1L, ret.Value[0]);
        }

        [TestMethod]
        public async Task Run_ReturnTable() {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
            Result<object[], string> ret = await _Make.Run("return { value = 'hello' }", TimeSpan.FromSeconds(1), cts.Token);

            Assert.AreEqual(true, ret.IsOk);

            Assert.AreEqual(1, ret.Value.Length);

            Dictionary<object, object>? table = ret.Value[0] as Dictionary<object, object>;
            Assert.IsNotNull(table);

            Assert.AreEqual("hello", table["value"]);
        }

    }
}
