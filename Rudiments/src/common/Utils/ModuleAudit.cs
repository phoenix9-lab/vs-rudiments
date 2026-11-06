using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Rudiments.Utils
{
    /// <summary>
    /// <c>/rudimentsmodules audit</c>: what of a switched-off group is still live.
    /// Reports visible collectibles, surviving recipes, and Rudiments classes or behaviours still
    /// attached to anything. With every group off it should list nothing but always-on registrations.
    /// </summary>
    public static class ModuleAudit
    {
        // Class / behaviour name (without the "rudiments:" prefix) -> the switch that owns it.
        private static readonly Dictionary<string, Func<bool>> Owner = Build();

        private static Dictionary<string, Func<bool>> Build()
        {
            var map = new Dictionary<string, Func<bool>>();
            void Add(Func<bool> on, params string[] names) { foreach (string n in names) map[n] = on; }

            Add(() => RudimentsModules.Fibre,
                "BlockRipple", "BlockBreak", "BlockEntityBreak", "BlockHatchel", "BlockScutchBoard", "BlockEntityScutchBoard",
                "BlockCropFlax", "BlockDryingRack", "BlockEntityDryingRack", "BlockFieldRetting", "BlockEntityFieldRetting",
                "BlockStook", "BlockEntityStook", "BlockRettingVat", "BlockEntityRettingVat", "RettingBath", "RettingBathInfo",
                "BlockMechScutcher", "BlockEntityMechScutcher", "FiberQuality", "DurabilityBonus",
                "ItemFieldRettableBundle", "ItemScutchSword");
            Add(() => RudimentsModules.Nettle, "BlockCropNettle", "BlockNettleStub", "ItemNettleRhizome", "BlockEntityNettle", "BlockEntityNettleConvert");
            Add(() => RudimentsModules.WoolCarding, "ItemHandCards");
            // BlockEntityReedSpread stays attached with the switch off (it just idles), so only the behaviour counts.
            Add(() => RudimentsModules.ReedSpread, "RhizomeSpread");
            Add(() => RudimentsModules.Kilns, "BlockSmallBrickKiln", "BlockUpdraftKiln", "BlockEntitySmallBrickKiln", "BlockEntityUpdraftKiln");
            Add(() => RudimentsModules.Ware, "Fragile", "WareTier", "BlockSeepingContainer", "BlockWareStorageVessel",
                "BlockWareMeal", "BlockWarePot", "BlockWareCrock", "BlockWareCookingContainer", "RudimentsWateringCan");
            Add(() => RudimentsModules.ClayBreakage, "ClayFragile", "GroundStorageWear", "FirepitWear", "SmeltingFailure", "BlockIngotMoldGated");
            Add(() => RudimentsModules.ClaySeepage, "Seepage");
            Add(() => RudimentsModules.Glazes, "BlockGlazableClayware", "GlazeApplicator", "LeadBurden");
            return map;
        }

        public static string Run(ICoreServerAPI api)
        {
            var sb = new StringBuilder();
            sb.AppendLine(RudimentsModules.Describe());

            var live = new SortedDictionary<string, List<string>>();
            void Note(string what, string who)
            {
                if (!live.TryGetValue(what, out var list)) live[what] = list = new List<string>();
                list.Add(who);
            }

            foreach (CollectibleObject coll in api.World.Collectibles)
            {
                if (coll?.Code == null) continue;
                string who = coll.Code.ToShortString();

                string group = ModuleContentSystem.GroupOf(coll);
                bool visible = coll.CreativeInventoryTabs?.Length > 0 || coll.CreativeInventoryStacks?.Length > 0;
                if (group != null && !ModuleContentSystem.IsOn(group) && visible) Note($"visible {group} content", who);

                // A switched-off group's own blocks keep their classes on purpose (they stay registered
                // so saves still load); only hooks on someone else's types mean something is still live.
                if (coll.Code.Domain == "rudiments") continue;

                // Compare registered names ("rudiments:Seepage"), never C# type names
                // (CollectibleBehaviorSeepage): several types are registered under another name.
                var classes = api.ClassRegistry;
                Check(Strip(coll is Block bl ? bl.Class : (coll as Item)?.Class), who, Note);
                foreach (var b in coll.CollectibleBehaviors) Check(Strip(classes.GetCollectibleBehaviorClassName(b.GetType())), who, Note);

                if (coll is Block block)
                {
                    foreach (var b in block.BlockBehaviors) Check(Strip(classes.GetBlockBehaviorClassName(b.GetType())), who, Note);
                    foreach (var b in block.BlockEntityBehaviors) Check(Strip(b.Name), who, Note);
                    if (block.EntityClass != null) Check(Strip(block.EntityClass), who, Note);
                }
            }

            foreach (EntityProperties type in api.World.EntityTypes)
            {
                var behaviors = type.Server?.BehaviorsAsJsonObj;
                if (behaviors == null) continue;
                foreach (var b in behaviors) Check(Strip(b["code"].AsString("")), type.Code.ToShortString(), Note);
            }

            int recipes = 0;
            foreach (var r in api.World.GridRecipes) if (r != null && ModuleContentSystem.Owned(r)) recipes++;
            var reg = api.ModLoader.GetModSystem<RecipeRegistrySystem>();
            if (reg != null)
            {
                recipes += reg.BarrelRecipes.Count(r => r != null && ModuleContentSystem.Owned(r));
                recipes += reg.ClayFormingRecipes.Count(r => r != null && ModuleContentSystem.Owned(r));
            }
            if (recipes > 0) Note("recipes of switched-off groups", $"{recipes} recipe(s)");

            if (live.Count == 0)
            {
                sb.AppendLine("Nothing of a switched-off group is live.");
            }
            foreach (var pair in live)
            {
                sb.AppendLine($"{pair.Key}: {pair.Value.Count} — {string.Join(", ", pair.Value.Distinct().Take(6))}{(pair.Value.Count > 6 ? ", ..." : "")}");
            }
            return sb.ToString().TrimEnd();
        }

        private static string Strip(string name) => name != null && name.StartsWith("rudiments:") ? name.Substring(10) : name;

        private static void Check(string name, string who, Action<string, string> note)
        {
            if (name != null && Owner.TryGetValue(name, out var on) && !on()) note($"live {name}", who);
        }
    }
}
