using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Rudiments.Utils
{
    /// <summary>
    /// Static helper for nettle's heavy-feeder nitrogen depletion of neighbouring farmland.
    /// Mirrors the style of NettleSting.cs. Server-side only.
    ///
    /// Plain (non-farmland) soil has no BlockEntity and carries only a fixed Fertility int that is
    /// never decremented by the game, so ConsumeNutrients only meaningfully affects farmland tiles.
    /// </summary>
    public static class NettleFeeder
    {
        /// <summary>
        /// Drains <paramref name="amount"/> units of nitrogen from every horizontally adjacent
        /// <see cref="BlockEntityFarmland"/> found at the given soil-level position.
        /// Must be called server-side; does nothing on the client.
        /// </summary>
        /// <param name="world">The world accessor.</param>
        /// <param name="soilPos">The position of the soil block directly below the nettle
        /// (i.e. <c>nettle.pos.DownCopy()</c>).</param>
        /// <param name="amount">Amount of N to deduct (floored at 0 by the API).</param>
        public static void DepleteNeighborNitrogen(IWorldAccessor world, BlockPos soilPos, float amount)
        {
            if (world.Side != EnumAppSide.Server) return;

            foreach (BlockFacing face in BlockFacing.HORIZONTALS)
            {
                BlockPos neighborPos = soilPos.AddCopy(face);
                BlockEntityFarmland bef = world.BlockAccessor.GetBlockEntity(neighborPos) as BlockEntityFarmland;
                bef?.ConsumeNutrients(EnumSoilNutrient.N, amount);
            }
        }
    }
}
