using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using System;

namespace HDPortraits.Models
{
    public class MetadataModel
    {
        public int Size { set; get; } = 64;
        public AnimationModel Animation { get; set; } = null;
        public string Portrait { 
            get => portraitPath; 
            set {
                portraitPath = value;
                Reload();
            }
        }
        public readonly RLazy<Texture2D> overrideTexture;

        internal string defaultPath = null;
        private Texture2D savedDefault = null;
        private string portraitPath = null;

        public MetadataModel()
        {
            overrideTexture = new(GetPortrait);
        }

        public void Reload() => overrideTexture.Reset();

        public Texture2D GetPortrait()
        {
            Animation?.Reset();

            if (portraitPath is not null)
                if(Utils.TryLoadAsset(portraitPath, out Texture2D texture))
                    return texture;
                else
                    ModEntry.monitor.Log($"Could not find image at game asset path: '{portraitPath}'.", LogLevel.Warn);

            if (defaultPath is not null)
                if (Utils.TryLoadAsset(defaultPath, out Texture2D texture))
                    return texture;
                else
                    ModEntry.monitor.Log($"Could not find default asset at path: '{defaultPath}'! An NPC is missing their portrait!", LogLevel.Error);

            return null;
        }
        public Texture2D GetDefault() => savedDefault;
    }
}
