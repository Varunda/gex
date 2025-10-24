using gex.Common.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Lua;
using Lua.Standard;
using gex.Code.ExtensionMethods;

namespace gex.Services {

    public class LuaRunner {

        private readonly ILogger<LuaRunner> _Logger;

        /// <summary>
        ///		what functions and variables the Lua state running the unit info is allowed to use
        /// </summary>
        private static readonly List<string?> ALLOWED_VARS = [
            "ipairs", "math", "string", "table", "tonumber",
            "tostring", "type", "unpack", "pack", "next", "pairs"
        ];

        public LuaRunner(ILogger<LuaRunner> logger) {
            _Logger = logger;
        }

        /// <summary>
        ///     run a lua script in a safe environment, providing some of the built-ins that Recoil adds
        /// </summary>
        /// <param name="lua">Lua script. Must not be compiled lua</param>
        /// <param name="runDuration">How long to run the Lua script before cancelling</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<Result<object[], string>> Run(string lua, TimeSpan runDuration, CancellationToken cancel) {
            Stopwatch timer = Stopwatch.StartNew();
            Stopwatch stepTimer = Stopwatch.StartNew();

            // compiled lua header byte
            if (lua[0] == 27) {
                return $"refusing to run compiled lua";
            }

            using NLua.Lua l = new();
            object[] output = l.DoString("return _G");
            if (output.Length != 1 || output[0] is not NLua.LuaTable globals) {
                return $"failed to get globals of LuaState for lua";
            }

            // remove all objects from the lua state that are not allowed
            foreach (object? key in globals.Keys) {
                if (key == null) {
                    continue;
                }

                string? iter = key.ToString();
                if (ALLOWED_VARS.Contains(iter) == false) {
                    //_Logger.LogTrace($"setting var to null [iter={iter}]");
                    l[iter] = null;
                }
            }

            long setupLuaMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            object[]? ret = null;
            string? exceptionMessage = null;
            DateTime start = DateTime.UtcNow;
            try {
                Task executeLua = new(() => {
                    string src = @"
                        local Spring = {
                            Utilities = {
                                Gametype = { }
                            }
                        }
                        function Spring.GetModOptions()
                            local t = {}
                            t[""xmas""] = false
                            t[""assistdronesbuildpowermultiplier""] = 1
                            return t
                        end

                        function Spring.Utilities.Gametype.IsRaptors() 
                            return false
                        end

                        function Spring.Utilities.Gametype.IsScavengers() 
                            return false
                        end

                        function table.copy(tbl)
                            local copy = {}
                            for key, value in pairs(tbl) do
                                if type(value) == ""table"" then
                                    copy[key] = table.copy(value)
                                else
                                    copy[key] = value
                                end
                            end
                            return copy
                        end

                        local lowerkeys
                        do
                            local lowerMap = {}

                            local function lowerkeys2(t)
                                if (lowerMap[t]) then
                                    return  -- avoid recursion / repetition
                                end

                                lowerMap[t] = true

                                local changes = {}
                                for k, v in pairs(t) do
                                    if (type(k) == 'string') then
                                        local l = string.lower(k)
                                        if (l ~= k) then
                                            if (t[l] == nil) then
                                                changes[l] = v
                                            end
                                            t[k] = nil
                                        end
                                    end
                                    if (type(v) == 'table') then
                                        lowerkeys2(v)
                                    end
                                end

                                -- insert new keys outside of the pairs() loop
                                for k, v in pairs(changes) do
                                    t[k] = v
                                end
                            end

                            lowerkeys = function(t)
                                lowerMap = {}
                                lowerkeys2(t)
                                return t  -- convenience, do not mistake this for a copy
                            end
                        end
                    " + lua;

                    try {
                        ret = l.DoString(src);
                    } catch (Exception ex) {
                        _Logger.LogError(ex, $"failed to run lua script [lua={src}]");
                        exceptionMessage = ex.Message;
                    }
                });

                start = DateTime.UtcNow;
                l.DebugHook += (object? sender, NLua.Event.DebugHookEventArgs args) => {
                    if (cancel.IsCancellationRequested == true) {
                        // intentionally crash the lua state
                        l.Push("cancelled");
                        l.State.Error("cancelled");
                        return;
                    }

                    TimeSpan diff = DateTime.UtcNow - start;
                    if (diff > runDuration) {
                        l.Push("execution timeout");
                        l.State.Error("execution timeout");
                    }
                };
                l.SetDebugHook(KeraLua.LuaHookMask.Line, 1);

                executeLua.Start();

                // limiting how long the lua takes is done in the debug hook that checks each line 
                // if the timeout was reached, or the cancel token was thrown
                await executeLua.WaitAsync(CancellationToken.None);
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to run lua");
                return Result<object[], string>.Err($"failed to run lua: {ex.Message}");
            }

            long runLuaMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            if (exceptionMessage != null) {
                return Result<object[], string>.Err($"exception running lua: {exceptionMessage}");
            }

            if (ret == null) {
                if (cancel.IsCancellationRequested == true) {
                    return Result<object[], string>.Err($"cancellation requested");
                }
                if (DateTime.UtcNow - start > runDuration) {
                    return Result<object[], string>.Err($"lua execution timed out");
                }

                return Result<object[], string>.Err($"got no return from lua");
            }

            // the default lua state is disposable, and will get disposed of once returned,
            // so gex copies the state into c# stuff (like a table into a Dictionary).
            // this is probably not a perfect lossly conversion, but it handles whatever BAR throws at it
            object[] clone = new object[ret.Length];
            for (int i = 0; i < ret.Length; ++i) {
                clone[i] = Clone(ret[i]);
            }

            return clone;
        }

        private object Clone(object obj) {
            if (obj is NLua.LuaTable table) {
                Dictionary<object, object> dict = [];
                List<object> keys = table.Keys.CopyToList<object>();

                foreach (object key in keys) {
                    dict[key] = Clone(table[key]);
                }

                return dict;
            } else if (obj is string str) {
                return str;
            } else if (obj is long l) {
                return l;
            } else if (obj is double d) {
                return d;
            } else if (obj is bool b) {
                return b;
            } else {
                throw new Exception($"unhandled type of object to clone: {obj.GetType().FullName}");
            }

        }

    }
}
