using Rudiments.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Rudiments.SRC.Common.BlockEntities
{
    /// <summary>
    /// Field / dew retting. Lay rippled flax or cured nettle on the ground and the weather
    /// rets it over several days. Progress is weather-driven — rain and humidity speed it up,
    /// arid spells STALL it (no progress, no harm). Quality follows the 4-stage model shared
    /// by all retting methods. Thresholds can be overridden per-block via JSON attributes.
    /// </summary>
    public class BlockEntityFieldRetting : BlockEntityRettingBase
    {
        protected override string InvKey => "fieldretting";
        protected override string LangPrefix => "rudiments:fieldretting";

        // JSON-tunable: rainfall below this value stalls retting entirely.
        private double DryStallRainfall = 0.05;

        protected override void LoadDefaults()
        {
            MinRetHours   = 72;
            FineChance    = 0.7;
            FineDelayMin  = 24;
            FineDelayMax  = 72;
            FineWindow    = 36;
            StandardDelay = 96;
            StandardHold  = 168;

            var attrs = Block?.Attributes;
            if (attrs != null)
                DryStallRainfall = attrs["dryStallRainfall"].AsDouble(DryStallRainfall);
        }

        public override bool OnInteract(IPlayer player)
        {
            var heldItem = player.InventoryManager.ActiveHotbarSlot?.Itemstack;

            if (heldItem == null && !FiberSlot.Empty)
            {
                if (player.InventoryManager.TryGiveItemstack(FiberSlot.Itemstack.Clone()))
                {
                    Api.World.PlaySoundAt(interactSound, player, null, false, 8f, 0.6f);
                    Api.World.BlockAccessor.SetBlock(0, Pos);
                    return true;
                }
                return false;
            }

            return base.OnInteract(player);
        }

        protected override double GetProgressRate()
        {
            var climate = Api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.NowValues);
            if (climate == null) return 0.5;

            // Arid weather stalls field retting — no progress, no damage.
            if (climate.Rainfall < DryStallRainfall) return 0.0;

            // Above the stall threshold: moisture × temperature, with the 0.35 moisture floor
            // the original formula used for warm/damp (non-arid) climates.
            double factor = GameMath.Clamp(0.35 + climate.Rainfall * 1.3, 0.2, 1.6)
                          * GameMath.Clamp((climate.Temperature + 2.0) / 22.0, 0.1, 1.3);

            // Active rainfall really gets retting going.
            if (FieldWeather.IsExposedRaining(Api.World, Pos, 0.04))
                factor *= 1.5;

            return factor;
        }
    }
}
