using Rudiments.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Rudiments.SRC.Common.BlockEntities
{
    /// <summary>
    /// Field / dew retting. Lay rippled flax or cured nettle on the ground and the weather
    /// rets it over several days. Always makes slow progress (dew alone suffices); rain and
    /// humidity speed it up significantly. Bundles placed in water or rivers ret at a steady
    /// faster rate without weather dependency. Quality follows the 4-stage model shared by
    /// all retting methods. Thresholds can be overridden per-block via JSON attributes.
    /// </summary>
    public class BlockEntityFieldRetting : BlockEntityRettingBase
    {
        protected override string InvKey => "fieldretting";
        protected override string LangPrefix => "rudiments:fieldretting";

        protected override void LoadDefaults()
        {
            MinRetHours   = 72;
            FineChance    = 0.7;
            FineDelayMin  = 24;
            FineDelayMax  = 72;
            FineWindow    = 36;
            StandardDelay = 96;
            StandardHold  = 168;
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
            // Submerged in water or river: steady retting without weather dependency.
            var liquidBlock = Api.World.BlockAccessor.GetBlock(Pos, BlockLayersAccess.Fluid);
            if (liquidBlock.IsLiquid())
                return 1.0;

            var climate = Api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.NowValues);
            if (climate == null) return 0.2;

            // Dew retting always progresses. A 0.2 baseline means even arid/dry weather
            // makes slow headway; moisture and temperature scale up from there.
            double factor = GameMath.Clamp(0.2 + climate.Rainfall * 1.0, 0.2, 1.4)
                          * GameMath.Clamp((climate.Temperature + 2.0) / 22.0, 0.1, 1.3);

            // Active rainfall significantly boosts retting.
            if (FieldWeather.IsExposedRaining(Api.World, Pos, 0.04))
                factor *= 2.0;

            return factor;
        }
    }
}
