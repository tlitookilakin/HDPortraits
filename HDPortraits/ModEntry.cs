using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using System.Collections.Generic;
using HDPortraits.Models;

namespace HDPortraits
{
    public class ModEntry : Mod
    {
        internal ITranslationHelper i18n => Helper.Translation;
        internal static IMonitor monitor;
        internal static IModHelper helper;
        internal static Harmony harmony;
        internal static string ModID;
        private static IHDPortraitsAPI api = new API();
        private static readonly HashSet<string> failedPaths = new();

        internal static Dictionary<string, MetadataModel> portraitSizes = new();
        internal static Dictionary<string, MetadataModel> backupPortraits = new();

        public override void Entry(IModHelper helper)
        {
            Monitor.Log("Starting up...", LogLevel.Debug);

            monitor = Monitor;
            ModEntry.helper = Helper;
            harmony = new(ModManifest.UniqueID);
            ModID = ModManifest.UniqueID;
            helper.Events.Content.AssetRequested += LoadDefaultAsset;
            helper.Events.Content.AssetsInvalidated += TryReloadAsset;
            helper.Events.GameLoop.GameLaunched += GameLaunched;
        }
        private void GameLaunched(object _, GameLaunchedEventArgs ev)
        {
            harmony.PatchAll();
            Patches.STFPatch.Init();
            ReloadBaseData();
        }
        public override object GetApi() => api;
        private void LoadDefaultAsset(object _, AssetRequestedEventArgs ev)
        {
            if (ev.Name.IsEquivalentTo("Mods/HDPortraits"))
                ev.LoadFromModFile<Dictionary<string, MetadataModel>>("assets/default.json", AssetLoadPriority.Low);
        }
        private void TryReloadAsset(object _, AssetsInvalidatedEventArgs ev)
        {
            foreach(var name in ev.Names)
                if (name.IsEquivalentTo("Mods/HDPortraits"))
                    ReloadBaseData();

                else if(name.IsDirectlyUnderPath("Mods/HDPortraits"))
                    ReloadItem(name);
        }
        private static void ReloadItem(IAssetName name)
        {
            monitor.Log($"Reloading portrait metadata for '{name}'...", LogLevel.Debug);
            var localPath = name.WithoutPath("Mods/HDPortraits");
            failedPaths.Remove(localPath);
            portraitSizes.Remove(localPath);
        }
        private static void ReloadBaseData()
        {
            monitor.Log("Reloading portrait data...", LogLevel.Debug);
            backupPortraits = helper.GameContent.Load<Dictionary<string, MetadataModel>>("Mods/HDPortraits");
            foreach ((string id, MetadataModel meta) in backupPortraits)
            {
                meta.defaultPath = "Portraits/" + id;
                meta.Reload();
                failedPaths.Remove(id);
            }
        }
        public static bool TryGetMetadata(string name, string suffix, out MetadataModel meta)
        {
            string path = $"{name}_{suffix}";
            if (suffix is not null)
            {
                if (portraitSizes.TryGetValue(path, out meta) && meta is not null)
                    return true; //cached

                if (!failedPaths.Contains(path) && 
                    (Utils.TryLoadAsset("Mods/HDPortraits/" + path, out meta) ||
                    backupPortraits.TryGetValue(path, out meta)) &&
                    meta is not null)
                {
                    meta.defaultPath = "Portraits/" + path;
                    portraitSizes[path] = meta;
                    return true; //suffix
                }
            }

            //no suffix or suffix not found
            if (portraitSizes.TryGetValue(name, out meta) && meta is not null)
                return true; //cached

            if (failedPaths.Contains(path))
            {
                meta = null;
                return false;
            }

            if ((Utils.TryLoadAsset("Mods/HDPortraits/" + name, out meta) || 
                backupPortraits.TryGetValue(name, out meta)) && 
                meta is not null)
            {
                meta.defaultPath = "Portraits/" + name;
                portraitSizes[name] = meta;
                return true; //base
            }

            monitor.Log($"No Data for {path}");

            failedPaths.Add(path);
            meta = null;
            return false; //not found
        }
    }
}
