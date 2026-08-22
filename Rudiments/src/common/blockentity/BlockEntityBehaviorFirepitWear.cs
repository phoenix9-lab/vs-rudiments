using System.Collections.Generic;
using Rudiments.SRC.Common.Items;
using Rudiments.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.BlockEntities
{
    /// <summary>
    /// <c>BreakageIncludesFirepitCooking</c>: a fragile cooking vessel can fail the moment its cook
    /// finishes, approximating the heat-then-cool cycle as a wear event same as active use.
    ///
    /// Taking the pot back <i>out</i> of the firepit is not a hookable moment — retrieval is a GUI
    /// inventory drag out of the firepit dialog, never a held interaction on the pot itself, so
    /// nothing short of chasing the stack across an arbitrary destination slot (cursor, hotbar,
    /// another container — a real duplication/loss risk to get wrong) would catch it there. Rolling
    /// on the raw-to-cooked transition instead uses the exact same "mutate the slot this behavior
    /// already owns" technique as <see cref="BlockEntityBehaviorGroundStorageWear"/>: safe, and the
    /// player-facing outcome is the same either way — sometimes a cook does not survive its own pot.
    /// A failure loses the meal along with the vessel, same as <see cref="ClayWare.Shatter"/> always
    /// does for a shattered container's solid contents.
    ///
    /// Fireclay is exempt with no special-casing needed: crucibles and both mold types were never
    /// given <c>CollectibleBehaviorFragile</c> in the first place (see ware-fragility.json), and they
    /// become a smelted result, never a <see cref="BlockCookedContainer"/> — so the two gates below
    /// (the behavior check and the type check) already exclude them structurally.
    /// </summary>
    public class BlockEntityBehaviorFirepitWear : BlockEntityBehavior
    {
        /// <summary>Only the pot's own slot (input) and the legacy alternate (output) — never fuel or
        /// the loose-ingredient cooking slots, which a fragile item has no business occupying.</summary>
        private static readonly int[] WatchedSlots = { 1, 2 };

        private readonly Dictionary<int, CollectibleObject> lastSeen = new();

        public BlockEntityBehaviorFirepitWear(BlockEntity blockentity) : base(blockentity) { }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);

            if (Blockentity is BlockEntityFirepit firepit)
            {
                firepit.Inventory.SlotModified += OnSlotModified;
            }
        }

        private void OnSlotModified(int slotId)
        {
            if (System.Array.IndexOf(WatchedSlots, slotId) < 0) return;
            if (Api?.Side != EnumAppSide.Server) return;
            if (Blockentity is not BlockEntityFirepit firepit) return;

            ItemSlot slot = firepit.Inventory[slotId];
            ItemStack stack = slot?.Itemstack;
            CollectibleObject current = stack?.Collectible;

            lastSeen.TryGetValue(slotId, out CollectibleObject previous);
            lastSeen[slotId] = current;

            if (current == null || current == previous) return;
            if (current is not BlockCookedContainer) return;
            if (!RudimentsModSystem.Config.BreakageIncludesFirepitCooking) return;
            if (!current.HasBehavior<CollectibleBehaviorFragile>()) return;

            double chance = WareTier.BreakChance(WareTier.Get(stack), RudimentsModSystem.Config);
            if (chance <= 0 || Api.World.Rand.NextDouble() >= chance) return;

            ItemStack broken = stack.Clone();
            broken.StackSize = 1;

            Vec3d pos = Blockentity.Pos.ToVec3d().Add(0.5, 0.4, 0.5);
            ClayWare.Shatter(Api.World, pos, broken, 1);

            slot.TakeOut(1);
            slot.MarkDirty();
            lastSeen[slotId] = slot.Itemstack?.Collectible;
        }
    }
}
