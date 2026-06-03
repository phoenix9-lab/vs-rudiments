using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.Items
{
    /// <summary>
    /// Nettle rhizome — a root cutting that lets the player establish new wild nettle patches on
    /// fertile soil without needing farmland. Right-click on soil/grass to plant. If the block
    /// below the placement position has Fertility > 0 and the placement position itself is open
    /// (Replaceable >= 6000), a nettle-1 block is placed.
    /// </summary>
    public class ItemNettleRhizome : Item
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel,
            EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (blockSel == null || byEntity.World.Side != EnumAppSide.Server) return;

            // We place ON TOP of the clicked block face.
            BlockPos placePos = blockSel.Position.AddCopy(blockSel.Face);
            BlockPos soilPos  = placePos.DownCopy();

            Block soil    = byEntity.World.BlockAccessor.GetBlock(soilPos);
            Block atPlace = byEntity.World.BlockAccessor.GetBlock(placePos);

            if (soil.Fertility <= 0 || atPlace.Replaceable < 6000) return;

            Block nettle1 = byEntity.World.GetBlock(new AssetLocation("rudiments:crop-nettle-1"));
            if (nettle1 == null) return;

            IPlayer player = (byEntity as EntityPlayer)?.Player;
            if (player != null && !byEntity.World.Claims.TryAccess(player, placePos, EnumBlockAccessFlags.BuildOrBreak)) return;

            byEntity.World.BlockAccessor.SetBlock(nettle1.BlockId, placePos);
            byEntity.World.PlaySoundAt(new AssetLocation("game:sounds/block/plant"), byEntity, null, false, 16f, 0.9f);
            slot.TakeOut(1);
            slot.MarkDirty();

            handling = EnumHandHandling.PreventDefault;
        }
    }
}
