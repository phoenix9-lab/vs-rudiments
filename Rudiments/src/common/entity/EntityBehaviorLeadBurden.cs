using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.Entities
{
    /// <summary>
    /// How much lead a player is carrying, and what it costs them.
    ///
    /// Lead poisoning is chronic, cumulative and slowly reversible, and that is exactly the shape
    /// <c>EntityBehaviorHealth.SetMaxHealthModifiers</c> already gives for free: a keyed entry summed
    /// into max health, adjustable every tick and removed by setting it to zero. So this is not
    /// damage over time and not a one-shot poison. A burden accrues per helping eaten or drunk from
    /// leaded ware, decays whenever you are not doing that, and the max-health penalty is a function
    /// of the burden with a grace threshold under it — occasional use is genuinely free.
    ///
    /// Lowering max health while at full health lowers current health with it, which vanilla's
    /// <c>UpdateMaxHealth</c> does on its own. Dying does not clear it. Neither does time in a bed:
    /// only not using the stuff.
    ///
    /// Server side only, like the health behavior itself. The penalty reaches the client through the
    /// synced health tree, and the player is told in words when it moves — there is no GUI for this
    /// and there is not supposed to be.
    /// </summary>
    public class EntityBehaviorLeadBurden : EntityBehavior
    {
        /// <summary>Accumulated dose, in helpings. Persisted on the player.</summary>
        public const string BurdenKey = "rudimentsleadburden";

        /// <summary>Total game days at the last decay settle, so decay runs while logged out too.</summary>
        private const string SettledKey = "rudimentsleadsettled";

        /// <summary>Ours alone: another mod's max-health modifier cannot collide with this key.</summary>
        private const string HealthModifierKey = "rudiments:lead";

        private const float CheckIntervalSeconds = 5f;

        private float sinceLastCheck;

        /// <summary>Last announced severity. -1 until the first tick, so logging in says nothing.</summary>
        private int lastStage = -1;

        public EntityBehaviorLeadBurden(Entity entity) : base(entity) { }

        public override string PropertyName() => "rudiments:leadburden";

        public double Burden
        {
            get => entity.WatchedAttributes.GetDouble(BurdenKey);
            private set => entity.WatchedAttributes.SetDouble(BurdenKey, Math.Max(0, value));
        }

        /// <summary>Wipes the burden and the penalty with it. The <c>/rudimentslead clear</c> route.</summary>
        public void Clear()
        {
            Burden = 0;
            entity.WatchedAttributes.SetDouble(SettledKey, entity.World.Calendar.TotalDays);
            ApplyPenalty();
        }

        /// <summary>Takes a dose. Settles first so the new lead is not retroactively decayed.</summary>
        public void Add(double amount)
        {
            if (amount <= 0) return;

            Settle();
            Burden += amount;
            ApplyPenalty();
        }

        public override void OnGameTick(float deltaTime)
        {
            base.OnGameTick(deltaTime);

            if (entity.World.Side != EnumAppSide.Server) return;

            sinceLastCheck += deltaTime;
            if (sinceLastCheck < CheckIntervalSeconds) return;
            sinceLastCheck = 0;

            Settle();
            ApplyPenalty();
        }

        /// <summary>
        /// Brings the burden forward to now. Calendar-driven rather than tick-driven, so it keeps
        /// running while the player is offline and it responds to the server's time speed.
        /// </summary>
        private void Settle()
        {
            double now = entity.World.Calendar.TotalDays;
            double last = entity.WatchedAttributes.GetDouble(SettledKey, now);
            double elapsed = now - last;

            // Negative means the calendar moved backwards under us (/time set). Nothing to decay;
            // just re-anchor rather than crediting the player a windfall on the next settle.
            if (elapsed > 0)
            {
                Burden -= elapsed * Math.Max(0, RudimentsModSystem.Config.LeadDecayPerDay);
            }

            if (elapsed != 0) entity.WatchedAttributes.SetDouble(SettledKey, now);
        }

        /// <summary>Max health points currently lost to lead. Zero below the grace threshold.</summary>
        public float Penalty()
        {
            if (!RudimentsModSystem.LeadPoisoningEnabled) return 0;

            RudimentsConfig cfg = RudimentsModSystem.Config;

            double over = Burden - Math.Max(0, cfg.LeadOnsetBurden);
            if (over <= 0) return 0;

            double perPoint = Math.Max(0.01, cfg.LeadBurdenPerHealthPoint);
            return (float)Math.Min(Math.Max(0, cfg.LeadMaxHealthPenalty), over / perPoint);
        }

        private void ApplyPenalty()
        {
            EntityBehaviorHealth health = entity.GetBehavior<EntityBehaviorHealth>();
            if (health == null) return;

            float penalty = Penalty();

            // Idempotent by design: SetMaxHealthModifiers only recomputes when the value actually
            // moved, so calling it every five seconds costs nothing and re-applies itself after a
            // relog (the modifier dictionary is in memory; the burden is what persists).
            health.SetMaxHealthModifiers(HealthModifierKey, -penalty);

            Announce(penalty);
        }

        /// <summary>
        /// Tells the player in words when the severity moves. Stage 1 is "there is lead in you but it
        /// has not cost you a heart yet", which is the warning that makes the rest fair.
        /// </summary>
        private void Announce(float penalty)
        {
            int stage = penalty <= 0 ? 0 : 1 + (int)penalty;
            if (stage == lastStage) return;

            int previous = lastStage;
            lastStage = stage;
            if (previous < 0) return;

            if (entity is not EntityPlayer entityPlayer) return;
            if (entityPlayer.Player is not IServerPlayer splr) return;

            string key = stage > previous
                ? (previous == 0 ? "rudiments:lead-onset" : "rudiments:lead-worse")
                : (stage == 0 ? "rudiments:lead-clear" : "rudiments:lead-better");

            splr.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get(key), EnumChatType.Notification);
        }
    }
}
