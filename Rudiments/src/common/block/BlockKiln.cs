using Rudiments.SRC.Common.BlockEntities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// Thin router for any <see cref="BlockEntityKilnBase"/>: right-click loads and unloads, sneak +
    /// right-click lights it. No GUI, no ignition item.
    ///
    /// One class serves both the small brick kiln and the updraft kiln because neither has any
    /// block-level behaviour that differs — the updraft kiln's chimney requirement is an ignition
    /// condition, and ignition conditions belong to the block entity. It is registered under both
    /// <c>rudiments:BlockSmallBrickKiln</c> and <c>rudiments:BlockUpdraftKiln</c> so each blocktype
    /// still names the class it means.
    /// </summary>
    internal class BlockKiln : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.Side == EnumAppSide.Client) return true;

            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not BlockEntityKilnBase be) return false;

            return byPlayer.Entity.Controls.ShiftKey ? be.TryIgnite(byPlayer) : be.OnInteract(byPlayer);
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            var be = world.BlockAccessor.GetBlockEntity(selection.Position) as BlockEntityKilnBase;
            if (be != null && be.IsBurning) return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);

            return new WorldInteraction[]
            {
                new WorldInteraction
                {
                    ActionLangCode = "rudiments:blockhelp-kiln-load",
                    MouseButton = EnumMouseButton.Right
                },
                new WorldInteraction
                {
                    ActionLangCode = "rudiments:blockhelp-kiln-ignite",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "shift"
                }
            }.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
    }
}
