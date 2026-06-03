using Rudiments.SRC.Common.BlockEntities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// Mechanically-powered scutching mill. Drive it with an axle from above or below; once turning it
    /// automatically breaks and hatchels dried fiber bundles into fibre, collapsing the two manual steps
    /// into one hands-off process. Modelled on the vanilla quern's mechanical-power hookup.
    /// </summary>
    public class BlockMechScutcher : BlockMPBase
    {
        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            bool ok = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            if (ok)
            {
                if (!tryConnect(world, byPlayer, blockSel.Position, BlockFacing.UP))
                {
                    tryConnect(world, byPlayer, blockSel.Position, BlockFacing.DOWN);
                }
            }
            return ok;
        }

        public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face) { }

        public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face, BlockMPBase forBlock)
        {
            return face == BlockFacing.UP || face == BlockFacing.DOWN;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.Side == EnumAppSide.Client) return true;

            var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMechScutcher;
            if (be != null && be.OnInteract(byPlayer)) return true;

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
}
