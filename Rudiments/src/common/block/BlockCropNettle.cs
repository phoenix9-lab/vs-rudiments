using Rudiments.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// Stinging nettle crop.
    ///
    /// HARVESTING:
    ///   Left-click fast break:
    ///     - Scythe held: no sting, 25% yield penalty.
    ///     - Bare hands / knife: stings unless gloves worn.
    ///   Right-click hold (careful harvest, stage >= 3): slow ~5s, never stings.
    ///
    /// STUB MECHANIC:
    ///   When Config.NettleAlwaysLeaveStub is true, every cut at any stage on any soil
    ///   leaves a BlockNettleStub root crown. The stub regrows to stage-1 automatically,
    ///   or can be dug with a shovel for the rhizome (the only way to permanently remove nettle).
    ///
    /// GROWTH / SPREAD / HEAVY FEEDER (wild): all calendar-driven by BlockEntityNettle, not the
    /// real-time random block-tick path (which is disabled here via ShouldReceiveServerGameTicks).
    /// Cultivated growth is driven by BlockEntityFarmland; cultivated neighbour depletion by
    /// CropBehaviorHeavyFeeder registered in CropProps.
    ///
    /// WALK-THROUGH STING via OnEntityInside. Any armor (protectionModifiers) blocks it.
    /// </summary>
    public class BlockCropNettle : BlockCrop
    {
        private float carefulHarvestTime = 5f;
        private static SimpleParticleProperties leafParticle = new();

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            carefulHarvestTime = Attributes?["carefulHarvestTime"]?.AsFloat(5f) ?? 5f;

            leafParticle = new SimpleParticleProperties(
                1, 3, ColorUtil.ToRgba(220, 60, 140, 50),
                new Vec3d(), new Vec3d(),
                new Vec3f(-0.4f, 0.1f, -0.4f), new Vec3f(0.4f, 0.6f, 0.4f),
                0.8f, 0.5f, 0.15f, 0.35f, EnumParticleModel.Quad);
            leafParticle.WithTerrainCollision = true;
            leafParticle.AddPos.Set(0.5f, 0.25f, 0.5f);
            leafParticle.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.08f);
            leafParticle.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -200);

            // Override own-soil nutrient consumption with config value (default 45).
            if (CropProps != null)
            {
                CropProps.NutrientConsumption = RudimentsModSystem.Config.NettleNutrientConsumption;
            }
        }

        // ── Fast break (left-click) ────────────────────────────────────────────────
        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (byPlayer != null)
            {
                bool holdingScythe = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack?.Collectible?.Code?.Path?.Contains("scythe") == true;
                if (holdingScythe)
                {
                    base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier * 0.75f);
                    TryPlaceStub(world, pos);
                    return;
                }
                NettleSting.TrySting(world, byPlayer, pos);
            }

            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
            TryPlaceStub(world, pos);
        }

        private void TryPlaceStub(IWorldAccessor world, BlockPos pos)
        {
            if (world.Side != EnumAppSide.Server) return;
            if (!RudimentsModSystem.Config.NettleAlwaysLeaveStub) return;

            Block stub = world.GetBlock(new AssetLocation("rudiments:nettlestub"));
            if (stub != null)
                world.BlockAccessor.SetBlock(stub.BlockId, pos);
        }

        // ── Right-click (careful harvest) ─────────────────────────────────────────
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
            => CurrentCropStage >= 3;

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.Side == EnumAppSide.Client && world.Rand.Next(0, 3) == 1)
            {
                leafParticle.MinPos.Set(blockSel.Position.X + 0.1f, blockSel.Position.Y + 0.1f, blockSel.Position.Z + 0.1f);
                api.World.SpawnParticles(leafParticle);
            }

            if (world.Side == EnumAppSide.Server && secondsUsed >= carefulHarvestTime)
            {
                ItemStack[] drops = GetDrops(world, blockSel.Position, byPlayer);
                if (drops != null)
                    foreach (var drop in drops)
                        if (drop?.StackSize > 0)
                            if (!byPlayer.InventoryManager.TryGiveItemstack(drop))
                                world.SpawnItemEntity(drop, blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5));

                world.BlockAccessor.SetBlock(0, blockSel.Position);
                TryPlaceStub(world, blockSel.Position);
                world.PlaySoundAt(new AssetLocation("game:sounds/block/plant"), byPlayer, null, false, 16f, 0.9f);
            }

            return secondsUsed < carefulHarvestTime;
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) { }

        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
            => true;

        // ── Walk-through sting ─────────────────────────────────────────────────────
        public override void OnEntityInside(IWorldAccessor world, Entity entity, BlockPos pos)
        {
            base.OnEntityInside(world, entity, pos);
            if (world.Side != EnumAppSide.Server) return;
            if (!(entity is EntityPlayer ep)) return;
            IPlayer player = world.PlayerByUid(ep.PlayerUID);
            if (player == null) return;
            NettleSting.TryStingWalkthrough(world, player, pos);
        }

        // ── Growth / spread are calendar-driven, not random-tick driven ─────────────
        //
        // Wild nettle growth, heavy-feeder neighbour depletion, and rhizome spread are all driven
        // by BlockEntityNettle off the game calendar (so they respond to time speed and are
        // deterministic). Cultivated growth is driven by BlockEntityFarmland. We therefore disable
        // the inherited real-time random block-tick growth path entirely.
        public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, System.Random offThreadRandom, out object extra)
        {
            extra = null;
            return false;
        }
    }
}
