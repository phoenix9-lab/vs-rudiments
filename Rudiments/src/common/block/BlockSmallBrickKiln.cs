using Rudiments.SRC.Common.BlockEntities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// Thin router for <see cref="BlockEntitySmallBrickKiln"/>: right-click loads and unloads,
    /// sneak + right-click lights it. Igniting with a held firestarter is left to the same
    /// right-click as everything else — this kiln has no GUI and no separate ignition item.
    /// </summary>
    internal class BlockSmallBrickKiln : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.Side == EnumAppSide.Client) return true;

            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not BlockEntitySmallBrickKiln be) return false;

            return byPlayer.Entity.Controls.ShiftKey ? be.TryIgnite(byPlayer) : be.OnInteract(byPlayer);
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            var be = world.BlockAccessor.GetBlockEntity(selection.Position) as BlockEntitySmallBrickKiln;
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
