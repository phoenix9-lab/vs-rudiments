using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Rudiments.SRC.Common.Blocks;
using Rudiments.SRC.Common.BlockEntities;
using Rudiments.SRC.Common.Entities;
using Rudiments.SRC.Common.Items;
using Rudiments.Utils;

namespace Rudiments
{
    public class RudimentsModSystem : ModSystem
    {
        public static RudimentsConfig Config { get; private set; } = new();

        /// <summary>World-config key mirroring <see cref="RudimentsConfig.LeadPoisoningEnabled"/>.</summary>
        public const string LeadPoisoningWorldKey = "Rudiments.LeadPoisoning";

        private static ICoreAPI coreApi;

        /// <summary>
        /// Whether lead poisoning is on — <b>as decided by the server</b>, not by whoever is looking.
        ///
        /// Every other setting in this mod is read from the local ModConfig on whichever side is
        /// asking, which is fine when the two sides agree and harmless when they do not. This one is
        /// different, because the burden is server-authoritative and the warnings are client-side: a
        /// client who turned it off in their own config would stop being warned and carry on being
        /// poisoned. So the server mirrors its answer into the world config, which is synced, and the
        /// local value is only the fallback for when there is no world config yet.
        /// </summary>
        public static bool LeadPoisoningEnabled
        {
            get
            {
                bool local = Config.LeadPoisoningEnabled;
                ITreeAttribute worldConfig = coreApi?.World?.Config;
                return worldConfig == null ? local : worldConfig.GetBool(LeadPoisoningWorldKey, local);
            }
        }

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            coreApi = api;
            Config = api.LoadModConfig<RudimentsConfig>("rudiments.json") ?? new();
            api.StoreModConfig(Config, "rudiments.json");

            // The lead mark rides on food and liquid stacks, so it must never be the reason two of
            // them refuse to merge — vanilla's TryPutLiquid returns 0 outright when the contents
            // differ by any attribute it does not know to ignore, which would read as "these two
            // waters will not combine" with nothing on screen to explain it.
            if (!GlobalConstants.IgnoredStackAttributes.Contains(LeadGlaze.MarkKey))
            {
                GlobalConstants.IgnoredStackAttributes = GlobalConstants.IgnoredStackAttributes.Append(LeadGlaze.MarkKey);
            }

            // Mirror the feature flags into the world config so JSON patches can condition on
            // them (patches are server-side; the JsonPatch loader runs after every StartPre).
            if (api.Side == EnumAppSide.Server)
            {
                api.World.Config.SetBool("Rudiments.FlaxBloomHarvest", Config.FlaxBloomHarvest);
                api.World.Config.SetBool("Rudiments.SeedsOnlyWhenMature", Config.SeedsOnlyWhenMature);
                api.World.Config.SetBool(LeadPoisoningWorldKey, Config.LeadPoisoningEnabled);
            }
        }

        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockRipple", typeof(BlockRipple));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockBreak", typeof(BlockBreak));
            api.RegisterBlockEntityClass($"{Mod.Info.ModID}:BlockEntityBreak", typeof(BlockEntityBreak));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockHatchel", typeof(BlockHatchel));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockScutchBoard", typeof(BlockScutchBoard));
            api.RegisterBlockEntityClass($"{Mod.Info.ModID}:BlockEntityScutchBoard", typeof(BlockEntityScutchBoard));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockCropFlax", typeof(BlockCropFlax));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockCropNettle", typeof(BlockCropNettle));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockDeadCrop", typeof(BlockDeadCropRudiments));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockDryingRack", typeof(BlockDryingRack));
            api.RegisterBlockEntityClass($"{Mod.Info.ModID}:BlockEntityDryingRack", typeof(BlockEntityDryingRack));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockFieldRetting", typeof(BlockFieldRetting));
            api.RegisterBlockEntityClass($"{Mod.Info.ModID}:BlockEntityFieldRetting", typeof(BlockEntityFieldRetting));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockStook", typeof(BlockStook));
            api.RegisterBlockEntityClass($"{Mod.Info.ModID}:BlockEntityStook", typeof(BlockEntityStook));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockRettingVat", typeof(BlockRettingVat));
            api.RegisterBlockEntityClass($"{Mod.Info.ModID}:BlockEntityRettingVat", typeof(BlockEntityRettingVat));
            api.RegisterBlockEntityBehaviorClass($"{Mod.Info.ModID}:RettingBath", typeof(BlockEntityBehaviorRettingBath));
            api.RegisterBlockBehaviorClass($"{Mod.Info.ModID}:RettingBathInfo", typeof(BlockBehaviorRettingBathInfo));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockOilPress", typeof(BlockOilPress));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockMechScutcher", typeof(BlockMechScutcher));
            api.RegisterBlockEntityClass($"{Mod.Info.ModID}:BlockEntityMechScutcher", typeof(BlockEntityMechScutcher));

            api.RegisterCollectibleBehaviorClass($"{Mod.Info.ModID}:FiberQuality", typeof(FiberQualityBehavior));
            api.RegisterCollectibleBehaviorClass($"{Mod.Info.ModID}:DurabilityBonus", typeof(DurabilityBonusBehavior));

            // ── Ware tiers and kilns ──
            api.RegisterCollectibleBehaviorClass($"{Mod.Info.ModID}:Fragile", typeof(CollectibleBehaviorFragile));
            api.RegisterCollectibleBehaviorClass($"{Mod.Info.ModID}:Seepage", typeof(CollectibleBehaviorSeepage));
            api.RegisterEntityBehaviorClass($"{Mod.Info.ModID}:ClayFragile", typeof(EntityBehaviorClayFragile));
            api.RegisterBlockBehaviorClass($"{Mod.Info.ModID}:WareTier", typeof(BlockBehaviorWareTier));
            api.RegisterBlockEntityBehaviorClass($"{Mod.Info.ModID}:WareTier", typeof(BlockEntityBehaviorWareTier));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockSeepingContainer", typeof(BlockSeepingContainer));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockWareStorageVessel", typeof(BlockWareStorageVessel));

            // One router class serves both kilns — the updraft kiln's chimney requirement is an
            // ignition condition, and those live on the block entity.
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockSmallBrickKiln", typeof(BlockKiln));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockUpdraftKiln", typeof(BlockKiln));
            api.RegisterBlockEntityClass($"{Mod.Info.ModID}:BlockEntitySmallBrickKiln", typeof(BlockEntitySmallBrickKiln));
            api.RegisterBlockEntityClass($"{Mod.Info.ModID}:BlockEntityUpdraftKiln", typeof(BlockEntityUpdraftKiln));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockGlazableClayware", typeof(BlockGlazableClayware));

            // ── Lead poisoning ──
            // Three subclasses over the vanilla vessels food and drink pass through. Two of them are
            // pure plumbing to stop vanilla's serve from wiping the bowl's ware attributes; the meal
            // bowl is where eating is actually observed.
            api.RegisterEntityBehaviorClass($"{Mod.Info.ModID}:LeadBurden", typeof(EntityBehaviorLeadBurden));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockWareMeal", typeof(BlockWareMeal));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockWarePot", typeof(BlockWarePot));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockWareCrock", typeof(BlockWareCrock));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockWareCookingContainer", typeof(BlockWareCookingContainer));

            api.RegisterBlockClass($"{Mod.Info.ModID}:RudimentsWateringCan", typeof(RudimentsWateringCan));
            api.RegisterCollectibleBehaviorClass($"{Mod.Info.ModID}:GlazeApplicator", typeof(CollectibleBehaviorGlazeApplicator));

            api.RegisterItemClass($"{Mod.Info.ModID}:ItemFieldRettableBundle", typeof(ItemFieldRettableBundle));
            api.RegisterItemClass($"{Mod.Info.ModID}:ItemNettleRhizome", typeof(ItemNettleRhizome));
            api.RegisterItemClass($"{Mod.Info.ModID}:ItemHandCards", typeof(ItemHandCards));
            api.RegisterItemClass($"{Mod.Info.ModID}:ItemScutchSword", typeof(ItemScutchSword));

            api.RegisterBlockBehaviorClass($"{Mod.Info.ModID}:RhizomeSpread", typeof(BlockBehaviorRhizomeSpread));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockNettleStub", typeof(BlockNettleStub));
            api.RegisterCropBehavior("HeavyFeeder", typeof(CropBehaviorHeavyFeeder));
            api.RegisterBlockEntityClass($"{Mod.Info.ModID}:BlockEntityNettle", typeof(BlockEntityNettle));
            api.RegisterBlockEntityClass($"{Mod.Info.ModID}:BlockEntityNettleConvert", typeof(BlockEntityNettleConvert));
            api.RegisterBlockEntityClass($"{Mod.Info.ModID}:BlockEntityReedSpread", typeof(BlockEntityReedSpread));

            base.Start(api);

            api.Logger.Notification("[{0}] v{1} — flax & nettle fibre chains, quality retting, mechanical scutch mill, linseed oil. Based on AgeOfFlax by OppoOtis.", Mod.Info.Name, Mod.Info.Version);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            // Death drops carry the same ByPlayerUid marker as a deliberately thrown item, so drop
            // breakage needs to know who died recently or dying with pottery in your bags would
            // smash all of it.
            RudimentsDeathTracker.Register(api);

            // Re-read ModConfig/rudiments.json into the live Config object so edits (manual or via
            // AutoConfigLib) take effect without a restart. All mod code reads RudimentsModSystem.Config
            // live, so reloading the object is enough.
            api.ChatCommands.Create("rudimentsreload")
                .WithDescription("Reload the Rudiments config from ModConfig/rudiments.json")
                .RequiresPrivilege(Vintagestory.API.Server.Privilege.controlserver)
                .HandleWith(_ =>
                {
                    Config = api.LoadModConfig<RudimentsConfig>("rudiments.json") ?? new RudimentsConfig();
                    api.StoreModConfig(Config, "rudiments.json");

                    // Re-mirror, or turning lead poisoning off would leave every client still warning
                    // about it until the next restart.
                    api.World.Config.SetBool(LeadPoisoningWorldKey, Config.LeadPoisoningEnabled);

                    return TextCommandResult.Success("Rudiments config reloaded.");
                });

            RegisterLeadCommand(api);
        }

        /// <summary>
        /// Read and clear a player's lead burden. There is no GUI for it anywhere and there is not
        /// going to be, so this is the only way to see a number that is otherwise only ever described
        /// in words — and the only way back for a character who accumulated one under a setting the
        /// server has since changed its mind about.
        /// </summary>
        private void RegisterLeadCommand(ICoreServerAPI api)
        {
            api.ChatCommands.Create("rudimentslead")
                .WithDescription("Show your lead burden, or clear one")
                .RequiresPlayer()
                .HandleWith(args => Report(args.Caller.Entity))
                .BeginSubCommand("clear")
                    .WithDescription("Clear a lead burden — yours, or the named player's")
                    .RequiresPrivilege(Privilege.controlserver)
                    .WithArgs(api.ChatCommands.Parsers.OptionalWord("player"))
                    .HandleWith(args =>
                    {
                        string name = args[0] as string;
                        Entity target = args.Caller.Entity;

                        if (name != null)
                        {
                            IServerPlayer found = api.World.AllOnlinePlayers
                                .FirstOrDefault(p => p.PlayerName.Equals(name, System.StringComparison.OrdinalIgnoreCase)) as IServerPlayer;

                            if (found == null) return TextCommandResult.Error($"No online player named '{name}'.");
                            target = found.Entity;
                        }

                        var behavior = target?.GetBehavior<EntityBehaviorLeadBurden>();
                        if (behavior == null) return TextCommandResult.Error("That entity carries no lead burden.");

                        behavior.Clear();
                        return TextCommandResult.Success($"Lead burden cleared for {name ?? args.Caller.GetName()}.");
                    })
                .EndSubCommand();
        }

        private static TextCommandResult Report(Entity entity)
        {
            var behavior = entity?.GetBehavior<EntityBehaviorLeadBurden>();
            if (behavior == null) return TextCommandResult.Error("You carry no lead burden.");

            if (!LeadPoisoningEnabled)
            {
                return TextCommandResult.Success($"Lead poisoning is off on this server. Burden on record: {behavior.Burden:0.#}.");
            }

            return TextCommandResult.Success(
                $"Lead burden {behavior.Burden:0.#} (nothing happens below {Config.LeadOnsetBurden:0.#}). " +
                $"Max health lost: {behavior.Penalty():0.#}. Shedding {Config.LeadDecayPerDay:0.#} per day.");
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            base.AssetsFinalize(api);

            // Barrel and grid recipes are parsed independently on both client and server from the
            // same assets, so apply the config overrides on both sides to keep them in sync.
            ApplyBarrelRettingRatio(api);
            ApplyPorcelainClayRatios(api);
            ApplyLinseedOilYield(api);

            // Itemtypes are server-authoritative and synced to clients, so attribute edits here
            // reach both sides.
            if (api.Side != EnumAppSide.Server) return;
            if (!api.ModLoader.IsModEnabled("wool")) return;

            bool spinningwheelLoaded = api.ModLoader.IsModEnabled("spinningwheel");
            int strippedCount = 0, spinnableCount = 0;

            foreach (Item item in api.World.Items)
            {
                if (item?.Code == null) continue;

                // Washed wool must be carded before it can be spun. Immersive Fibercraft patches
                // spinningProps onto wool:fibers-*; json patch order between unrelated mods is
                // undefined, so the attribute is stripped here, after all patching is done.
                // Fibers get the offhand flag explicitly: carding holds them in the off hand, and
                // Immersive Fibercraft only grants offhand to items it still sees spinningProps on.
                if (item.Code.Domain == "wool" && item.Code.Path.StartsWith("fibers-"))
                {
                    if (item.Attributes?.KeyExists("spinningProps") == true)
                    {
                        (item.Attributes.Token as JObject)?.Remove("spinningProps");
                        strippedCount++;
                    }
                    item.StorageFlags |= EnumItemStorageFlags.Offhand;
                }

                // Rolags become the spinnable stage instead: 2 rolags -> 1 twine on the drop
                // spindle / spinning wheel, mirroring Immersive Fibercraft's raw-fiber ratio.
                if (spinningwheelLoaded && item.Code.Domain == Mod.Info.ModID && item.Code.Path.StartsWith("rolag-"))
                {
                    string color = item.Code.Path.Substring(item.Code.Path.LastIndexOf('-') + 1);
                    if (color == "redbrown" || color == "lightbrown") color = "brown";
                    if (api.World.GetItem(new AssetLocation("wool", "twine-wool-" + color)) == null) continue;
                    if (item.Attributes?.Token is not JObject attributes) continue;

                    attributes["spinningProps"] = new JObject
                    {
                        ["outputType"] = "wool:twine-wool-" + color,
                        ["outputQuantity"] = 1,
                        ["inputQuantity"] = 2,
                        ["spinTime"] = 4
                    };

                    // The drop spindle spins from the off hand. Immersive Fibercraft grants this
                    // flag to spinnable items in its own AssetsFinalize, but if that ran before
                    // ours it never saw the rolags' spinningProps — so set it here as well.
                    item.StorageFlags |= EnumItemStorageFlags.Offhand;
                    spinnableCount++;
                }
            }

            if (strippedCount > 0 || spinnableCount > 0)
            {
                api.Logger.Notification("[{0}] Carding compat: washed wool must now be carded first — removed direct spinnability from {1} fiber items, made {2} rolags spinnable.", Mod.Info.Name, strippedCount, spinnableCount);
            }
        }

        /// <summary>
        /// Overwrites the water/limewater litres on the retting-bath barrel recipes with the
        /// configured ratio, so server owners can tune bundle-per-barrel capacity without
        /// hand-editing assets/rudiments/recipes/barrel/retting-*.json.
        /// </summary>
        private void ApplyBarrelRettingRatio(ICoreAPI api)
        {
            float litresPerBundle = System.Math.Max(0.1f, Config.BarrelRettingLitresPerBundle);

            var recipeSys = api.ModLoader.GetModSystem<RecipeRegistrySystem>();
            if (recipeSys?.BarrelRecipes == null) return;

            int changed = 0;
            foreach (BarrelRecipe recipe in recipeSys.BarrelRecipes)
            {
                if (recipe.Code == null || !recipe.Code.StartsWith("rettingbath-")) continue;

                foreach (BarrelRecipeIngredient ingred in recipe.Ingredients)
                {
                    string path = ingred.Code?.Path;
                    if (path != "waterportion" && path != "limewaterportion") continue;

                    ingred.Litres = litresPerBundle;
                    ingred.ConsumeLitres = litresPerBundle;
                    changed++;
                }
            }

            if (changed > 0)
            {
                api.Logger.Notification("[{0}] Barrel retting: {1} litre(s) of water/limewater per bundle ({2} ingredient(s) adjusted).", Mod.Info.Name, litresPerBundle, changed);
            }
        }

        /// <summary>
        /// Retunes how much blue clay each porcelain route converts, by editing the two loaded grid
        /// recipes rather than making server owners hand-edit
        /// assets/rudiments/recipes/grid/porcelainclay-*.json. Both the clay input and the porcelain
        /// output move together, because the config value means "blue clay converted per unit of
        /// temper" — the temper quantity itself never changes.
        /// </summary>
        private void ApplyPorcelainClayRatios(ICoreAPI api)
        {
            int perQuartz = System.Math.Max(1, Config.PorcelainClayPerQuartz);
            int perFlint = System.Math.Max(1, Config.PorcelainClayPerFlint);
            int changed = 0;

            foreach (GridRecipe recipe in api.World.GridRecipes)
            {
                if (recipe?.Output?.Code?.Path != "clay-porcelain") continue;
                if (recipe.Ingredients == null || !recipe.Ingredients.TryGetValue("C", out var clay)) continue;

                // The two routes differ by which temper they use, and each has its own lever.
                bool quartz = recipe.Ingredients.ContainsKey("Q");
                int perUnit = quartz ? perQuartz : perFlint;

                clay.Quantity = perUnit;
                recipe.Output.StackSize = perUnit;
                changed++;
            }

            if (changed > 0)
            {
                api.Logger.Notification("[{0}] Porcelain clay: {1} blue clay per crushed quartz, {2} per powdered flint + bonemeal ({3} recipe(s) adjusted).", Mod.Info.Name, perQuartz, perFlint, changed);
            }
        }

        /// <summary>
        /// Overwrites the litresPerItem the vanilla fruit press extracts per flax seed, so server
        /// owners can tune oil yield without hand-editing
        /// assets/rudiments/patches/linseedoil-fruitpress.json. The pressed byproduct
        /// (rudiments:linseedcake) is left alone — the fruit press produces it deterministically,
        /// with no chance/quantity field of its own to tune.
        /// </summary>
        private void ApplyLinseedOilYield(ICoreAPI api)
        {
            float litresPerSeed = System.Math.Max(0.001f, Config.LinseedOilLitresPerSeed);

            Item flaxSeeds = api.World.GetItem(new AssetLocation("game:seeds-flax"));
            if (flaxSeeds?.Attributes?.Token is not JObject attrs) return;
            if (attrs["juiceableProperties"] is not JObject props) return;

            props["litresPerItem"] = litresPerSeed;
            api.Logger.Notification("[{0}] Linseed oil: {1} litre(s) of oilportion-flax per seed pressed.", Mod.Info.Name, litresPerSeed);
        }
    }
}
