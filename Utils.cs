using Reloaded.Mod.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using p5rpc.bustupparam.merging.Configuration;

namespace p5rpc.bustupparam.merging
{
    internal unsafe class Utils
    {
        private static ILogger _logger;
        private static Config _config;
        internal static nint BaseAddress { get; private set; }

        internal static bool Initialise(ILogger logger, Config config, IModLoader modLoader)
        {
            _logger = logger;
            _config = config;
            using var thisProcess = Process.GetCurrentProcess();
            BaseAddress = thisProcess.MainModule!.BaseAddress;

            return true;
        }

        internal static void Log(string message)
        {
            _logger.WriteLineAsync($"[Bustup Param Merging] {message}");
        }

        internal static void LogDebug(string message)
        {
            if (_config.Debug) _logger.WriteLineAsync($"[Bustup Param Merging] {message}");
        }

        internal static void LogWarning(string message)
        {
            _logger.WriteLineAsync($"[Bustup Param Merging] {message}", System.Drawing.Color.Yellow);
        }

        internal static void LogError(string message)
        {
            _logger.WriteLineAsync($"[Bustup Param Merging] {message}", System.Drawing.Color.Red);
        }
    }
}
