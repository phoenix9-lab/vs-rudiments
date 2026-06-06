using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Rudiments.Utils
{
    /// <summary>
    /// Adds alternative tool-binding crafting paths (friction-fit / glue / nail / glue+nail) and wires
    /// the <see cref="ToolBindingBehavior"/> onto every tool.
    ///
    /// Runs at ExecuteOrder 1.02 — just after ToolsRequireRope (1.01) — so the tool grid recipes it
    /// reads already have rope injected (when that mod is present), and any recipe we register is never
    /// seen by it. Coexists cleanly whether the fork, the original, or neither rope mod is installed.
    /// </summary>
    public class ToolBindingSystem : ModSystem
    {
        public override double ExecuteOrder() => 1.02;

        // Recipe derivation is server-authoritative; behaviour injection happens on both sides.
        public override void AssetsLoaded(ICoreAPI api)
        {
            if (api is ICoreServerAPI sapi) DeriveRecipes(sapi);
        }

        // Server: collectibles are populated by AssetsFinalize, so inject here.
        public override void AssetsFinalize(ICoreAPI api)
        {
            if (api.Side != EnumAppSide.Server) return;
            int n = InjectInto(api, api.World.Items) + InjectInto(api, api.World.Blocks);
            api.Logger.Notification("[rudiments] tool-binding behaviour added to {0} tools", n);
        }

        // Client: AssetsFinalize runs before the client has populated Items/Blocks (they fill during
        // the texture-atlas build). BlockTexturesLoaded fires once collectibles AND textures are ready.
        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            // In singleplayer the client shares the server's tool objects (already injected), so this
            // adds 0; in true multiplayer the client has its own objects and this attaches the behaviour.
            api.Event.BlockTexturesLoaded += () =>
            {
                int n = InjectInto(api, api.World.Items) + InjectInto(api, api.World.Blocks);
                if (n > 0) api.Logger.Notification("[rudiments] (client) tool-binding behaviour added to {0} tools", n);
            };
        }

        private static int InjectInto<T>(ICoreAPI api, IEnumerable<T> objs) where T : CollectibleObject
        {
            if (objs == null) return 0;
            int n = 0;
            foreach (CollectibleObject obj in objs)
            {
                if (obj?.Tool == null) continue;
                if (obj.CollectibleBehaviors != null && obj.CollectibleBehaviors.Any(b => b is ToolBindingBehavior)) continue;
                var beh = new ToolBindingBehavior(obj);
                beh.OnLoaded(api);
                obj.CollectibleBehaviors = (obj.CollectibleBehaviors ?? Array.Empty<CollectibleBehavior>()).Append(beh).ToArray();
                n++;
            }
            return n;
        }

        // ── Recipe derivation ──────────────────────────────────────────────────────────

        private void DeriveRecipes(ICoreServerAPI api)
        {
            var cfg = RudimentsModSystem.Config;
            bool ropeMod = api.ModLoader.IsModEnabled("toolsrequireropeupdate")
                        || api.ModLoader.IsModEnabled("toolsrequirerope");

            var bases = api.World.GridRecipes
                .Where(r => r?.Output?.ResolvedItemStack?.Collectible?.Tool != null)
                .ToList();

            int added = 0;
            foreach (GridRecipe baseRecipe in bases)
            {
                try
                {
                    string bindingKey = FindBindingKey(baseRecipe, cfg.RopeBindingCodes);
                    string outPath = baseRecipe.Output.ResolvedItemStack.Collectible.Code.Path;
                    bool stone = cfg.FrictionStoneMaterials.Any(m => outPath.Contains(m));

                    foreach (string container in cfg.GlueContainers)
                    {
                        added += TryDerive(api, baseRecipe, "glue", cfg, bindingKey, container);
                        added += TryDerive(api, baseRecipe, "gluenail", cfg, bindingKey, container);
                    }
                    added += TryDerive(api, baseRecipe, "nail", cfg, bindingKey, null);

                    if (stone && bindingKey != null && (!cfg.FrictionRequiresRopeMod || ropeMod))
                    {
                        added += TryDerive(api, baseRecipe, "frictionfit", cfg, bindingKey, null);
                    }
                }
                catch (Exception e)
                {
                    api.Logger.Warning("[rudiments] could not derive binding recipes for {0}: {1}",
                        baseRecipe?.Output?.ResolvedItemStack?.Collectible?.Code, e.Message);
                }
            }

            api.Logger.Notification("[rudiments] derived {0} tool-binding recipes (ToolsRequireRope={1})", added, ropeMod);
        }

        private int TryDerive(ICoreServerAPI api, GridRecipe baseRecipe, string method, RudimentsConfig cfg, string bindingKey, string glueContainer)
        {
            GridRecipe r = baseRecipe.Clone();

            switch (method)
            {
                case "nail":
                    if (!SetOrAddBinding(r, bindingKey, MakeNail(cfg))) return 0;
                    break;
                case "glue":
                    if (!SetOrAddBinding(r, bindingKey, MakeGlue(cfg, glueContainer))) return 0;
                    break;
                case "gluenail":
                    if (!SetOrAddBinding(r, bindingKey, MakeNail(cfg))) return 0;
                    if (!AddBinding(r, MakeGlue(cfg, glueContainer))) return 0;
                    r.Shapeless = true;
                    break;
                case "frictionfit":
                    if (bindingKey == null || !RemoveBinding(r, bindingKey)) return 0;
                    break;
                default:
                    return 0;
            }

            string headCode = method == "frictionfit" ? FindHeadCode(baseRecipe) : null;
            ApplyOutputAttributes(r, method, DurMul(cfg, method), headCode);

            string suffix = glueContainer != null ? "-" + SafeName(glueContainer) : "";
            r.Name = new AssetLocation("rudiments",
                $"binding/{method}/{SafeName(baseRecipe.Output.ResolvedItemStack.Collectible.Code.ToString())}{suffix}");

            r.ResolvedIngredients = null; // force rebuild from the mutated pattern + ingredients
            if (!r.Resolve(api.World, "rudiments:toolbinding")) return 0;

            api.RegisterCraftingRecipe(r);
            return 1;
        }

        // ── Ingredient builders ─────────────────────────────────────────────────────────

        private static CraftingRecipeIngredient MakeNail(RudimentsConfig cfg) => new()
        {
            Type = EnumItemClass.Item,
            Code = new AssetLocation(cfg.NailIngredient),
            Quantity = cfg.NailQuantity
        };

        private static CraftingRecipeIngredient MakeGlue(RudimentsConfig cfg, string container) => new()
        {
            Type = EnumItemClass.Block,
            Code = new AssetLocation(container),
            Quantity = 1,
            RecipeAttributes = new JsonObject(JObject.Parse(
                "{\"requiresContent\":{\"type\":\"item\",\"code\":\"" + cfg.GlueLiquidContent + "\"}," +
                "\"requiresLitres\":" + cfg.GlueLitres.ToString(CultureInfo.InvariantCulture) + "}"))
        };

        // ── Grid pattern surgery (mirrors ToolsRequireRope's expansion, comma-safe) ──────

        private static bool SetOrAddBinding(GridRecipe r, string key, CraftingRecipeIngredient ingred)
        {
            if (key != null && r.Ingredients.ContainsKey(key))
            {
                r.Ingredients[key] = ingred;
                return true;
            }
            return AddBinding(r, ingred);
        }

        private static bool AddBinding(GridRecipe r, CraftingRecipeIngredient ingred)
        {
            char letter = FindAvailableLetter(r);
            string chars = r.IngredientPattern.Replace(",", "").Replace("_", "");
            switch (chars.Length)
            {
                case 1:
                    r.IngredientPattern = $"{chars[0]}{letter}"; r.Width = 1; r.Height = 2; break;
                case 2:
                    r.IngredientPattern = $"{chars[0]}{chars[1]}{letter}"; r.Width = 1; r.Height = 3; break;
                case 3:
                    r.IngredientPattern = $"{chars[0]}{chars[1]},{chars[2]}{letter}"; r.Width = 2; r.Height = 2; break;
                default:
                    return false; // no room in a 2x2 grid
            }
            r.Ingredients[letter.ToString()] = ingred;
            return true;
        }

        private static bool RemoveBinding(GridRecipe r, string key)
        {
            if (!r.Ingredients.ContainsKey(key)) return false;
            r.Ingredients.Remove(key);
            // SHAPELESS rebuild from the remaining ingredients (head + handle). The base recipe we clone
            // is the already-resolved per-variant recipe, so the output is concrete (e.g. axe-flint) —
            // shapeless does NOT break output linkage. A shaped recipe previews (loose Matches) but fails
            // on take, because ConsumeInput strictly position-matches the rebuilt ResolvedIngredients.
            var keys = r.Ingredients.Keys.ToList();
            if (keys.Count == 0) return false;

            // Count occurrences of each remaining letter in the old pattern so the shapeless recipe
            // still requires the correct total quantity (e.g. two sticks that each appeared once).
            string flat = r.IngredientPattern.Replace(",", "").Replace("_", "");
            foreach (string k in keys)
            {
                int count = flat.Count(c => c == k[0]);
                if (count > 0) r.Ingredients[k].Quantity *= count;
            }

            r.IngredientPattern = string.Concat(keys);
            r.Width = keys.Count;
            r.Height = 1;
            r.Shapeless = true;
            return true;
        }

        private static char FindAvailableLetter(GridRecipe r)
        {
            for (char l = 'A'; l <= 'Z'; l++)
            {
                if (!r.IngredientPattern.Contains(l) && !r.Ingredients.ContainsKey(l.ToString())) return l;
            }
            return 'Z';
        }

        // ── Identification helpers ───────────────────────────────────────────────────────

        private static string FindBindingKey(GridRecipe r, string[] ropeCodes)
        {
            foreach (var kv in r.Ingredients)
            {
                string p = (kv.Value?.ResolvedItemStack?.Collectible?.Code ?? kv.Value?.Code)?.Path;
                if (p != null && ropeCodes.Any(c => p.Contains(c))) return kv.Key;
            }
            return null;
        }

        private static string FindHeadCode(GridRecipe r)
        {
            foreach (var kv in r.Ingredients)
            {
                AssetLocation code = kv.Value?.ResolvedItemStack?.Collectible?.Code ?? kv.Value?.Code;
                string p = code?.Path;
                if (p != null && (p.Contains("head") || p.Contains("blade"))) return code.ToString();
            }
            return null;
        }

        private static float DurMul(RudimentsConfig cfg, string method) => method switch
        {
            "frictionfit" => cfg.FrictionDurabilityMul,
            "glue" => cfg.GlueDurabilityMul,
            "nail" => cfg.NailDurabilityMul,
            "gluenail" => cfg.GlueNailDurabilityMul,
            _ => 1f
        };

        private static void ApplyOutputAttributes(GridRecipe r, string method, float mul, string headCode)
        {
            var jo = new JObject
            {
                ["bindingMethod"] = method,
                ["durabilityMul"] = mul
            };
            if (headCode != null) jo["frictionHead"] = headCode;
            r.Output.Attributes = new JsonObject(jo);
        }

        private static string SafeName(string s)
        {
            return new string(s.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
        }
    }
}
