using Rudiments.SRC.Common.BlockEntities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Rudiments.SRC.Common.Items
{
    /// <summary>
    /// Custom item class for flaxbundle and nettlebundle. Right-clicking the TOP face of a
    /// solid block auto-places the appropriate ground block and transfers the bundles:
    ///   *-unprocessed  → stook (cure mode)
    ///   flaxbundle-rippled / nettlebundle-cured → field retting (laid bundles)
    ///   *-retted       → stook (dry mode, risky)
    ///   everything else → fall through to vanilla GroundStorable
    /// </summary>
    public class ItemFieldRettableBundle : Item
    {
        // ---- helpers ----

        /// <summary>Returns the cured output code for an unprocessed bundle, or null.</summary>
        public static AssetLocation GetCuredOutput(ItemStack stack)
        {
            if (stack?.Collectible?.Code == null) return null;
            string path = stack.Collectible.Code.Path;
            if (path.EndsWith("-unprocessed"))
                return stack.Collectible.Code.CopyWithPath(path.Replace("-unprocessed", "-cured"));
            return null;
        }

        /// <summary>
        /// Returns true when the bundle should be placed as a stook, and the mode
        /// (cure vs dry) is determined by the bundle path suffix.
        ///   cure: *-unprocessed
        ///   dry:  *-retted
        /// </summary>
        public static bool GetStookMode(ItemStack stack, out bool isCure)
        {
            isCure = false;
            if (stack?.Collectible?.Code == null) return false;
            string path = stack.Collectible.Code.Path;
            if (path.EndsWith("-unprocessed")) { isCure = true;  return true; }
            if (path.EndsWith("-retted"))       { isCure = false; return true; }
            return false;
        }

        // ---- routing ----

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity,
            BlockSelection blockSel, EntitySelection entitySel, bool firstEvent,
            ref EnumHandHandling handling)
        {
            if (blockSel == null || blockSel.Face != BlockFacing.UP)
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }

            IWorldAccessor world = byEntity.World;
            BlockPos placePos = blockSel.Position.UpCopy();
            Block atPlace = world.BlockAccessor.GetBlock(placePos);
            if (atPlace.BlockId != 0)
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }

            ItemStack heldStack = slot.Itemstack;
            string path = heldStack?.Collectible?.Code?.Path ?? "";

            // --- route: stook (cure mode) ---
            if (path.EndsWith("-unprocessed"))
            {
                handling = EnumHandHandling.PreventDefault;
                if (world.Side == EnumAppSide.Server)
                    PlaceStook(world, placePos, (byEntity as EntityPlayer)?.Player);
                return;
            }

            // --- route: field retting ---
            if (BlockEntityRettingBase.GetRettedOutput(heldStack) != null)
            {
                handling = EnumHandHandling.PreventDefault;
                if (world.Side == EnumAppSide.Server)
                    PlaceFieldRetting(world, placePos, (byEntity as EntityPlayer)?.Player);
                return;
            }

            // --- route: stook (dry mode) ---
            if (path.EndsWith("-retted"))
            {
                handling = EnumHandHandling.PreventDefault;
                if (world.Side == EnumAppSide.Server)
                    PlaceStook(world, placePos, (byEntity as EntityPlayer)?.Player);
                return;
            }

            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        }

        private static void PlaceFieldRetting(IWorldAccessor world, BlockPos placePos, IPlayer player)
        {
            Block block = world.GetBlock(new AssetLocation("rudiments:fieldretting-north"));
            if (block == null || block.Id == 0) return;

            world.BlockAccessor.SetBlock(block.BlockId, placePos);
            world.BlockAccessor.TriggerNeighbourBlockUpdate(placePos);

            var be = world.BlockAccessor.GetBlockEntity(placePos) as BlockEntityFieldRetting;
            if (be != null && player != null)
                be.OnInteract(player);
        }

        private static void PlaceStook(IWorldAccessor world, BlockPos placePos, IPlayer player)
        {
            Block block = world.GetBlock(new AssetLocation("rudiments:stook"));
            if (block == null || block.Id == 0) return;

            world.BlockAccessor.SetBlock(block.BlockId, placePos);
            world.BlockAccessor.TriggerNeighbourBlockUpdate(placePos);

            var be = world.BlockAccessor.GetBlockEntity(placePos) as BlockEntityStook;
            if (be != null && player != null)
                be.OnInteract(player);
        }
    }
}
