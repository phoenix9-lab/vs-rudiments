using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// An invisible buried rhizome that slowly emerges as crop-nettle-1.
    /// Mirrors BlockNettleStub's tick pattern.
    ///
    /// - If the block below has Fertility == 0 (e.g. stone, water), self-destructs.
    ///   This is the natural counterplay: tilling farmland (replaceable >= 9000) also destroys it.
    /// - Otherwise rolls against Config.NettleCreepEmergeChance; on success, converts to crop-nettle-1.
    ///
    /// The class is always registered so any creep blocks placed while the toggle was on continue
    /// to resolve and emerge even if the player later turns NettleCreepEnabled off.
    /// </summary>
    public class BlockHiddenRhizome : Block
    {
        public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
        {
            extra = null;

            Block below = world.BlockAccessor.GetBlock(pos.DownCopy());

            // Self-destruct if sitting on infertile ground
            if (below.Fertility <= 0)
            {
                extra = "selfdestruct";
                return true;
            }

            // Roll for emergence
            if (offThreadRandom.NextDouble() < RudimentsModSystem.Config.NettleCreepEmergeChance)
            {
                extra = "emerge";
                return true;
            }

            return false;
        }

        public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
        {
            if (extra is string action)
            {
                if (action == "selfdestruct")
                {
                    world.BlockAccessor.SetBlock(0, pos);
                    return;
                }

                if (action == "emerge")
                {
                    Block nettle1 = world.GetBlock(new AssetLocation("rudiments:crop-nettle-1"));
                    if (nettle1 != null)
                        world.BlockAccessor.SetBlock(nettle1.BlockId, pos);
                    else
                        world.BlockAccessor.SetBlock(0, pos);
                    return;
                }
            }
        }
    }
}
