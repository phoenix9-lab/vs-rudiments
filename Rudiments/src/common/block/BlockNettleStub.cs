using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// The root crown left behind after a mature (stage 8-9) wild nettle is cut.
    /// Automatically regrows to stage-1 nettle after a few in-game days.
    /// Right-clicking with a shovel (~1.5s) extracts the rhizome and removes the stub entirely.
    /// This mirrors how coopersreed works: the harvested base persists and can be dug for the root.
    /// </summary>
    public class BlockNettleStub : Block
    {
        static SimpleParticleProperties dirtParticle = new();

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            dirtParticle = new SimpleParticleProperties(
                2, 6, ColorUtil.ToRgba(200, 120, 80, 55),
                new Vec3d(), new Vec3d(),
                new Vec3f(-0.5f, 0.1f, -0.5f), new Vec3f(0.5f, 0.4f, 0.5f),
                0.6f, 1f, 0.1f, 0.3f, EnumParticleModel.Cube);
            dirtParticle.WithTerrainCollision = true;
            dirtParticle.AddPos.Set(0.5f, 0, 0.5f);
        }

        public override bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref string failureCode)
        {
            Block below = world.BlockAccessor.GetBlock(blockSel.Position.DownCopy());
            if (!below.SideSolid[BlockFacing.UP.Index])
            {
                failureCode = "requiresolidground";
                return false;
            }
            return base.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode);
        }

        // Regrow into stage-1 nettle is calendar-driven by BlockEntityNettleConvert (entityClass in
        // nettlestub.json), so it responds to time speed and is deterministic.

        // Shovel right-click: dig out the rhizome, remove stub.
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            return byPlayer?.InventoryManager?.ActiveHotbarSlot?.Itemstack?.Collectible?.Tool == EnumTool.Shovel;
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.Side == EnumAppSide.Client && world.Rand.Next(0, 3) == 1)
            {
                dirtParticle.MinPos.Set(blockSel.Position.X + 0.1f, blockSel.Position.Y, blockSel.Position.Z + 0.1f);
                api.World.SpawnParticles(dirtParticle);
            }

            if (world.Side == EnumAppSide.Server && secondsUsed >= 1.5f)
            {
                Item rhizome = world.GetItem(new AssetLocation("rudiments:nettlerhizome"));
                if (rhizome != null)
                {
                    ItemStack stack = new ItemStack(rhizome, 1);
                    if (!byPlayer.InventoryManager.TryGiveItemstack(stack))
                        world.SpawnItemEntity(stack, blockSel.Position.ToVec3d().Add(0.5, 0.2, 0.5));
                }
                world.BlockAccessor.SetBlock(0, blockSel.Position);
                world.PlaySoundAt(new AssetLocation("game:sounds/block/soil"), byPlayer, null, false, 16f, 0.9f);
            }

            return secondsUsed < 1.5f;
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) { }

        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
            => true;
    }
}
