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
        [TestMethod]
        public async Task Run_ForeverLoop_Exits() {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
            Result<object[], string> ret = await _Make.Run("local i = 0\nwhile true do i = i + 1\n end", TimeSpan.FromSeconds(1), cts.Token);

            Assert.AreEqual(false, ret.IsOk);
            Assert.IsTrue(ret.Error.StartsWith("exception running lua: execution timeout"), $"wrong error message, got error: {ret.Error}");
        }

        /// <summary>
        ///     ensure a script is correctly stopped when a cancellation token is cancelled
        /// </summary>
        [TestMethod]
        public async Task Run_CancelThrown() {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
            Result<object[], string> ret = await _Make.Run("local i = 0\nwhile true do i = i + 1\n end", TimeSpan.FromSeconds(10), cts.Token);

            Assert.AreEqual(false, ret.IsOk);
            Assert.IsTrue(ret.Error.StartsWith("exception running lua: cancelled"), $"wrong error message, got error: {ret.Error}");
        }

        /// <summary>
        ///     test for ensuring a basic return is correct
        /// </summary>
        [TestMethod]
        public async Task Run_Return1() {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
            Result<object[], string> ret = await _Make.Run("return 1", TimeSpan.FromSeconds(1), cts.Token);

            Assert.AreEqual(true, ret.IsOk);
            Assert.AreEqual(1L, ret.Value[0]);
        }

        /// <summary>
        ///     test for ensuring that returning a table gives the correct values
        /// </summary>
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

        /// <summary>
        ///     test for ensuring lua code that errors during execution is caught and returns the correct error
        /// </summary>
        [TestMethod]
        public async Task Run_FailsDueToMissingFunction() {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
            Result<object[], string> ret = await _Make.Run("if DoesNotExist.IsHere() then return 1 end", TimeSpan.FromSeconds(1), cts.Token);

            Assert.AreEqual(false, ret.IsOk, $"expected failed lua execution");
            // the line this error can occur can change, so don't check for it
            Assert.IsTrue(ret.Error.StartsWith("exception running lua: [string \"chunk\"]"));
            Assert.IsTrue(ret.Error.Contains("attempt to index a nil value (global 'DoesNotExist')"));
        }

        /// <summary>
        ///     test for ensuring lua code that is bad syntax is caught and returns the correct error
        /// </summary>
        [TestMethod]
        public async Task Run_FailsDueToBadSyntax() {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
            Result<object[], string> ret = await _Make.Run("if DoesNotExist for return 1 end", TimeSpan.FromSeconds(1), cts.Token);

            Assert.AreEqual(false, ret.IsOk, $"expected failed lua execution");
            // the line this error can occur can change, so don't check for it
            Assert.IsTrue(ret.Error.StartsWith("exception running lua: [string \"chunk\"]"), $"got error: {ret.Error}");
            Assert.IsTrue(ret.Error.Contains("'then' expected near 'for'"), $"got error: {ret.Error}");
        }

    }
}
