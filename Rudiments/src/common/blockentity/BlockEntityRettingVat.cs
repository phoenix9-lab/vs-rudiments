using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Rudiments.SRC.Common.BlockEntities
{
    /// <summary>
    /// Water / barrel retting. Submerging bundles in a vat speeds up fibre separation
    /// significantly compared to field retting, and the water environment is steady — no
    /// weather variance. Quality still follows the 4-stage time-windowed model: under-retted
    /// → Coarse → Fine window (RNG) → Standard → Rot. Because conditions are controlled,
    /// the Fine window is easier to hit but the rot timer is also tighter, rewarding
    /// attentive players. Thresholds can be overridden per-block via JSON attributes.
    ///
    /// Lime modifier (slot 1 / LimeSlot):
    ///   Place quicklime (game:quicklime) in the second slot to activate lime retting.
    ///   One unit of quicklime is consumed when a new retting cycle starts (fibres inserted
    ///   while lime is present, or lime inserted while fibres are already waiting).
    ///   Lime retting is 2.5× faster but quality is hard-capped at Standard — the alkaline
    ///   environment destroys fine fibre structure (FineChance forced to 0). The rot window
    ///   is also halved, so leaving the vat unattended quickly ruins the batch.
    /// </summary>
    public class BlockEntityRettingVat : BlockEntityRettingBase
    {
        protected override string InvKey => "rettingvat";
        protected override string LangPrefix => "rudiments:rettingvat";
        protected override int InventorySlots => 2;

        /// <summary>Slot 1: quicklime modifier.</summary>
        public ItemSlot LimeSlot => inventory[1];

        private const string QuicklimeCode = "game:quicklime";

        protected override void LoadDefaults()
        {
            MinRetHours   = 18;
            FineChance    = 0.6;
            FineDelayMin  = 6;
            FineDelayMax  = 18;
            FineWindow    = 12;
            StandardDelay = 24;
            StandardHold  = 36;
        }

        // GetProgressRate() returns 1.0 (base default) — water retting is steady, no weather factor.
        // Lime rate boost is applied in the base class OnGameTick via limeActive flag.

        /// <summary>
        /// Returns true if the lime slot currently holds quicklime.
        /// </summary>
        private bool HasLime()
        {
            if (LimeSlot.Empty) return false;
            return LimeSlot.Itemstack?.Collectible?.Code?.ToString() == QuicklimeCode;
        }

        /// <summary>
        /// Consume one unit of quicklime from the lime slot and set limeActive.
        /// Called when a new retting cycle starts with lime present.
        /// </summary>
        private void ConsumeLime()
        {
            if (!HasLime()) return;
            LimeSlot.TakeOut(1);
            LimeSlot.MarkDirty();
            limeActive = true;
            MarkDirty(true);
        }

        /// <summary>
        /// Hook called by the base class after fibres are successfully inserted.
        /// If lime is present, consume one unit and activate lime mode.
        /// </summary>
        protected override void OnFibersInserted()
        {
            if (HasLime())
                ConsumeLime();
        }

        /// <summary>
        /// Hook called when lime is placed into the lime slot while fibres are already waiting
        /// (not yet converted). Consume one unit immediately to start lime mode.
        /// </summary>
        private void OnLimeInserted()
        {
            // Only activate if a retting cycle is in progress but not yet converted
            // (i.e. fibres are present, not yet retted). If already converted, lime has no effect.
            if (!FiberSlot.Empty && !limeActive)
                ConsumeLime();
        }

        public override bool OnInteract(IPlayer player)
        {
            var heldSlot = player.InventoryManager.ActiveHotbarSlot;
            var heldItem = heldSlot?.Itemstack;

            // --- Lime slot interactions (shift-right-click or lime in hand) ---
            // Player holds quicklime → try to insert into lime slot
            if (heldItem != null && heldItem.Collectible?.Code?.ToString() == QuicklimeCode)
            {
                if (LimeSlot.Empty)
                {
                    LimeSlot.Itemstack = heldItem.Clone();
                    heldSlot.TakeOutWhole();
                    heldSlot.MarkDirty();
                    LimeSlot.MarkDirty();
                    OnLimeInserted();
                    MarkDirty(true);
                    Api.World.PlaySoundAt(interactSound, player, null, false, 8f, 0.6f);
                    return true;
                }
                else
                {
                    // Stack more lime into slot
                    int transferable = System.Math.Min(heldItem.StackSize,
                        LimeSlot.Itemstack.Collectible.MaxStackSize - LimeSlot.Itemstack.StackSize);
                    if (transferable > 0)
                    {
                        LimeSlot.Itemstack.StackSize += transferable;
                        heldSlot.TakeOut(transferable);
                        heldSlot.MarkDirty();
                        LimeSlot.MarkDirty();
                        MarkDirty(true);
                        Api.World.PlaySoundAt(interactSound, player, null, false, 8f, 0.6f);
                        return true;
                    }
                }
            }

            // Player has empty hand + lime slot is occupied → retrieve lime
            if (heldItem == null && !LimeSlot.Empty && FiberSlot.Empty)
            {
                if (player.InventoryManager.TryGiveItemstack(LimeSlot.Itemstack.Clone()))
                {
                    LimeSlot.Itemstack = null;
                    LimeSlot.MarkDirty();
                    MarkDirty(true);
                    Api.World.PlaySoundAt(interactSound, player, null, false, 8f, 0.6f);
                    return true;
                }
            }

            // Fall through to base class for fiber slot interactions
            return base.OnInteract(player);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            // Show lime slot status when vat is empty (so player knows lime is loaded)
            if (FiberSlot.Empty && !LimeSlot.Empty)
            {
                dsc.AppendLine(Lang.Get("rudiments:rettingvat-lime-loaded", LimeSlot.Itemstack.StackSize));
            }

            base.GetBlockInfo(forPlayer, dsc);
        }
    }
}
