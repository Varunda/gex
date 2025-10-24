using Dapper;
using Dapper.ColumnMapper;
using gex.Code;
using gex.Code.Tracking;
using gex.Common;
using gex.Common.Code;
using gex.Common.Models;
using gex.Models;
using gex.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex {

    public class Program {

        // Is set in main
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        private static IHost _Host;
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

        public static async Task Main(string[] args) {
            Console.WriteLine($"starting at {DateTime.UtcNow:u}");

            bool hostBuilt = false;

            CancellationTokenSource stopSource = new();

            using TracerProvider? trace = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("npgsql"))
                .AddAspNetCoreInstrumentation(options => {
                    // only profile api calls
                    options.Filter = (c) => {
                        return c.Request.Path.StartsWithSegments("/api");
                    };
                })
                //.AddNpgsql()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(AppActivitySource.ActivitySourceName))
                .AddSource(AppActivitySource.ActivitySourceName)
                .Build();

            // add meters
            MeterProviderBuilder meterProvider = Sdk.CreateMeterProviderBuilder()
                .AddAspNetCoreInstrumentation()
                .AddMeter("System.Runtime")
                .AddMeter("Gex.LobbyClient")
                .AddMeter("Npgsql");

            foreach (string metricName in GetMetricNames()) {
                meterProvider.AddMeter(metricName);
            }

            meterProvider.AddPrometheusHttpListener(opt => {
                // exposes prometheus metrics on 9184
                opt.UriPrefixes = ["http://localhost:9184"];
            }).Build();

            // Gex must be started in a background thread, as _Host.RunAsync will block until the whole server
            //      shuts down. If we were to await this Task, then it would be blocked until the server is done
            //      running, at which point then the command bus stuff would start
            //
            // That's not useful, because we want to be able to input commands while the server is running,
            //      not after the server is done running
            _ = Task.Run(async () => {
                ILogger<Program>? logger = null;
                try {
                    Stopwatch timer = Stopwatch.StartNew();

                    _Host = CreateHostBuilder(args).Build();
                    logger = _Host.Services.GetRequiredService<ILogger<Program>>();
                    MapDapperTypes(logger);

                    hostBuilt = true;
                    Console.WriteLine($"Took {timer.ElapsedMilliseconds}ms to build program");
                    timer.Stop();
                } catch (Exception ex) {
                    if (logger != null) {
                        logger.LogError(ex, "fatal error starting program");
                    } else {
                        Console.WriteLine($"Fatal error starting program:\n{ex}");
                    }
                }

                try {
                    //await _Host.RunConsoleAsync();
                    await _Host.RunAsync(stopSource.Token);
                } catch (Exception ex) {
                    if (logger != null) {
                        logger.LogError(ex, $"error while running program");
                    } else {
                        Console.WriteLine($"error while running program:\n{ex}");
                    }
                }
            });

            for (int i = 0; i < 10; ++i) {
                await Task.Delay(1000);
                if (hostBuilt == true) {
                    break;
                }
            }

            if (_Host == null) {
                Console.Error.WriteLine($"FATAL> _Host was null after construction");
                return;
            }

            ILogger<Program> logger = _Host.Services.GetRequiredService<ILogger<Program>>();

            CommandBus? commands = _Host.Services.GetService(typeof(CommandBus)) as CommandBus;
            if (commands == null) {
                logger.LogError($"missing CommandBus");
                Console.Error.WriteLine($"Missing ICommandBus");
            }

            // print both incase the logger is misconfigured or something
            logger.LogInformation($"ran host");
            Console.WriteLine($"Ran host");

            bool stop = false;
            AppDomain.CurrentDomain.ProcessExit += (object? sender, EventArgs args) => {
                logger.LogInformation($"process exit called");
                stop = true;
            };
            Console.CancelKeyPress += (object? sender, ConsoleCancelEventArgs e) => {
                logger.LogInformation($"SIGINT detected, exiting");
                stop = true;
            };

            string? line = "";
            bool fastStop = false;
            while (line != ".close") {
                if (stop == true) {
                    break;
                }

                if (Console.KeyAvailable == true) {
                    line = Console.ReadLine();

                    if (line == ".close" || line == ".closefast") {
                        if (line == ".closefast") {
                            fastStop = true;
                        }
                        break;
                    } else {
                        if (commands == null) {
                            logger.LogError($"Missing {nameof(CommandBus)} from host, cannot execute '{line}'");
                            Console.Error.WriteLine($"Missing {nameof(CommandBus)} from host, cannot execute '{line}'");
                        }
                        if (line != null && commands != null) {
                            await commands.Execute(line);
                        }
                    }
                } else {
                    await Task.Delay(10);
                }
            }

            if (fastStop == true) {
                logger.LogInformation($"stopping from 1'000ms");
                Console.WriteLine($"stopping after 1'000ms");

                CancellationTokenSource cts = new();
                cts.CancelAfter(1000 * 1);
                await _Host.StopAsync(cts.Token);
            } else {
                Console.WriteLine($"stopping without a token");
                await _Host.StopAsync();
            }

        }

        public static IHostBuilder CreateHostBuilder(string[] args) {
            IHostBuilder? host = Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging => {
                    // i don't like any of the provided default loggers
                    logging.AddConsole(options => options.FormatterName = "OneLineLogger")
                        .AddConsoleFormatter<OneLineLogger, AppFormatterOptions>(options => {

                        });
                })
                .ConfigureAppConfiguration(appConfig => {
                    appConfig.AddUserSecrets<Startup>();
                    appConfig.AddJsonFile("secrets.json");
                    appConfig.AddJsonFile("env.json");
                }).ConfigureWebHostDefaults(webBuilder => {
                    webBuilder.UseStartup<Startup>();
                });

            return host;
        }

        /// <summary>
        ///		automatically perform Dapper mapping using an attribute
        /// </summary>
        /// <param name="logger"></param>
        private static void MapDapperTypes(ILogger<Program> logger) {

            Type[] types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(iter => iter.GetCustomAttribute<DapperColumnsMappedAttribute>() != null).ToArray();

            foreach (Type t in types) {
                logger.LogDebug($"adding dapper column mapping [type={t.FullName}]");
                SqlMapper.SetTypeMap(t, new ColumnTypeMapper(t));
            }

            SqlMapper.AddTypeHandler(new DapperUnsignedTypeHandlers.UIntHandler());
            SqlMapper.AddTypeHandler(new DapperUnsignedTypeHandlers.ULongHandler());
        }

        /// <summary>
        ///		automatically get all metric names used within the program. uses the <see cref="MetricNameAttribute"/>
        /// </summary>
        /// <returns></returns>
        private static List<string> GetMetricNames() {
            List<string> metricNames = [];

            AppDomain.CurrentDomain.GetAssemblies().ToList().ForEach(iter => {
                Console.WriteLine($"getting metric services from assembly [assembly={iter.FullName}]");
                metricNames.AddRange(GetAssemblyMetrics(iter));
            });

            Assembly common = Assembly.GetAssembly(typeof(GexCommon))
                ?? throw new Exception($"failed to get assembly of type GexCommon");

            metricNames.AddRange(GetAssemblyMetrics(common));

            return metricNames;
        }

        private static List<string> GetAssemblyMetrics(Assembly ass) {
            List<string> metricNames = [];

            Type[] types = ass.GetTypes()
                .Where(iter => iter.GetCustomAttribute<MetricNameAttribute>() != null).ToArray();

            foreach (Type t in types) {
                MetricNameAttribute? attr = t.GetCustomAttribute<MetricNameAttribute>();
                if (attr == null) {
                    continue;
                }

                Console.WriteLine($"adding metric service [type={t.FullName}] [metric name={attr.Name}]");

                metricNames.Add(attr.Name);
            }

            return metricNames;
        }

    }
}
