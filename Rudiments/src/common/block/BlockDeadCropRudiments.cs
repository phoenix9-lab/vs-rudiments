using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// Replaces the vanilla dead-crop block class (via patches/crop-seeds.json) to remove the
    /// vanilla "dead crops always drop seeds" hack in BlockEntityDeadCrop.GetDrops. Drops are
    /// whatever the stored crop stage would drop, run through the usual farmland damage
    /// multipliers — so a crop eaten by an animal or killed before maturity returns nothing.
    /// Honors the SeedsOnlyWhenMature config flag at runtime: when disabled, falls through to
    /// the vanilla behavior (guaranteed seed return).
    /// </summary>
    public class BlockDeadCropRudiments : BlockDeadCrop
    {
        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (!RudimentsModSystem.Config.SeedsOnlyWhenMature)
            {
                return base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
            }

            if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityDeadCrop be)
            {
                if (be.Inventory[0].Empty) return System.Array.Empty<ItemStack>();
                return be.Inventory[0].Itemstack.Block?.GetDrops(world, pos, byPlayer, dropQuantityMultiplier)
                       ?? System.Array.Empty<ItemStack>();
            }

            return base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
        }
    }
}
