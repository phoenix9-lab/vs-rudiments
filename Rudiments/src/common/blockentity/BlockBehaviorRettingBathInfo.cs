using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Rudiments.SRC.Common.BlockEntities
{
    /// <summary>
    /// Companion BlockBehavior to BlockEntityBehaviorRettingBath. BlockLiquidContainerBase only
    /// calls BlockBehavior.GetPlacedBlockInfo (block-level), never BlockEntityBehavior.GetBlockInfo.
    /// This class bridges the gap: it reads the entity behavior's state and returns the progress
    /// text so it surfaces in the barrel's HUD.
    /// </summary>
    public class BlockBehaviorRettingBathInfo : BlockBehavior
    {
        public BlockBehaviorRettingBathInfo(Block block) : base(block) { }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            var be = world.BlockAccessor.GetBlockEntity(pos);
            if (be == null) return "";

            var beh = be.GetBehavior<BlockEntityBehaviorRettingBath>();
            if (beh == null || !beh.IsActive) return "";

            var sb = new StringBuilder();
            beh.AppendStatus(sb, world);
            return sb.ToString();
        }
    }
}
