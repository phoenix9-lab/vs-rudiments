using Rudiments.SRC.Common.BlockEntities;
using Vintagestory.API.Common;

namespace Rudiments.SRC.Common.Blocks
{
    internal class BlockStook : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.Side == EnumAppSide.Client) return true;

            var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityStook;
            if (be == null) return false;

            return be.OnInteract(byPlayer);
        }
    }
}
