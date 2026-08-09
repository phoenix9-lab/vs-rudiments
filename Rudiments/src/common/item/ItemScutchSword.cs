using Rudiments.SRC.Common.BlockEntities;
using Rudiments.SRC.Common.Blocks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Rudiments.SRC.Common.Items
{
    /// <summary>
    /// Scutching sword (Swedish <em>skäkta</em>, also swingle or scutching knife) — a wooden blade,
    /// deliberately dulled so it scrapes the woody boon off retted stems without cutting the fibre.
    ///
    /// Hold <strong>left mouse</strong> against a loaded <see cref="BlockEntityScutchBoard"/> to strike
    /// it rhythmically. The board owns all the state; this item only turns held-attack time into
    /// discrete strokes and dresses them with particles and sound. Against anything else it falls
    /// through to the default attack, so it still works as a crude club.
    /// </summary>
    public class ItemScutchSword : Item
    {
        // Per-stack, auto-scoped scratch space: the board being worked and the last stroke counted.
        // Preferable to entity attributes because two players on two boards never collide.
        private const string PosXKey = "scutchPosX";
        private const string PosYKey = "scutchPosY";
        private const string PosZKey = "scutchPosZ";
        private const string StrokeKey = "scutchStroke";

        /// <summary>Beyond this the player has walked off the board and the swing stops.</summary>
        private const float MaxWorkDistance = 6f;

        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            if (blockSel == null || slot?.Itemstack == null)
            {
                base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
                return;
            }

            if (byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) is not BlockEntityScutchBoard be || be.IsEmpty)
            {
                base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
                return;
            }

            slot.Itemstack.TempAttributes.SetInt(PosXKey, blockSel.Position.X);
            slot.Itemstack.TempAttributes.SetInt(PosYKey, blockSel.Position.InternalY);
            slot.Itemstack.TempAttributes.SetInt(PosZKey, blockSel.Position.Z);
            slot.Itemstack.TempAttributes.SetInt(StrokeKey, -1);

            // PreventDefaultAction plays the swing animation on both sides and calls *Step / *Stop,
            // but does not break blocks — so the board takes no damage from being scutched on.
            handling = EnumHandHandling.PreventDefaultAction;
        }

        public override bool OnHeldAttackStep(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
        {
            if (slot?.Itemstack == null) return false;

            BlockPos pos = StashedPos(slot);
            if (pos == null) return false;

            // Aim drift mid-swing must not retarget the strokes, but walking away ends them.
            if (byEntity.Pos.XYZ.DistanceTo(pos.ToVec3d().Add(0.5, 0.5, 0.5)) > MaxWorkDistance) return false;
            if (blockSelection != null && !blockSelection.Position.Equals(pos)) return false;

            if (byEntity.World.BlockAccessor.GetBlockEntity(pos) is not BlockEntityScutchBoard be || be.IsEmpty) return false;

            float strokesPerSecond = GameMath.Clamp(RudimentsModSystem.Config.ScutchStrokesPerSecond, 0.1f, 20f);
            int strokeNo = (int)(secondsPassed * strokesPerSecond);
            if (strokeNo == slot.Itemstack.TempAttributes.GetInt(StrokeKey, -1)) return true;

            slot.Itemstack.TempAttributes.SetInt(StrokeKey, strokeNo);

            // Server-authoritative: the meters move here and nowhere else, and MarkDirty pushes them
            // to every client watching the board.
            if (byEntity.World.Side != EnumAppSide.Server) return true;
            if (!be.ApplyStroke((byEntity as EntityPlayer)?.Player)) return false;

            OnStrokeLanded(byEntity.World, pos, be.BladeOnBareFiber);

            DamageItem(byEntity.World, byEntity, slot, 1);
            slot.MarkDirty();

            // The stack is gone if the sword just broke.
            return slot.Itemstack != null;
        }

        /// <summary>
        /// The sensory tell. While there is still boon to clear the strike is dull and throws brown
        /// shives; once the worked side is clean the blade is on bare fibre, the note sharpens and
        /// pale fluff comes off instead — the same signal that says "this side is done, turn it".
        /// Spawned server-side so every client sees the same cue as the meters they were sent.
        /// </summary>
        private static void OnStrokeLanded(IWorldAccessor world, BlockPos pos, bool onBareFiber)
        {
            SimpleParticleProperties particles = onBareFiber ? BlockScutchBoard.TowParticles : BlockScutchBoard.ShiveParticles;
            particles.MinPos.Set(pos.X + 0.35f, pos.Y + 1.0f, pos.Z + 0.35f);
            world.SpawnParticles(particles);

            world.PlaySoundAt(
                new AssetLocation("game", "sounds/block/planks"),
                pos.X + 0.5, pos.Y + 1.0, pos.Z + 0.5,
                null,
                onBareFiber ? 1.35f : 0.7f,
                12f);
        }

        public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
        {
            ClearStash(slot);
        }

        public override bool OnHeldAttackCancel(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            ClearStash(slot);
            return true;
        }

        /// <summary>The board this swing was aimed at. Y round-trips through InternalY so the
        /// three-argument constructor recovers the dimension.</summary>
        private static BlockPos StashedPos(ItemSlot slot)
        {
            if (slot?.Itemstack == null) return null;
            if (!slot.Itemstack.TempAttributes.HasAttribute(PosXKey)) return null;

            return new BlockPos(
                slot.Itemstack.TempAttributes.GetInt(PosXKey),
                slot.Itemstack.TempAttributes.GetInt(PosYKey),
                slot.Itemstack.TempAttributes.GetInt(PosZKey));
        }

        private static void ClearStash(ItemSlot slot)
        {
            if (slot?.Itemstack == null) return;
            slot.Itemstack.TempAttributes.RemoveAttribute(PosXKey);
            slot.Itemstack.TempAttributes.RemoveAttribute(PosYKey);
            slot.Itemstack.TempAttributes.RemoveAttribute(PosZKey);
            slot.Itemstack.TempAttributes.RemoveAttribute(StrokeKey);
        }
    }
}
