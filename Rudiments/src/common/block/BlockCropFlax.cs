using Rudiments.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// Vanilla flax crop with the rudiments class. Stage-based drops are defined in
    /// patches/crop-flax.json (conditioned on the FlaxBloomHarvest world config flag):
    /// nothing before bloom, fiber-only at stage 8 (in bloom), fiber + seeds + grain at
    /// stage 9 (mature). Bundles are stamped with their harvest potential here — bloom-cut
    /// rets Standard→Fine and never yields seeds; mature rets Coarse→Standard like nettle.
    /// With FlaxBloomHarvest disabled the legacy drop table applies and nothing is stamped,
    /// so every bundle rets over the full coarse-to-fine range. The base BlockCrop handles
    /// wild drop reduction.
    /// </summary>
    public class BlockCropFlax : BlockCrop
    {
        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            ItemStack[] drops = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
            if (drops == null) return null;
            if (!RudimentsModSystem.Config.FlaxBloomHarvest) return drops;

            // Second-to-last stage = in bloom (peak fiber); last stage = fully mature.
            int potential;
            if (CurrentCropStage == CropProps.GrowthStages - 1) potential = FiberQuality.Fine;
            else if (CurrentCropStage == CropProps.GrowthStages) potential = FiberQuality.Standard;
            else return drops;

            foreach (ItemStack drop in drops)
            {
                if (drop?.Collectible?.Code?.Path?.StartsWith("flaxbundle") == true)
                {
                    FiberQuality.SetPotential(drop, potential);
                }
            }

            return drops;
        }
    }
}
