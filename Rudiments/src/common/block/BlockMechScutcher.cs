using Rudiments.SRC.Common.BlockEntities;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent.Mechanics;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// Mechanically-powered scutching mill. Drive it with an axle into either post, or from above or
    /// below as before; once turning it automatically breaks and scutches dried (or already broken)
    /// fiber bundles into scutched bundles, collapsing the two manual steps into one hands-off process.
    /// Placed like the vanilla brake: the frame faces the player with the axle running across it,
    /// unless a drive already sits beside the spot, in which case the mill turns to line up with it.
    /// </summary>
    public class BlockMechScutcher : BlockMPBase
    {
        private WorldInteraction[] interactions;

        /// <summary>The direction the frame faces. The axle runs square to it, through both posts;
        /// the front and back are the only faces without a connector.</summary>
        public BlockFacing Facing => BlockFacing.FromCode(Variant["side"]);

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            if (api is not ICoreClientAPI) return;

            interactions = ObjectCacheUtil.GetOrCreate(api, "mechScutcherInteractions", () =>
            {
                List<ItemStack> feedStacks = new List<ItemStack>();
                foreach (Item item in api.World.Items)
                {
                    if (item?.Code == null) continue;
                    ItemStack stack = new ItemStack(item);
                    if (BlockEntityMechScutcher.IsMillable(api.World, stack)) feedStacks.Add(stack);
                }

                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "rudiments:scutcher-blockhelp-feed",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = feedStacks.ToArray()
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "rudiments:scutcher-blockhelp-collect",
                        MouseButton = EnumMouseButton.Right,
                        RequireFreeHand = true
                    }
                };
            });
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode)) return false;

            BlockFacing facing = SuggestedHVOrientation(byPlayer, blockSel)[0];

            // Prefer facing the player; failing a drive on that axle, try the mill turned a quarter.
            foreach (BlockFacing side in new[] { facing, facing.GetCW() })
            {
                Block block = world.GetBlock(CodeWithVariant("side", side.Code));
                foreach (BlockFacing face in BlockFacing.ALLFACES)
                {
                    if (face.Axis == side.Axis) continue;

                    BlockPos neibPos = blockSel.Position.AddCopy(face);
                    if (world.BlockAccessor.GetBlock(neibPos) is not IMechanicalPowerBlock neib
                        || !neib.HasMechPowerConnectorAt(world, neibPos, face.Opposite, this)) continue;

                    if (!block.DoPlaceBlock(world, byPlayer, blockSel, itemstack)) return false;
                    neib.DidConnectAt(world, neibPos, face.Opposite);
                    WasPlaced(world, blockSel.Position, face);
                    return true;
                }
            }

            return world.GetBlock(CodeWithVariant("side", facing.Code)).DoPlaceBlock(world, byPlayer, blockSel, itemstack);
        }

        public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face) { }

        // Decided from the variant alone: vanilla axles pass their own position here, not the mill's.
        // Top and bottom stay connectors so mills driven from above or below before 2.2.0 keep turning.
        public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face, BlockMPBase forBlock)
        {
            return face.Axis != Facing.Axis;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not BlockEntityMechScutcher be)
                return base.OnBlockInteractStart(world, byPlayer, blockSel);

            // Only claim clicks the mill can answer, so an axle in hand still builds against it.
            ItemStack held = byPlayer.InventoryManager?.ActiveHotbarSlot?.Itemstack;
            if (held == null ? be.IsEmpty : !BlockEntityMechScutcher.IsFibreBundle(held))
                return base.OnBlockInteractStart(world, byPlayer, blockSel);

            // Claim the interaction on both sides even when a load is refused (wrong stage, mixed
            // batch, full) — the block entity raises the in-game error itself.
            if (world.Side == EnumAppSide.Client) return true;
            be.OnInteract(byPlayer);
            return true;
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            WorldInteraction[] baseInteractions = base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
            return interactions == null ? baseInteractions : interactions.Append(baseInteractions);
        }
    }
}
