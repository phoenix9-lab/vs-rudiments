using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// Thin subclass of BlockReeds that injects the rhizome-spread tick.
    /// All existing BlockReeds behavior is preserved; we only add the spread attempt
    /// on each game tick the block already receives. Applied to reedpapyrus via JSON patch
    /// so coopersreed, papyrus, tule, and brownsedge all get rhizome spreading.
    /// The BlockBehaviorRhizomeSpread behavior handles water-vs-soil detection automatically
    /// from the block's "habitat" variant.
    /// </summary>
    public class BlockReedsWithSpread : BlockReeds
    {
        public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
        {
            base.OnServerGameTick(world, pos, extra);
            GetBehavior<BlockBehaviorRhizomeSpread>()?.TrySpreadTick(world, pos);
        }
    }
}
