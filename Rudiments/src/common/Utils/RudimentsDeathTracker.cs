using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Rudiments.Utils
{
    /// <summary>
    /// Remembers which players died in the last couple of seconds, so drop breakage can tell a
    /// deliberately thrown pot from a pot scattered by a corpse.
    ///
    /// It is needed because death drops <b>do</b> carry <c>ByPlayerUid</c>: they go through
    /// <c>InventoryBasePlayer.spawnItemEntity</c>, which sets it exactly like a hand-thrown item.
    /// Without this, dying with a shelf of pottery in your bags would smash all of it.
    ///
    /// The drop is enqueued one main-thread frame after the death event fires, so the window is
    /// generous. <see cref="Rudiments.SRC.Common.Entities.EntityBehaviorClayFragile"/> checks the
    /// exact <c>minsecondsToDespawn</c> marker first and only falls back to this window — the two
    /// together cover the case where a future version stops setting that marker.
    /// </summary>
    public static class RudimentsDeathTracker
    {
        private const long WindowMs = 2000;

        private static readonly Dictionary<string, long> deathTimes = new Dictionary<string, long>();
        private static ICoreServerAPI sapi;

        public static void Register(ICoreServerAPI api)
        {
            sapi = api;
            api.Event.PlayerDeath += OnPlayerDeath;
            api.Event.PlayerDisconnect += plr => deathTimes.Remove(plr.PlayerUID);
        }

        private static void OnPlayerDeath(IServerPlayer byPlayer, DamageSource damageSource)
        {
            if (byPlayer?.PlayerUID == null) return;
            deathTimes[byPlayer.PlayerUID] = sapi.World.ElapsedMilliseconds;
        }

        public static bool DiedJustNow(string playerUid)
        {
            if (sapi == null || playerUid == null) return false;
            if (!deathTimes.TryGetValue(playerUid, out long at)) return false;
            return sapi.World.ElapsedMilliseconds - at < WindowMs;
        }
    }
}
