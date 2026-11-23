using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Rudiments.Utils
{
    /// <summary>
    /// Hides the content of a switched-off group and removes its recipes.
    ///
    /// <para>The blocks and items stay registered, so a save that already holds them keeps loading
    /// and switching the group back on restores everything; they just lose their creative tab and
    /// handbook page. Recipes that make or use them are dropped (by <see cref="ModuleRecipeRemoval"/>)
    /// so nothing new can be crafted.</para>
    ///
    /// <para>Which group owns a collectible is decided by <see cref="GroupOf"/>, and a
    /// <c>rudiments:</c> collectible it cannot place is reported, so new content cannot ship
    /// unclassified.</para>
    /// </summary>
    public class ModuleContentSystem : ModSystem
    {
        public const string Fibre = "Fibre", Nettle = "Nettle", WoolCarding = "WoolCarding",
            Kilns = "Kilns", Porcelain = "Porcelain", ClayBreakage = "ClayBreakage";

        private static readonly (string Group, string[] Patterns)[] RudimentsOwned =
        {
            (Fibre, new[] { "break-*", "dryingrack-*", "fieldretting-*", "hatchel-*", "mechscutcher-*",
                "rettingvat", "ripple-*", "scutchboard-*", "stook", "scutchsword", "hewnscutchsword",
                "flaxbundle-*", "coarsefibers", "finefibers", "finecord", "linseedcake",
                "rettingbathportion" }),
            (Nettle, new[] { "seeds-nettle", "crop-nettle-*", "nettlestub", "nettlecreep", "nettleleaves",
                "cookednettlerhizome", "nettlebundle-*", "nettlefiber", "nettlerhizome" }),
            (WoolCarding, new[] { "handcards", "rolag-*" }),
            (Kilns, new[] { "smallbrickkiln-*", "updraftkiln-*", "updraftkilnchimney" }),
            (Porcelain, new[] { "*-porcelain-wet" }),
        };

        public static bool IsOn(string group) => RudimentsModules.IsOn(group) ?? true;

        /// <summary>True if any content group is off, i.e. there is anything to hide or remove.</summary>
        public static bool AnyOff => new[] { Fibre, Nettle, WoolCarding, Kilns, Porcelain, ClayBreakage }.Any(g => !IsOn(g));

        /// <summary>The group that owns a collectible, or null if it belongs to none.</summary>
        public static string GroupOf(CollectibleObject coll)
        {
            AssetLocation code = coll?.Code;
            if (code == null) return null;

            if (code.Domain == "rudiments")
            {
                foreach (var (group, patterns) in RudimentsOwned)
                {
                    if (patterns.Any(p => WildcardUtil.Match(p, code.Path))) return group;
                }
            }
            if (WareTier.IsPorcelain(coll)) return Porcelain;
            if (code.Domain == "game" && WildcardUtil.Match("crucible-*-failed", code.Path)) return ClayBreakage;
            return null;
        }

        public override double ExecuteOrder() => 10;

        public override void AssetsFinalize(ICoreAPI api)
        {
            base.AssetsFinalize(api);
            if (api.Side != EnumAppSide.Server) return;

            var hidden = new Dictionary<string, int>();
            var unclassified = new List<string>();

            foreach (CollectibleObject coll in api.World.Collectibles)
            {
                if (coll?.Code == null) continue;

                string group = GroupOf(coll);
                if (group == null)
                {
                    if (coll.Code.Domain == "rudiments") unclassified.Add(coll.Code.ToShortString());
                    continue;
                }
                if (IsOn(group)) continue;

                Hide(coll);
                hidden[group] = hidden.GetValueOrDefault(group) + 1;
            }

            foreach (var pair in hidden.OrderBy(p => p.Key))
            {
                api.Logger.Notification("[{0}] Modules: {1} is off — hid {2} collectible(s).", Mod.Info.Name, pair.Key, pair.Value);
            }
            if (unclassified.Count > 0)
            {
                api.Logger.Warning("[{0}] Modules: {1} collectible(s) belong to no module group and cannot be switched off: {2}",
                    Mod.Info.Name, unclassified.Count, string.Join(", ", unclassified.Take(10)));
            }
        }

        private static void Hide(CollectibleObject coll)
        {
            coll.CreativeInventoryTabs = Array.Empty<string>();
            coll.CreativeInventoryStacks = Array.Empty<CreativeTabAndStackList>();

            // A few types ask for a handbook page explicitly; exclude beats include.
            if (coll.Attributes?.Token is JObject attrs)
            {
                if (attrs["handbook"] is not JObject handbook) attrs["handbook"] = handbook = new JObject();
                handbook["exclude"] = true;
            }
        }

        /// <summary>True if a recipe makes or uses content of a switched-off group.</summary>
        public static bool Owned(IRecipeBase recipe)
        {
            try
            {
                string outGroup = GroupOf(recipe.RecipeOutput?.ResolvedItemStack?.Collectible);
                if (outGroup != null && !IsOn(outGroup)) return true;

                foreach (IRecipeIngredient ing in recipe.RecipeIngredients)
                {
                    AssetLocation code = ing?.Code;
                    if (code == null || code.Domain != "rudiments") continue;
                    foreach (var (group, patterns) in RudimentsOwned)
                    {
                        if (IsOn(group)) continue;
                        if (patterns.Any(p => WildcardUtil.Match(p, code.Path) || WildcardUtil.Match(code.Path, p))) return true;
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // A recipe with no output or ingredients specified is not ours to judge.
            }
            return false;
        }
    }

    /// <summary>
    /// Removes the recipes of switched-off groups, after every other mod has registered its own.
    ///
    /// <para>VS numbers clayforming recipes as <c>Count + 1</c> when they are registered, so removing
    /// any earlier would hand a later recipe the id of one that is still there. Removing last leaves
    /// a gap instead, which is harmless: ids are sent to clients as they are, not renumbered.</para>
    /// </summary>
    public class ModuleRecipeRemoval : ModSystem
    {
        // Registration is only allowed below 99999, so nothing can be added after this runs.
        public override double ExecuteOrder() => 99999;

        public override void AssetsFinalize(ICoreAPI api)
        {
            base.AssetsFinalize(api);
            if (!ModuleContentSystem.AnyOff) return;

            // Recipes are parsed on the server and handed to clients, but the grid and barrel
            // lists are also edited on both sides elsewhere in this mod, so do the same here.
            int removed = api.World.GridRecipes.RemoveAll(r => r != null && ModuleContentSystem.Owned(r));
            var registry = api.ModLoader.GetModSystem<RecipeRegistrySystem>();
            if (registry != null)
            {
                removed += registry.BarrelRecipes?.RemoveAll(r => r != null && ModuleContentSystem.Owned(r)) ?? 0;
                removed += registry.ClayFormingRecipes?.RemoveAll(r => r != null && ModuleContentSystem.Owned(r)) ?? 0;
            }
            if (removed > 0) api.Logger.Notification("[{0}] Modules: removed {1} recipe(s) of switched-off groups.", Mod.Info.Name, removed);
        }
    }
}
