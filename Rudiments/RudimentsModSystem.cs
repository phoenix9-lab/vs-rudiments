using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
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
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockDryingRack", typeof(BlockDryingRack));
            api.RegisterBlockEntityClass($"{Mod.Info.ModID}:BlockEntityDryingRack", typeof(BlockEntityDryingRack));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockFieldRetting", typeof(BlockFieldRetting));
            api.RegisterBlockEntityClass($"{Mod.Info.ModID}:BlockEntityFieldRetting", typeof(BlockEntityFieldRetting));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockStook", typeof(BlockStook));
            api.RegisterBlockEntityClass($"{Mod.Info.ModID}:BlockEntityStook", typeof(BlockEntityStook));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockRettingVat", typeof(BlockRettingVat));
            api.RegisterBlockEntityClass($"{Mod.Info.ModID}:BlockEntityRettingVat", typeof(BlockEntityRettingVat));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockOilPress", typeof(BlockOilPress));
            api.RegisterBlockClass($"{Mod.Info.ModID}:BlockMechScutcher", typeof(BlockMechScutcher));
            api.RegisterBlockEntityClass($"{Mod.Info.ModID}:BlockEntityMechScutcher", typeof(BlockEntityMechScutcher));

            api.RegisterCollectibleBehaviorClass($"{Mod.Info.ModID}:FiberQuality", typeof(FiberQualityBehavior));
            api.RegisterCollectibleBehaviorClass($"{Mod.Info.ModID}:DurabilityBonus", typeof(DurabilityBonusBehavior));
            api.RegisterCollectibleBehaviorClass($"{Mod.Info.ModID}:ToolBinding", typeof(ToolBindingBehavior));

            api.RegisterItemClass($"{Mod.Info.ModID}:ItemFieldRettableBundle", typeof(ItemFieldRettableBundle));
            api.RegisterItemClass($"{Mod.Info.ModID}:ItemNettleRhizome", typeof(ItemNettleRhizome));

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
    }
}
