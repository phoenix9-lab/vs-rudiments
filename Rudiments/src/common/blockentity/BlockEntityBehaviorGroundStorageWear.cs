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
    /// <c>BreakageIncludesGroundStorage</c>: a fragile vessel can fail the moment it is set down on
    /// a shelf or the bare ground, not just while it is actively being used.
    ///
    /// There is no held-interact hook for this. Ground storage moves the item out of the player's
    /// hand entirely inside <c>CollectibleBehaviorGroundStorable.OnHeldInteractStart</c>, which runs
    /// before <c>CollectibleBehaviorFragile</c> ever sees the slot (it is appended after
    /// GroundStorable in every vessel's behavior list) and which sets <c>PreventSubsequent</c> on
    /// success anyway — so there is nothing left in hand for Fragile's own
    /// <c>OnHeldInteractStop</c> to roll against once the interaction ends. This behavior watches the
    /// one place the placed item unambiguously lands instead: the ground-storage block entity's own
    /// inventory.
    ///
    /// One behavior on the vanilla <c>groundstorage</c> block entity type covers every vessel that
    /// carries <c>CollectibleBehaviorFragile</c>, with no per-item patching.
    /// </summary>
    public class BlockEntityBehaviorGroundStorageWear : BlockEntityBehavior
    {
        private readonly Dictionary<int, CollectibleObject> lastSeen = new();

        public BlockEntityBehaviorGroundStorageWear(BlockEntity blockentity) : base(blockentity) { }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);

            if (Blockentity is BlockEntityGroundStorage groundStorage)
            {
                groundStorage.Inventory.SlotModified += OnSlotModified;
            }
        }

        /// <summary>
        /// Edge-triggered on "a fragile vessel just arrived here" — compared against what this slot
        /// held last time, not re-rolled on every unrelated dirty notification (temperature ticks,
        /// neighbouring slots changing, etc.) for as long as it keeps sitting there.
        /// </summary>
        private void OnSlotModified(int slotId)
        {
            if (Api?.Side != EnumAppSide.Server) return;
            if (Blockentity is not BlockEntityGroundStorage groundStorage) return;

            ItemSlot slot = groundStorage.Inventory[slotId];
            ItemStack stack = slot?.Itemstack;
            CollectibleObject current = stack?.Collectible;

            lastSeen.TryGetValue(slotId, out CollectibleObject previous);
            lastSeen[slotId] = current;

            if (current == null || current == previous) return;
            if (!RudimentsModSystem.Config.BreakageIncludesGroundStorage) return;
            if (!current.HasBehavior<CollectibleBehaviorFragile>()) return;

            double chance = WareTier.BreakChance(WareTier.Get(stack), RudimentsModSystem.Config);
            if (chance <= 0 || Api.World.Rand.NextDouble() >= chance) return;

            ItemStack broken = stack.Clone();
            broken.StackSize = 1;

            Vec3d pos = Blockentity.Pos.ToVec3d().Add(0.5, 0.25, 0.5);
            ClayWare.Shatter(Api.World, pos, broken, 1);

            slot.TakeOut(1);
            slot.MarkDirty();
            lastSeen[slotId] = slot.Itemstack?.Collectible;
        }
    }
}
