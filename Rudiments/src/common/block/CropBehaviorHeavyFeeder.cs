using Rudiments.Utils;
using Vintagestory.API.Common;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// CropBehavior that drains nitrogen from neighbouring farmland tiles each time the nettle
    /// crop grows on farmland (cultivated path).
    ///
    /// Sets handling = EnumHandling.PassThrough and returns false so vanilla growth + own-soil
    /// nutrient consumption proceed normally after this behavior runs.
    ///
    /// Register via: api.RegisterCropBehavior("HeavyFeeder", typeof(CropBehaviorHeavyFeeder))
    /// NOTE: crop behaviors use a bare name (like vanilla "Pumpkin"), NOT a domain-prefixed one —
    /// the cropProps "name" must match the registered name exactly.
    /// JSON cropProps entry:
    ///   "behaviors": [{ "name": "HeavyFeeder", "properties": {} }]
    /// </summary>
    public class CropBehaviorHeavyFeeder : CropBehavior
    {
        public CropBehaviorHeavyFeeder(Block block) : base(block) { }

        public override bool TryGrowCrop(ICoreAPI api, IFarmlandBlockEntity farmland, double currentTotalHours, int newGrowthStage, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;

            if (RudimentsModSystem.Config.NettleHeavyFeederEnabled)
            {
                // farmland.Pos is the farmland tile; pass it directly — DepleteNeighborNitrogen
                // scans the four horizontal neighbours of that position.
                NettleFeeder.DepleteNeighborNitrogen(api.World, farmland.Pos, RudimentsModSystem.Config.NettleNeighborNitrogenDepletion);
            }

            // Return false so vanilla growth (SetBlock + ConsumeNutrients) proceeds.
            return false;
        }
    }
}
