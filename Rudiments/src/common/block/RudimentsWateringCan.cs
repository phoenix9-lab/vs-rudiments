using System.Text;
using Rudiments.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// A watering can that has to actually hold water.
    ///
    /// The spouted, rosed watering can is a late invention — roughly 17th century onward in Europe.
    /// Earlier gardeners used thumb-pots and chantepleures, and <i>ollas</i>, buried unglazed jars
    /// that seep on purpose, predate both by millennia. Vanilla handing one over on day one is the
    /// ahistorical part; requiring a sealed body puts it back where it belongs.
    ///
    /// Refuse-to-fill rather than refuse-to-craft, deliberately: the player learns the rule at the
    /// water's edge with the object in hand and a readable message, instead of hitting a silent
    /// recipe wall and wondering what they are missing.
    ///
    /// Continuous draining was evaluated and cut. <c>Get/SetRemainingWateringSeconds</c> are public
    /// but not virtual, and the unit is watering-seconds rather than litres, so there is nothing
    /// clean to hook. Do not attempt it.
    /// </summary>
    internal class RudimentsWateringCan : BlockWateringCan
    {
        public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (RefusesToFill(itemslot, byEntity, blockSel))
            {
                handHandling = EnumHandHandling.PreventDefault;
                return;
            }

            base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
        }

        /// <summary>
        /// True when this is an attempt to fill an unsealed can from a water source. Emptying a can
        /// onto crops is never blocked — only getting water into a porous one is.
        /// </summary>
        private bool RefusesToFill(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel)
        {
            if (!RudimentsModSystem.Config.SealedWareRequiredForWateringCan) return false;
            if (blockSel == null) return false;

            ItemStack stack = itemslot?.Itemstack;
            if (stack == null || WareTier.IsSealed(stack)) return false;

            Block targeted = byEntity.World.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Fluid);
            if (targeted?.IsLiquid() != true) return false;

            if (byEntity.World.Side == EnumAppSide.Server && (byEntity as EntityPlayer)?.Player is IServerPlayer splr)
            {
                splr.SendIngameError("unsealedcan", Lang.Get("rudiments:wateringcan-unsealed"));
            }

            return true;
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            if (!RudimentsModSystem.Config.SealedWareRequiredForWateringCan) return;

            dsc.AppendLine(WareTier.IsSealed(inSlot?.Itemstack)
                ? Lang.Get("rudiments:wateringcan-sealed-ok")
                : Lang.Get("rudiments:wateringcan-sealed-required"));
        }
    }
}
