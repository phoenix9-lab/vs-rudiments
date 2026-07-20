using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Rudiments.SRC.Common.Blocks;
using Rudiments.SRC.Common.BlockEntities;
using Rudiments.SRC.Common.Items;
using Rudiments.Utils;

namespace Rudiments
{
    public class RudimentsModSystem : ModSystem
    {
        public static RudimentsConfig Config { get; private set; } = new();

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            Config = api.LoadModConfig<RudimentsConfig>("rudiments.json") ?? new();
            api.StoreModConfig(Config, "rudiments.json");

            // Mirror the feature flags into the world config so JSON patches can condition on
            // them (patches are server-side; the JsonPatch loader runs after every StartPre).
            if (api.Side == EnumAppSide.Server)
            {
                api.World.Config.SetBool("Rudiments.FlaxBloomHarvest", Config.FlaxBloomHarvest);
                api.World.Config.SetBool("Rudiments.SeedsOnlyWhenMature", Config.SeedsOnlyWhenMature);
            }
        }

        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockRipple", typeof(BlockRipple));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockBreak", typeof(BlockBreak));
            api.RegisterBlockEntityClass($"{Mod.Info.ModID}:BlockEntityBreak", typeof(BlockEntityBreak));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockHatchel", typeof(BlockHatchel));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockScutchBoard", typeof(BlockScutchBoard));
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

            api.RegisterItemClass($"{Mod.Info.ModID}:ItemFieldRettableBundle", typeof(ItemFieldRettableBundle));
            api.RegisterItemClass($"{Mod.Info.ModID}:ItemNettleRhizome", typeof(ItemNettleRhizome));
            api.RegisterItemClass($"{Mod.Info.ModID}:ItemHandCards", typeof(ItemHandCards));

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
                    return TextCommandResult.Success("Rudiments config reloaded.");
                });
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            base.AssetsFinalize(api);

            // Barrel recipes are parsed independently on both client and server from the same
            // assets, so apply the config override on both sides to keep them in sync.
            ApplyBarrelRettingRatio(api);

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
    }
}
