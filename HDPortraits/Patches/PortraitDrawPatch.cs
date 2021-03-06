using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Reflection.Emit;
using HDPortraits.Models;

namespace HDPortraits.Patches
{
    [HarmonyPatch]
    class PortraitDrawPatch
    {
        private static ILHelper patcher = SetupPatch();
        internal static readonly PerScreen<HashSet<MetadataModel>> lastLoaded = new(() => new());
        internal static readonly PerScreen<MetadataModel> currentMeta = new();
        internal static readonly PerScreen<string> overrideName = new();
        internal static readonly PerScreen<Dictionary<NPC, string>> NpcEventSuffixes = new(() => new());
        internal static FieldInfo islandwear = typeof(NPC).FieldNamed("isWearingIslandAttire");

        [HarmonyPatch(typeof(Event), "command_changePortrait")]
        [HarmonyPostfix]
        public static void changeActivePortraitOf(string[] split, Event __instance)
        {
            NPC n = __instance.getActorByName(split[1]) ?? Game1.getCharacterFromName(split[1]);
            NpcEventSuffixes.Value[n] = split[2];
            if (Game1.activeClickableMenu is DialogueBox db && db.characterDialogue?.speaker == n)
            {
                DialoguePatch.Finish();
                DialoguePatch.Init(db);
            }
        }

        [HarmonyPatch(typeof(DialogueBox), "drawPortrait")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => patcher.Run(instructions);
        public static ILHelper SetupPatch()
        {
            return new ILHelper("Dialogue Patch")
                .SkipTo(new CodeInstruction[]
                {
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, typeof(DialogueBox).FieldNamed("characterDialogue")),
                    new(OpCodes.Ldfld, typeof(Dialogue).FieldNamed("speaker")),
                    new(OpCodes.Callvirt,typeof(NPC).MethodNamed("get_Portrait"))
                })
                .Add(new CodeInstruction(OpCodes.Call, typeof(PortraitDrawPatch).MethodNamed("SwapTexture")))
                .SkipTo(new CodeInstruction[]
                {
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, typeof(DialogueBox).FieldNamed("characterDialogue")),
                    new(OpCodes.Callvirt, typeof(Dialogue).MethodNamed("getPortraitIndex"))
                })
                .Remove(new CodeInstruction[]
                {
                    new(OpCodes.Ldc_I4_S, 64),
                    new(OpCodes.Ldc_I4_S, 64),
                    new(OpCodes.Call, typeof(Game1).MethodNamed("getSourceRectForStandardTileSheet"))
                })
                .Add(new CodeInstruction(OpCodes.Call,typeof(PortraitDrawPatch).MethodNamed("GetData")))
                .SkipTo(new CodeInstruction[]
                {
                    new(OpCodes.Call, typeof(Color).MethodNamed("get_White")),
                    new(OpCodes.Ldc_R4, 0f),
                    new(OpCodes.Call, typeof(Vector2).MethodNamed("get_Zero"))
                })
                .Remove()
                .Add(new CodeInstruction[]{
                    new(OpCodes.Call,typeof(PortraitDrawPatch).MethodNamed("GetScale"))
                })
                .Finish();
        }
        public static Texture2D SwapTexture(Texture2D texture) => currentMeta.Value?.overrideTexture.Value ?? texture;
        public static Rectangle GetData(Texture2D texture, int index)
        {
            int asize = currentMeta.Value?.Size ?? 64;
            Rectangle ret = (currentMeta.Value?.Animation != null) ?
                currentMeta.Value.Animation.GetSourceRegion(texture, asize, index, Game1.currentGameTime.ElapsedGameTime.Milliseconds) :
                Game1.getSourceRectForStandardTileSheet(texture, index, asize, asize);
            if (!texture.Bounds.Contains(ret))
                ret = new(0, 0, asize, asize);
            return ret;
        }
        public static string GetSuffix(NPC npc)
        {
            return NpcEventSuffixes.Value.TryGetValue(npc, out string s) ? s : 
                (bool)islandwear.GetValue(npc) ? "Beach" : 
                npc.uniquePortraitActive ? npc.currentLocation.Name : null;
        }
        public static float GetScale() => currentMeta.Value is not null ? 256f / currentMeta.Value.Size : 4f;
    }
}
