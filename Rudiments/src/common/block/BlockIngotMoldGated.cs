using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// <c>MoldRecoveryRequiresTool</c>: gates a shattered ingot mold's metal-bits payout behind the
    /// same hammer-offhand + chisel-active requirement vanilla already uses for chiseling a hardened,
    /// non-shattered pour loose (<c>BlockIngotMold.GetChiseledStack</c>) — bare-handed breaking today
    /// recovers the metal for free, which this closes.
    ///
    /// <c>BlockIngotMold.OnBlockBroken</c>'s own bare-hand branch never reaches this: it first tries
    /// <c>GetStateAwareMoldSided(mold, shattered: true)</c>, which reads the mold's own
    /// <c>shatteredDrops</c> attribute — undefined on every vanilla ingotmold blocktype, so that call
    /// always returns empty and <c>OnBlockBroken</c> falls through to <c>base.OnBlockBroken</c>, which
    /// spawns whatever <see cref="GetDrops"/> returns. That fallthrough is the only place the metal
    /// bits actually originate, and byPlayer is passed straight through to it — so overriding
    /// <see cref="GetDrops"/> alone is sufficient, with no need to touch <c>OnBlockBroken</c> itself.
    ///
    /// Only a completed, hardened, non-shattered ingot recovered via this same fallthrough (an edge
    /// case — normally taken by right-click before the block ever breaks) is left ungated: this is
    /// about the shatter-recovery risk specifically, not ingot pickup in general. Scoped to ingot
    /// molds; tool molds are unaffected.
    /// </summary>
    public class BlockIngotMoldGated : BlockIngotMold
    {
        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (!RudimentsModSystem.Config.MoldRecoveryRequiresTool) return base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
            if (world.BlockAccessor.GetBlockEntity(pos) is not BlockEntityIngotMold beim) return base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);

            bool hasTools = byPlayer?.InventoryManager is IPlayerInventoryManager invMan && invMan.OffhandTool is EnumTool.Hammer && invMan.ActiveTool is EnumTool.Chisel;

            var drops = new List<ItemStack>(beim.GetStateAwareMolds());

            if (hasTools)
            {
                drops.AddRange(beim.GetStateAwareMoldedStacks());
            }
            else
            {
                if (!beim.ShatteredLeft && beim.GetStateAwareContentsLeft() is ItemStack l) drops.Add(l);
                if (!beim.ShatteredRight && beim.GetStateAwareContentsRight() is ItemStack r) drops.Add(r);
            }

            return drops.ToArray();
        }
    }
}
