using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Rudiments.Utils
{
    public static class NettleSting
    {
        public const float StingDamage = 0.25f;
        private const long CooldownMs = 1500;

        /// <summary>
        /// Harvesting protection: gloves or gauntlets worn on the hands slot.
        /// Tools (knife, axe) give no exemption.
        /// </summary>
        public static bool WearingGloves(IPlayer player)
        {
            var inv = player?.InventoryManager?.GetOwnInventory(GlobalConstants.characterInvClassName);
            if (inv == null) return false;
            foreach (var slot in inv)
            {
                var code = slot?.Itemstack?.Collectible?.Code?.Path;
                if (code != null && (code.Contains("glove") || code.Contains("gauntlet"))) return true;
            }
            return false;
        }

        /// <summary>
        /// Walk-through protection: actual armor (items with protectionModifiers).
        /// Decorative clothing, accessories, and backpacks do NOT protect.
        /// </summary>
        public static bool WearingArmor(IPlayer player)
        {
            var inv = player?.InventoryManager?.GetOwnInventory(GlobalConstants.characterInvClassName);
            if (inv == null) return false;
            foreach (var slot in inv)
            {
                if (slot?.Itemstack == null) continue;
                if (slot.Itemstack.Collectible.Attributes?["protectionModifiers"].Exists == true)
                    return true;
            }
            return false;
        }

        /// <summary>Sting if bare-handed (harvesting). Server-authoritative.</summary>
        public static void TrySting(IWorldAccessor world, IPlayer player, BlockPos pos)
        {
            if (world.Side != EnumAppSide.Server || player?.Entity == null) return;
            if (WearingGloves(player)) return;
            ApplySting(world, player, pos);
        }

        /// <summary>Sting if unarmored (walk-through). Server-authoritative.</summary>
        public static void TryStingWalkthrough(IWorldAccessor world, IPlayer player, BlockPos pos)
        {
            if (world.Side != EnumAppSide.Server || player?.Entity == null) return;
            if (WearingArmor(player)) return;
            ApplySting(world, player, pos);
        }

        private static void ApplySting(IWorldAccessor world, IPlayer player, BlockPos pos)
        {
            long now = world.ElapsedMilliseconds;
            long last = player.Entity.Attributes.GetLong("nettleStingMs", 0);
            if (now - last < CooldownMs) return;
            player.Entity.Attributes.SetLong("nettleStingMs", now);

            player.Entity.ReceiveDamage(new DamageSource()
            {
                Source = EnumDamageSource.Block,
                Type = EnumDamageType.PiercingAttack,
                SourcePos = pos?.ToVec3d()
            }, StingDamage);

            world.PlaySoundAt(new AssetLocation("game:sounds/player/hurt1"), player, null, false, 8f, 0.3f);
        }
    }
}
