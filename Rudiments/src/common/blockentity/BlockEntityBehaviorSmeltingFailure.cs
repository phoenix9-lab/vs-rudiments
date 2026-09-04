using System.Collections.Generic;
using Rudiments.SRC.Common.Blocks;
using Rudiments.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.BlockEntities
{
    /// <summary>
    /// <c>BreakageIncludesSmeltingFailure</c>: a crucible can fail the moment its smelt finishes,
    /// giving crucibles the same partial-metal-recovery risk ingot molds already have when they
    /// shatter — see <see cref="BEIngotMold.GetStateAwareContentsSided"/>, which the eventual payout
    /// mirrors: both read the metal's own vanilla <c>shatteredStack</c> attribute and pay out a flat
    /// share of the fill/unit count, so any metal or alloy a mold already handles works here with no
    /// per-metal patching.
    ///
    /// A mold gets there via a thermal-shock roll on cooling followed by a separate "break the placed
    /// block open" step that pays out the recovery. A crucible is always an item — Unplaceable +
    /// GroundStorable, per crucible.json, never a placed block — so there is no "break it open"
    /// interim state to reuse as-is. This behavior supplies the crucible's own version of that state
    /// instead: on a failed roll it transforms the smelted crucible into a <see
    /// cref="BlockCrucibleFailed"/> item, carrying the pending metal content across, using the same
    /// "mutate the slot this behavior already owns" technique as
    /// <see cref="BlockEntityBehaviorFirepitWear"/>. The actual metal-bits payout happens later, when
    /// that item is cracked open — see <see cref="Rudiments.SRC.Common.Items.CollectibleBehaviorCrucibleCrack"/>.
    /// </summary>
    public class BlockEntityBehaviorSmeltingFailure : BlockEntityBehavior
    {
        /// <summary>The crucible's own slot (input) and where its smelted result lands (output) —
        /// never fuel or the loose-ore cooking slots.</summary>
        private static readonly int[] WatchedSlots = { 1, 2 };

        private readonly Dictionary<int, CollectibleObject> lastSeen = new();

        public BlockEntityBehaviorSmeltingFailure(BlockEntity blockentity) : base(blockentity) { }

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
            if (current is not BlockSmeltedContainer smeltedBlock) return;
            if (!RudimentsModSystem.Config.BreakageIncludesSmeltingFailure) return;

            double chance = RudimentsModSystem.Config.SmeltingFailureChance;
            if (chance <= 0 || Api.World.Rand.NextDouble() >= chance) return;

            var contents = smeltedBlock.GetContents(Api.World, stack);

            string color = stack.Collectible.Variant["color"];
            Block failedBlock = Api.World.GetBlock(new AssetLocation("game", $"crucible-{color}-failed"));
            if (failedBlock is not BlockCrucibleFailed bcf) return;

            ItemStack failedStack = new(failedBlock);
            bcf.SetContents(failedStack, contents.Key, contents.Value);

            BlockPos pos = Blockentity.Pos;
            Api.World.PlaySoundAt(ClayWare.BreakSound, pos.X, pos.Y, pos.Z, null, true, 16f);
            stack.Block?.SpawnBlockBrokenParticles(pos);

            slot.Itemstack = failedStack;
            slot.MarkDirty();
            lastSeen[slotId] = failedStack.Collectible;
        }
    }
}
