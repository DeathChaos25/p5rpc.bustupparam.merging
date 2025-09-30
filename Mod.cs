using CriFs.V2.Hook.Interfaces;
using p5rpc.bustupparam.merging.Configuration;
using p5rpc.bustupparam.merging.Template;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using System.Runtime.InteropServices;
using static p5rpc.bustupparam.merging.Utils;

#if DEBUG
using System.Diagnostics;
#endif

namespace p5rpc.bustupparam.merging
{
    /// <summary>
    /// Your mod logic goes here.
    /// </summary>
    public class Mod : ModBase // <= Do not Remove.
    {
        /// <summary>
        /// Provides access to the mod loader API.
        /// </summary>
        private readonly IModLoader _modLoader;

        /// <summary>
        /// Provides access to the Reloaded logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Entry point into the mod, instance that created this class.
        /// </summary>
        private readonly IMod _owner;

        /// <summary>
        /// Provides access to this mod's configuration.
        /// </summary>
        private Config _configuration;

        /// <summary>
        /// The configuration of the currently executing mod.
        /// </summary>
        private readonly IModConfig _modConfig;

        private ICriFsRedirectorApi _criFsApi = null!;

        private List<string> _probingPaths = new();

        private List<BustupEntry> _bustupEntriesFinal = new();
        private List<BustupParamAssistEntry> _bustupParamAssistEntriesFinal = new();

        public Mod(ModContext context)
        {
            _modLoader = context.ModLoader;
            _logger = context.Logger;
            _owner = context.Owner;
            _configuration = context.Configuration;
            _modConfig = context.ModConfig;

#if DEBUG
            // Attaches debugger in debug mode; ignored in release.
            Debugger.Launch();
#endif

            Initialise(_logger, _configuration, _modLoader);

            var originalFilePath = Path.Combine(_modLoader.GetDirectoryForModId(_modConfig.ModId), "Original");

            _bustupEntriesFinal = BustupParam.ReadBustupFile(Path.Combine(originalFilePath, "BUSTUP_PARAM_Original.DAT"));
            _bustupParamAssistEntriesFinal = BustupParamAssist.ReadBustupParamAssistFile(Path.Combine(originalFilePath, "MSGASSISTBUSTUPPARAM_Original.DAT"));

            _modLoader.GetController<ICriFsRedirectorApi>().TryGetTarget(out _criFsApi!);

            BindDummies();

            _criFsApi.AddBindCallback(CheckBustupParamInMods);
        }

        private void CheckForMerge(string modsPath, string modname = "")
        {
            foreach (var path in _probingPaths)
            {
                string fullPath = Path.Combine(modsPath, path);

                if (!Directory.Exists(fullPath)) continue;

                foreach (var file in Directory.EnumerateFiles(fullPath, "*.DAT", SearchOption.AllDirectories))
                {
                    if (Path.GetFileName(file).ToUpper() == "BUSTUP_PARAM.DAT")
                    {
                        var newEntries = BustupParam.ReadBustupFile(file);
                        _bustupEntriesFinal = BustupParam.MergeEntries(_bustupEntriesFinal, newEntries);
                        Log($"[BUSTUP_PARAM.DAT] Merged entries from mod {modname}");
                    }
                    else if (Path.GetFileName(file).ToUpper() == "MSGASSISTBUSTUPPARAM.DAT")
                    {
                        var newEntries = BustupParamAssist.ReadBustupParamAssistFile(file);
                        _bustupParamAssistEntriesFinal = BustupParamAssist.MergeEntries(_bustupParamAssistEntriesFinal, newEntries);
                        Log($"[MSGASSISTBUSTUPPARAM.DAT] Merged entries from mod {modname}");
                    }
                }
            }
        }

        private void OutputFinalFiles()
        {
            var ownPath = _modLoader.GetDirectoryForModId(_modConfig.ModId);
            var outputPath = Path.Combine(ownPath, "Output");

            var bustupOutputPath = Path.Combine(outputPath, "BUSTUP", "DATA", "BUSTUP_PARAM.DAT");
            var bustupAssistOutputPath = Path.Combine(outputPath, "FONT", "ASSIST", "BUSTUP", "MSGASSISTBUSTUPPARAM.DAT");

            BustupParam.WriteBustupFile(bustupOutputPath, _bustupEntriesFinal);
            BustupParamAssist.WriteBustupParamAssistFile(bustupAssistOutputPath, _bustupParamAssistEntriesFinal);

            LogDebug($"Wrote merged BUSTUP_PARAM.DAT with {_bustupEntriesFinal.Count} entries to {bustupOutputPath}");
            LogDebug($"Wrote merged MSGASSISTBUSTUPPARAM.DAT with {_bustupParamAssistEntriesFinal.Count} entries to {bustupAssistOutputPath}");
        }

        public void BindDummies()
        {
            var ownPath = _modLoader.GetDirectoryForModId(_modConfig.ModId);
            var outputPath = Path.Combine(ownPath, "Output");

            var bustupOutputPath = Path.Combine(outputPath, "BUSTUP", "DATA", "BUSTUP_PARAM.DAT");
            var bustupAssistOutputPath = Path.Combine(outputPath, "FONT", "ASSIST", "BUSTUP", "MSGASSISTBUSTUPPARAM.DAT");

            _criFsApi.AddBind(bustupOutputPath, @"BUSTUP\DATA\BUSTUP_PARAM.DAT", _modConfig.ModId);
            _criFsApi.AddBind(bustupAssistOutputPath, @"FONT\ASSIST\BUSTUP\MSGASSISTBUSTUPPARAM.DAT", _modConfig.ModId);
        }

        private void CheckBustupParamInMods(ICriFsRedirectorApi.BindContext context)
        {
            var loadedMods = _modLoader.GetActiveMods();

            _probingPaths = _criFsApi.GetProbingPaths().ToList();

            foreach (var modconfig in loadedMods)
            {
                var modsPath = _modLoader.GetDirectoryForModId(modconfig.Generic.ModId);

                CheckForMerge(modsPath, modconfig.Generic.ModName);
            }

            OutputFinalFiles();
        }

        #region Standard Overrides
        public override void ConfigurationUpdated(Config configuration)
        {
            // Apply settings from configuration.
            // ... your code here.
            _configuration = configuration;
            _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
        }
        #endregion

        #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Mod() { }
#pragma warning restore CS8618
        #endregion
    }
}