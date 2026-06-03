using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Rudiments.Utils
{
    /// <summary>
    /// Shared weather/climate helper for field blocks (field retting and stook).
    /// Centralises the exposure check and climate factor math that was previously
    /// duplicated between BlockEntityRettingBed and BlockEntityDryingRack.
    /// </summary>
    public static class FieldWeather
    {
        /// <summary>
        /// Returns true when the position is at or above the rain-map height AND
        /// the current rainfall exceeds <paramref name="rainfallThreshold"/>.
        /// </summary>
        public static bool IsExposedRaining(IWorldAccessor world, BlockPos pos, double rainfallThreshold = 0.04)
        {
            int rainY = world.BlockAccessor.GetRainMapHeightAt(pos.X, pos.Z);
            if (pos.Y < rainY) return false;
            var climate = world.BlockAccessor.GetClimateAt(pos, EnumGetClimateMode.NowValues);
            return climate != null && climate.Rainfall > rainfallThreshold;
        }

        /// <summary>
        /// The temperature × humidity dry-factor used by the drying rack and stook dry mode.
        /// Warm + low humidity → fast drying; cold/wet → slow. Does NOT include the
        /// exposed-rain penalty (×0.15) — callers apply that separately so behaviour
        /// remains identical to the original rack logic.
        /// </summary>
        public static double DryFactor(ClimateCondition climate)
        {
            if (climate == null) return 1.0;
            double tempF = GameMath.Clamp((climate.Temperature + 5.0) / 25.0, 0.15, 1.6);
            double humid = GameMath.Clamp(1.0 - climate.Rainfall * 0.8, 0.3, 1.0);
            return tempF * humid;
        }
    }
}
