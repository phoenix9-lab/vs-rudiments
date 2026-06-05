using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// Reusable rhizome-spread behavior. Applied to nettle crop and all reedpapyrus variants.
    ///
    /// Design: the hosting block calls TrySpreadTick() from its own OnServerGameTick override.
    ///
    /// Config properties (all optional):
    ///   configGroup         - "nettle" or "reed"; selects which RudimentsConfig fields to read
    ///   densityMatch        - string array of code-path prefixes to count for the density cap
    ///   requireFertileSoil  - override auto-detect: force soil check
    ///   requireWater        - override auto-detect: force water check
    ///
    /// Auto-detection rules (when not explicitly configured):
    ///   - If block has variant "habitat" == "water" or "ice" → requires water below the target
    ///   - Otherwise → requires fertile soil below the target (Fertility > 0)
    ///   - If no spread block is configured for reed → spreads as the current block (same variant)
    ///   - Never spreads when block variant "state" == "harvested"
    /// </summary>
    public class BlockBehaviorRhizomeSpread : BlockBehavior
    {
        private string configGroup = "";
        private string[] densityMatch = Array.Empty<string>();

        private bool? requireFertileSoil = null; // null = auto-detect
        private bool? requireWater       = null; // null = auto-detect

        public BlockBehaviorRhizomeSpread(Block block) : base(block) { }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);

            configGroup = properties["configGroup"].AsString("");

            if (properties["densityMatch"].Exists)
                densityMatch = properties["densityMatch"].AsArray<string>(Array.Empty<string>());

            if (properties["requireFertileSoil"].Exists) requireFertileSoil = properties["requireFertileSoil"].AsBool();
            if (properties["requireWater"].Exists)       requireWater       = properties["requireWater"].AsBool();
        }

        /// <summary>
        /// Call from the hosting block's tick. Returns the position a new plant was placed at, or null
        /// if nothing spread this attempt.
        ///
        /// <paramref name="originX"/>/<paramref name="originZ"/> + <paramref name="maxRadius"/> enforce a
        /// hard outward cap: targets further than maxRadius (horizontal) from the patch origin are
        /// rejected. Pass maxRadius &lt;= 0 (or no origin) for unbounded spread.
        /// </summary>
        public BlockPos TrySpreadTick(IWorldAccessor world, BlockPos pos, int originX = int.MinValue, int originZ = int.MinValue, int maxRadius = 0)
        {
            // Never spread from a harvested state (e.g. coopersreed-harvested)
            if (block.Variant?["state"] == "harvested") return null;

            var cfg = RudimentsModSystem.Config;

            // ── Group-specific enabled check and config lookup ────────────────────────
            double plainChance, tilledChance;
            int maxDensity, densityRadius;
            bool isNettle = configGroup == "nettle";
            bool isReed   = configGroup == "reed";

            if (isNettle)
            {
                if (!cfg.NettleSpreadEnabled) return null;
                plainChance    = cfg.NettleSpreadChance;
                tilledChance   = cfg.NettleTilledSpreadChance;
                maxDensity     = cfg.NettleSpreadMaxDensity;
                densityRadius  = cfg.NettleSpreadDensityRadius;
            }
            else if (isReed)
            {
                if (!cfg.ReedSpreadEnabled) return null;
                plainChance    = cfg.ReedSpreadChance;
                tilledChance   = cfg.ReedSpreadChance; // no tilled bonus for reeds
                maxDensity     = cfg.ReedSpreadMaxDensity;
                densityRadius  = cfg.ReedSpreadDensityRadius;
            }
            else
            {
                // Legacy path: no configGroup set, use a single hardcoded fallback
                plainChance   = 0.05;
                tilledChance  = 0.05;
                maxDensity    = 0;
                densityRadius = 2;
            }

            // ── Pick random adjacent target ────────────────────────────────────────────
            BlockFacing face   = BlockFacing.HORIZONTALS[world.Rand.Next(4)];
            BlockPos target    = pos.AddCopy(face.Normali);
            BlockPos belowPos  = target.DownCopy();

            // ── Outward radius cap (hard bound on patch size from its origin) ─────────────
            if (maxRadius > 0 && originX != int.MinValue)
            {
                long dx = target.X - originX;
                long dz = target.Z - originZ;
                if (dx * dx + dz * dz > (long)maxRadius * maxRadius) return null;
            }

            Block atTarget   = world.BlockAccessor.GetBlock(target);
            Block belowBlock = world.BlockAccessor.GetBlock(belowPos);

            if (atTarget.Replaceable < 6000) return null;

            // ── Resolve water vs soil requirement ─────────────────────────────────────
            bool needsWater, needsSoil;
            if (requireWater.HasValue || requireFertileSoil.HasValue)
            {
                needsWater = requireWater ?? false;
                needsSoil  = requireFertileSoil ?? false;
            }
            else
            {
                string habitat = block.Variant?["habitat"];
                needsWater = habitat == "water" || habitat == "ice";
                needsSoil  = !needsWater;
            }

            if (needsWater && !belowBlock.IsLiquid())    return null;
            if (needsSoil  && belowBlock.Fertility <= 0) return null;

            // ── Chance roll (after target selection so tilled bonus is accurate) ──────
            double effChance = (belowBlock is BlockFarmland) ? tilledChance : plainChance;
            if (world.Rand.NextDouble() >= effChance) return null;

            // ── Density cap ────────────────────────────────────────────────────────────
            if (maxDensity > 0 && densityMatch.Length > 0)
            {
                int count = 0;
                BlockPos scanMin = target.AddCopy(-densityRadius, -densityRadius, -densityRadius);
                BlockPos scanMax = target.AddCopy( densityRadius,  densityRadius,  densityRadius);

                world.BlockAccessor.WalkBlocks(scanMin, scanMax, (scannedBlock, x, y, z) =>
                {
                    if (scannedBlock.Code?.Domain == "rudiments")
                    {
                        string path = scannedBlock.Code.Path;
                        foreach (string prefix in densityMatch)
                        {
                            if (path.StartsWith(prefix, StringComparison.Ordinal))
                            {
                                count++;
                                break;
                            }
                        }
                    }
                });

                if (count >= maxDensity) return null;
            }

            // ── Resolve the spread block ───────────────────────────────────────────────
            Block spreadBlock;
            if (isNettle)
            {
                // Use hidden rhizome when creep is enabled, otherwise place visible stage-1
                string spreadCode = cfg.NettleCreepEnabled ? "rudiments:nettlecreep" : "rudiments:crop-nettle-1";
                spreadBlock = world.GetBlock(new AssetLocation(spreadCode));
            }
            else
            {
                // Reed (and legacy): use the current block variant (same as old behaviour)
                spreadBlock = block;
            }

            if (spreadBlock == null) return null;

            world.BlockAccessor.SetBlock(spreadBlock.BlockId, target);
            return target;
        }
    }
}
