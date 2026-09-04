using Rudiments.SRC.Common.Blocks;
using Rudiments.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.Items
{
    /// <summary>
    /// Cracks open a failed crucible sitting in ground storage (shelf or bare ground), same
    /// hammer-offhand + chisel-active requirement <c>BlockIngotMold.OnBlockBroken</c> already uses
    /// for chiseling a hardened mold pour loose. There is no vanilla "chisel a portable item" hook to
    /// reuse — molds get their equivalent by being placed blocks, but a crucible stays
    /// <c>Unplaceable</c> — so this rides the one extension point ground storage exposes for exactly
    /// this shape of interaction: <see cref="IContainedInteractable"/>, the same interface
    /// <c>CollectibleBehaviorGroundStoredProcessable</c> uses for "right-click a ground-stored item
    /// with a tool to process it." Scoped to ground storage only: two hands can't hold a hammer, a
    /// chisel, and the crucible being worked all at once.
    /// </summary>
    public class CollectibleBehaviorCrucibleCrack : CollectibleBehavior, IContainedInteractable
    {
        public CollectibleBehaviorCrucibleCrack(CollectibleObject collObj) : base(collObj) { }

        public bool OnContainedInteractStart(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (slot.Itemstack?.Collectible is not BlockCrucibleFailed crucible) return false;
            if (byPlayer?.InventoryManager is not IPlayerInventoryManager invMan) return false;
            if (invMan.OffhandTool is not EnumTool.Hammer || invMan.ActiveTool is not EnumTool.Chisel) return false;

            if (be.Api.Side == EnumAppSide.Server)
            {
                var contents = crucible.GetContents(be.Api.World, slot.Itemstack);
                ItemStack metal = contents.Key;
                int units = contents.Value;

                Vec3d pos = blockSel.Position.ToVec3d().Add(0.5, 0.3, 0.5);

                ItemStack broken = slot.Itemstack.Clone();
                broken.StackSize = 1;
                ClayWare.Shatter(be.Api.World, pos, broken, 1);

                if (metal != null && units > 0)
                {
                    var recoveredJson = metal.Collectible.Attributes?["shatteredStack"].AsObject<JsonItemStack>();
                    recoveredJson?.Resolve(be.Api.World, "shatteredStack for " + metal.Collectible.Code);

                    if (recoveredJson?.ResolvedItemstack is ItemStack recovered)
                    {
                        recovered.StackSize = (int)(units * RudimentsModSystem.Config.SmeltingFailureYield);
                        if (recovered.StackSize > 0 && !byPlayer.InventoryManager.TryGiveItemstack(recovered))
                        {
                            be.Api.World.SpawnItemEntity(recovered, pos);
                        }
                    }
                }

                slot.TakeOut(1);
                slot.MarkDirty();
                if (be.Inventory.Empty) be.Api.World.BlockAccessor.SetBlock(0, blockSel.Position);

                ItemSlot activeSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
                activeSlot.Itemstack?.Collectible.DamageItem(be.Api.World, byPlayer.Entity, activeSlot);
                ItemSlot offhandSlot = byPlayer.Entity.LeftHandItemSlot;
                offhandSlot?.Itemstack?.Collectible.DamageItem(be.Api.World, byPlayer.Entity, offhandSlot);
            }

            return true;
        }

        public bool OnContainedInteractStep(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) => false;

        public void OnContainedInteractStop(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) { }

        public bool OnContainedInteractCancel(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason) => true;

        public WorldInteraction[] GetContainedInteractionHelp(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (slot.Itemstack?.Collectible is not BlockCrucibleFailed) return [];

            return
            [
                new WorldInteraction
                {
                    ActionLangCode = "rudiments:blockhelp-crucible-crack",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = ObjectCacheUtil.GetToolStacks(be.Api, EnumTool.Chisel)
                }
            ];
        }
    }
}
