using System.Text;
using Rudiments.SRC.Common.Blocks;
using Rudiments.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.Items
{
    /// <summary>
    /// Turns a raw mineral into a glaze by dusting it onto bone-dry greenware. Attached to
    /// <c>game:nugget-galena</c> for lead; the <c>glaze</c> property names which glaze it applies,
    /// so later glazes need no new class.
    ///
    /// There is no glaze bucket, no second firing and no new item: the nugget goes straight onto the
    /// pot and the pot goes straight into the kiln, which is how lead-glazed earthenware was made
    /// from around 1400 BCE onward. One nugget per vessel.
    ///
    /// Two ways to reach the ware, because greenware lives in ground storage rather than your hands:
    /// right-click a ground-storage pile of it, or hold it in your <b>off hand</b> — the same
    /// affordance the hand cards already use.
    /// </summary>
    public class CollectibleBehaviorGlazeApplicator : CollectibleBehavior
    {
        private string glaze;
        private string requiresGlaze;

        public CollectibleBehaviorGlazeApplicator(CollectibleObject collObj) : base(collObj) { }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            glaze = properties["glaze"].AsString("lead");

            // Tin glaze goes over a lead base rather than onto bare clay, so it can name a glaze it
            // requires the ware to already carry.
            requiresGlaze = properties["requiresGlaze"].AsString(null);
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);

            IWorldAccessor world = byEntity?.World;
            if (world == null || byEntity is not EntityPlayer entityPlayer) return;

            IPlayer byPlayer = entityPlayer.Player;
            ItemSlot target = FindGreenware(world, byEntity, blockSel);
            if (target == null) return;

            // We are handling this interaction whichever way it resolves, so the nugget never also
            // gets placed or eaten.
            handHandling = EnumHandHandling.PreventDefault;
            handling = EnumHandling.PreventDefault;

            if (world.Side != EnumAppSide.Server) return;

            ItemStack ware = target.Itemstack;

            if (WareTier.GetGlaze(ware) == glaze)
            {
                Refuse(byPlayer, "alreadyglazed", Lang.Get("rudiments:glaze-already", WareTier.GlazeName(glaze)));
                return;
            }

            if (requiresGlaze != null && WareTier.GetGlaze(ware) != requiresGlaze)
            {
                Refuse(byPlayer, "needsbase", Lang.Get("rudiments:glaze-needsbase", WareTier.GlazeName(requiresGlaze), WareTier.GlazeName(glaze)));
                return;
            }

            // One nugget per vessel: a stack of four raw bowls costs four. Rather than split the
            // stack we ask for the whole cost up front, which keeps the ground-storage slot intact.
            if (slot.StackSize < ware.StackSize)
            {
                Refuse(byPlayer, "notenough", Lang.Get("rudiments:glaze-notenough", ware.StackSize, slot.StackSize));
                return;
            }

            WareTier.SetGlaze(ware, glaze);
            slot.TakeOut(ware.StackSize);
            slot.MarkDirty();
            target.MarkDirty();

            if (blockSel != null && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityGroundStorage begs)
            {
                begs.MarkDirty(true);
            }

            world.PlaySoundAt(new AssetLocation("game", "sounds/player/messycraft"), byEntity, byPlayer, false, 12f);
        }

        /// <summary>
        /// Greenware the player is pointing at in ground storage, or failing that whatever is in
        /// their off hand. Returns null when neither is glazable, so the nugget behaves normally.
        /// </summary>
        private static ItemSlot FindGreenware(IWorldAccessor world, EntityAgent byEntity, BlockSelection blockSel)
        {
            if (blockSel != null && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityGroundStorage begs)
            {
                ItemSlot slot = begs.GetSlotAt(blockSel);
                if (IsGlazable(slot)) return slot;
            }

            return IsGlazable(byEntity.LeftHandItemSlot) ? byEntity.LeftHandItemSlot : null;
        }

        /// <summary>
        /// Only greenware whose blocktype actually carries the glaze through firing. Testing for the
        /// class rather than for "is it raw clay" means a mod's greenware is glazable exactly when
        /// someone has opted it in, and never accidentally.
        /// </summary>
        private static bool IsGlazable(ItemSlot slot)
        {
            return slot?.Itemstack?.Collectible is BlockGlazableClayware;
        }

        private static void Refuse(IPlayer byPlayer, string code, string message)
        {
            if (byPlayer is IServerPlayer splr) splr.SendIngameError(code, message);
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            dsc.AppendLine(Lang.Get("rudiments:glaze-applicator-help", WareTier.GlazeName(glaze)));
        }
    }
}
