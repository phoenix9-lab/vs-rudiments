using System;
using Rudiments.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Rudiments.SRC.Common.Entities
{
    /// <summary>
    /// Fired pottery does not survive being thrown on the floor. One behavior on the item entity
    /// type covers every clay item in the game, including other mods', with no per-item patching —
    /// see <see cref="ClayWare.IsBreakablePottery"/> for the three predicates that decide what
    /// counts.
    ///
    /// Two triggers, both of which have to get past the exemptions:
    ///   • <b>deliberate</b> — the player threw it (<c>ByPlayerUid</c> is set by the ground-slot
    ///     drop path and by nothing else except a player inventory dump)
    ///   • <b>hard landing</b> — it hit the ground fast enough, however it got there
    ///
    /// And the exemption that matters: <b>death drops</b>. They carry <c>ByPlayerUid</c> too, so
    /// without an explicit test, dying with pottery in your bags would smash all of it. Two
    /// independent tests catch them — the <c>minsecondsToDespawn</c> marker that only
    /// <c>InventoryBasePlayer</c> writes, and <see cref="RudimentsDeathTracker"/>'s recent-death
    /// window.
    /// </summary>
    public class EntityBehaviorClayFragile : EntityBehavior
    {
        /// <summary>
        /// Set by <c>InventoryBasePlayer.spawnItemEntity</c> and nowhere else in the game, which
        /// makes it an exact "this came out of a player inventory dump" marker — death drops and
        /// the disconnect mouse-slot secure. A hand-thrown item never carries it.
        /// </summary>
        private const string InventoryDumpMarker = "minsecondsToDespawn";

        public EntityBehaviorClayFragile(Entity entity) : base(entity) { }

        public override string PropertyName() => "rudiments:clayfragile";

        public override void OnFallToGround(Vec3d lastTerrainContact, double withYMotion)
        {
            base.OnFallToGround(lastTerrainContact, withYMotion);

            IWorldAccessor world = entity?.World;
            if (world == null || world.Side != EnumAppSide.Server) return;
            if (entity is not EntityItem itemEntity) return;

            ItemStack stack = itemEntity.Itemstack;
            if (!ClayWare.IsBreakablePottery(stack)) return;

            RudimentsConfig cfg = RudimentsModSystem.Config;

            if (!cfg.ThrownBreakOnDeathDrop && IsDeathDrop(itemEntity)) return;

            bool deliberate = itemEntity.ByPlayerUid != null;
            bool hardLanding = cfg.ClayImpactBreakSpeed > 0 && Math.Abs(withYMotion) >= cfg.ClayImpactBreakSpeed;
            if (!deliberate && !hardLanding) return;

            if (cfg.ThrownClayBreakChance <= 0) return;
            if (world.Rand.NextDouble() >= cfg.ThrownClayBreakChance) return;

            int quantity = cfg.ThrownClayBreakWholeStack ? stack.StackSize : 1;

            // Spill the contents once rather than once per item: a stack shares one `contents`
            // attribute, so spilling per item would duplicate whatever is inside it.
            ItemStack representative = stack.Clone();
            representative.StackSize = 1;
            ClayWare.Shatter(world, entity.Pos.XYZ, representative, quantity);

            if (quantity >= stack.StackSize)
            {
                entity.Die(EnumDespawnReason.Removed);
            }
            else
            {
                stack.StackSize -= quantity;
                itemEntity.WatchedAttributes.MarkPathDirty("itemstack");
            }
        }

        private static bool IsDeathDrop(EntityItem itemEntity)
        {
            if (itemEntity.Attributes?.HasAttribute(InventoryDumpMarker) == true) return true;
            return RudimentsDeathTracker.DiedJustNow(itemEntity.ByPlayerUid);
        }
    }
}
